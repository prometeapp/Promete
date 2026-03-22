using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.GLDesktop;

/// <summary>
/// OpenGL バックエンドによる <see cref="IRenderTextureProvider"/> の実装です。
/// </summary>
internal sealed class GLRenderTextureProvider : IRenderTextureProvider
{
    // RenderTexture ごとの GL リソースキャッシュ
    private readonly Dictionary<RenderTexture, (uint fbo, uint rbo)> _cache = [];
    private readonly OpenGLDesktopWindow _window;
    private GL GL => _window.GL;

    public GLRenderTextureProvider(IWindow window)
    {
        _window = (OpenGLDesktopWindow)window;
        _window.Destroy += ClearAll;
    }

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
        var gl = GL;
        var previousViewport = GLHelper.GetViewport(gl);
        gl.GetInteger(GLEnum.FramebufferBinding, out var previousFbo);

        gl.BindFramebuffer(GLEnum.Framebuffer, _cache[rt].fbo);
        gl.Viewport(0, 0, (uint)rt.Size.X, (uint)rt.Size.Y);

        if (clearColor is { } c)
        {
            gl.ClearColor(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        return new CaptureScope(gl, (uint)previousFbo, previousViewport);
    }

    public unsafe void Resize(RenderTexture rt, VectorInt newSize)
    {
        var gl = GL;

        // テクスチャを同一ハンドルで再割り当て（既存の Texture2D 参照を維持）
        gl.BindTexture(GLEnum.Texture2D, (uint)rt.Texture.Handle);
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba,
            (uint)newSize.X, (uint)newSize.Y, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        gl.BindTexture(GLEnum.Texture2D, 0);

        // RBO を作り直す
        var (fbo, oldRbo) = _cache[rt];
        gl.DeleteRenderbuffer(oldRbo);
        var newRbo = CreateRBO(newSize);

        gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
        gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, newRbo);
        gl.BindFramebuffer(GLEnum.Framebuffer, 0);

        _cache[rt] = (fbo, newRbo);

        // Texture2D のサイズ情報を更新（ハンドルは再利用）
        rt.Texture = new Texture2D(rt.Texture.Handle, newSize, _ => { });
    }

    public void Release(RenderTexture rt)
    {
        if (!_cache.TryGetValue(rt, out var cached)) return;
        var gl = GL;
        gl.DeleteFramebuffer(cached.fbo);
        gl.DeleteRenderbuffer(cached.rbo);
        gl.DeleteTexture((uint)rt.Texture.Handle);
        _cache.Remove(rt);
    }

    // --- private helpers ---

    private unsafe Texture2D CreateGLTexture(VectorInt size)
    {
        var gl = GL;
        var handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(GLEnum.Texture2D, handle);
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba,
            (uint)size.X, (uint)size.Y, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        gl.BindTexture(GLEnum.Texture2D, 0);
        return new Texture2D((int)handle, size, _ => { });
    }

    private uint CreateRBO(VectorInt size)
    {
        var gl = GL;
        var rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
        // ステンシルバッファも確保する（DepthComponent24のみだとステンシルテストが機能しない）
        gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.Depth24Stencil8, (uint)size.X, (uint)size.Y);
        gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);
        return rbo;
    }

    private (uint fbo, uint rbo) CreateFBO(uint textureHandle, VectorInt size)
    {
        var gl = GL;
        var rbo = CreateRBO(size);
        var fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
        gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, textureHandle, 0);
        gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, rbo);

        var status = gl.CheckFramebufferStatus(GLEnum.Framebuffer);
        gl.BindFramebuffer(GLEnum.Framebuffer, 0);

        if (status != GLEnum.FramebufferComplete)
            throw new InvalidOperationException($"フレームバッファが不完全です: {status}");

        return (fbo, rbo);
    }

    private void ClearAll()
    {
        var gl = GL;
        foreach (var (rt, (fbo, rbo)) in _cache.ToList())
        {
            gl.DeleteFramebuffer(fbo);
            gl.DeleteRenderbuffer(rbo);
            gl.DeleteTexture((uint)rt.Texture.Handle);
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
