using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Silk.NET.OpenGL;

namespace Promete.GLDesktop;

/// <summary>
/// OpenGL バックエンドによる <see cref="IRenderTextureProvider"/> の実装です。
/// </summary>
internal sealed class GLRenderTextureProvider : IRenderTextureProvider
{
    // RenderTexture ごとの GL リソースキャッシュ
    private readonly Dictionary<RenderTexture, (uint fbo, uint rbo)> _cache = [];

    public GLRenderTextureProvider(PrometeApp app)
    {
        app.Destroy += ClearAll;
    }

    internal GL GL { get; set; }

    public RenderTexture Create(VectorInt size)
    {
        var texture = CreateGLTexture(size);
        var rt = new RenderTexture(size, texture, this);
        var (fbo, rbo) = CreateFBO((uint)texture.Handle, size);
        _cache[rt] = (fbo, rbo);
        return rt;
    }

    public IDisposable BeginCapture(RenderTexture rt, Color? clearColor)
    {
        var previousViewport = GLHelper.GetViewport(GL);
        GL.GetInteger(GLEnum.FramebufferBinding, out var previousFbo);

        GL.BindFramebuffer(GLEnum.Framebuffer, _cache[rt].fbo);
        GL.Viewport(0, 0, (uint)rt.Size.X, (uint)rt.Size.Y);

        if (clearColor is { } c)
        {
            GL.ClearColor(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        return new CaptureScope(GL, (uint)previousFbo, previousViewport);
    }

    public unsafe void Resize(RenderTexture rt, VectorInt newSize)
    {
        // テクスチャを同一ハンドルで再割り当て（既存の Texture2D 参照を維持）
        GL.BindTexture(GLEnum.Texture2D, (uint)rt.Texture.Handle);
        GL.TexImage2D(
            GLEnum.Texture2D,
            0,
            (int)InternalFormat.Rgba,
            (uint)newSize.X,
            (uint)newSize.Y,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            null
        );
        GL.BindTexture(GLEnum.Texture2D, 0);

        // RBO を作り直す
        var (fbo, oldRbo) = _cache[rt];
        GL.DeleteRenderbuffer(oldRbo);
        var newRbo = CreateRBO(newSize);

        GL.BindFramebuffer(GLEnum.Framebuffer, fbo);
        GL.FramebufferRenderbuffer(
            GLEnum.Framebuffer,
            GLEnum.DepthStencilAttachment,
            GLEnum.Renderbuffer,
            newRbo
        );
        GL.BindFramebuffer(GLEnum.Framebuffer, 0);

        _cache[rt] = (fbo, newRbo);

        // Texture2D のサイズ情報を更新（ハンドルは再利用）
        rt.Texture = new Texture2D(rt.Texture.Handle, newSize, _ => { });
    }

    public void Release(RenderTexture rt)
    {
        if (!_cache.TryGetValue(rt, out var cached))
            return;
        GL.DeleteFramebuffer(cached.fbo);
        GL.DeleteRenderbuffer(cached.rbo);
        GL.DeleteTexture((uint)rt.Texture.Handle);
        _cache.Remove(rt);
    }

    // --- private helpers ---
    private unsafe Texture2D CreateGLTexture(VectorInt size)
    {
        var handle = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(GLEnum.Texture2D, handle);
        GL.TexImage2D(
            GLEnum.Texture2D,
            0,
            (int)InternalFormat.Rgba,
            (uint)size.X,
            (uint)size.Y,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            null
        );
        GL.TexParameter(
            GLEnum.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)GLEnum.Nearest
        );
        GL.TexParameter(
            GLEnum.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)GLEnum.Nearest
        );
        GL.BindTexture(GLEnum.Texture2D, 0);
        return new Texture2D((int)handle, size, _ => { });
    }

    private uint CreateRBO(VectorInt size)
    {
        var rbo = GL.GenRenderbuffer();
        GL.BindRenderbuffer(GLEnum.Renderbuffer, rbo);

        // ステンシルバッファも確保する（DepthComponent24のみだとステンシルテストが機能しない）
        GL.RenderbufferStorage(
            GLEnum.Renderbuffer,
            GLEnum.Depth24Stencil8,
            (uint)size.X,
            (uint)size.Y
        );
        GL.BindRenderbuffer(GLEnum.Renderbuffer, 0);
        return rbo;
    }

    private (uint fbo, uint rbo) CreateFBO(uint textureHandle, VectorInt size)
    {
        var rbo = CreateRBO(size);
        var fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(GLEnum.Framebuffer, fbo);
        GL.FramebufferTexture2D(
            GLEnum.Framebuffer,
            GLEnum.ColorAttachment0,
            GLEnum.Texture2D,
            textureHandle,
            0
        );
        GL.FramebufferRenderbuffer(
            GLEnum.Framebuffer,
            GLEnum.DepthStencilAttachment,
            GLEnum.Renderbuffer,
            rbo
        );

        var status = GL.CheckFramebufferStatus(GLEnum.Framebuffer);
        GL.BindFramebuffer(GLEnum.Framebuffer, 0);

        if (status != GLEnum.FramebufferComplete)
            throw new InvalidOperationException($"フレームバッファが不完全です: {status}");

        return (fbo, rbo);
    }

    private void ClearAll()
    {
        foreach (var (rt, (fbo, rbo)) in _cache.ToList())
        {
            GL.DeleteFramebuffer(fbo);
            GL.DeleteRenderbuffer(rbo);
            GL.DeleteTexture((uint)rt.Texture.Handle);
        }

        _cache.Clear();
    }

    // --- スコープ ---
    private readonly struct CaptureScope : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _previousFbo;
        private readonly VectorInt _previousViewport;

        public CaptureScope(GL gl, uint previousFbo, VectorInt previousViewport)
        {
            _gl = gl;
            _previousFbo = previousFbo;
            _previousViewport = previousViewport;
        }

        public void Dispose()
        {
            _gl.BindFramebuffer(GLEnum.Framebuffer, _previousFbo);
            _gl.Viewport(0, 0, (uint)_previousViewport.X, (uint)_previousViewport.Y);
        }
    }
}
