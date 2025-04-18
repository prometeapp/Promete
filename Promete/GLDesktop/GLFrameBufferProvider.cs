using System;
using System.Collections.Generic;
using System.Linq;
using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.GLDesktop;

public class GLFrameBufferProvider : IFrameBufferProvider
{
    private readonly PrometeApp _app;
    private readonly OpenGLDesktopWindow _glWindow;
    private readonly Dictionary<FrameBuffer, (uint framebuffer, uint renderbuffer, int textureHandle)> _fboCache = [];

    public GLFrameBufferProvider(IWindow window, PrometeApp app)
    {
        _app = app;
        _glWindow = (OpenGLDesktopWindow)window;

        // ウィンドウが破棄されたときにキャッシュをクリアする
        _glWindow.Destroy += ClearCache;
    }


    public unsafe Texture2D CreateTexture(FrameBuffer frameBuffer)
    {
        // テクスチャを作成
        var textureHandle = _glWindow.GL.GenTexture();
        var gl = _glWindow.GL;
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(GLEnum.Texture2D, textureHandle);
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba, (uint)frameBuffer.Width, (uint)frameBuffer.Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        return new Texture2D((int)textureHandle, new VectorInt(frameBuffer.Width, frameBuffer.Height), HandleTextureDispose);
    }


    public void Render(FrameBuffer frameBuffer)
    {
        var gl = _glWindow.GL;

        // FBOが存在しない場合は作成する
        if (!_fboCache.TryGetValue(frameBuffer, out var buffers))
        {
            buffers = GenerateNewBuffer(frameBuffer, gl);
        }

        // フレームバッファにバインド
        gl.BindFramebuffer(GLEnum.Framebuffer, buffers.framebuffer);

        // ビューポートを設定
        gl.Viewport(0, 0, (uint)frameBuffer.Width, (uint)frameBuffer.Height);

        // 背景をクリア（色と深度バッファをクリア）
        var bgColor = frameBuffer.BackgroundColor;
        gl.ClearColor(bgColor.R / 255f, bgColor.G / 255f, bgColor.B / 255f, bgColor.A / 255f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 子要素をレンダリング
        foreach (var child in frameBuffer.SortedChildren)
        {
            _app.RenderNode(child);
        }

        // フレームバッファのバインドを解除
        gl.BindFramebuffer(GLEnum.Framebuffer, 0);

        // ビューポートを元に戻す
        gl.Viewport(0, 0, (uint)_glWindow.ActualWidth, (uint)_glWindow.ActualHeight);
    }

    private (uint framebuffer, uint renderbuffer, int textureHandle) GenerateNewBuffer(FrameBuffer frameBuffer, GL gl)
    {
        // テクスチャをバインド
        var textureHandle = (uint)frameBuffer.Texture.Handle;
        gl.BindTexture(GLEnum.Texture2D, textureHandle);

        // レンダーバッファを作成（デプスバッファ用）
        var rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
        gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.DepthComponent24, (uint)frameBuffer.Width, (uint)frameBuffer.Height);

        // フレームバッファを作成
        var fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
        gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, textureHandle, 0);
        gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthAttachment, GLEnum.Renderbuffer, rbo);

        // フレームバッファの状態をチェック
        var status = gl.CheckFramebufferStatus(GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            // バインド解除を追加
            gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);
            gl.BindTexture(GLEnum.Texture2D, 0);

            throw new InvalidOperationException($"フレームバッファが不完全です: {status}");
        }

        // キャッシュに保存 (テクスチャハンドルも保存)
        (uint framebuffer, uint renderbuffer, int textureHandle) buffers = (fbo, rbo, (int)textureHandle);
        _fboCache[frameBuffer] = buffers;

        // バインドを解除
        gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);
        gl.BindTexture(GLEnum.Texture2D, 0);
        return buffers;
    }

    /// <summary>
    /// テクスチャが破棄されたときに呼び出されるコールバック
    /// </summary>
    private void HandleTextureDispose(Texture2D texture)
    {
        var (key, _) = _fboCache.FirstOrDefault(entry => entry.Value.textureHandle == texture.Handle);
        if (key == null) return;
        CleanupFramebuffer(key);
    }

    /// <summary>
    /// フレームバッファのリソースをクリーンアップします
    /// </summary>
    private void CleanupFramebuffer(FrameBuffer frameBuffer)
    {
        if (!_fboCache.TryGetValue(frameBuffer, out var buffers)) return;
        var gl = _glWindow.GL;
        gl.DeleteFramebuffer(buffers.framebuffer);
        gl.DeleteRenderbuffer(buffers.renderbuffer);
        gl.DeleteTexture((uint)buffers.textureHandle);
        _fboCache.Remove(frameBuffer);
    }

    /// <summary>
    /// キャッシュをクリアします。
    /// </summary>
    private void ClearCache()
    {
        var gl = _glWindow.GL;
        foreach (var (_, (fbo, rbo, textureHandle)) in _fboCache)
        {
            gl.DeleteFramebuffer(fbo);
            gl.DeleteRenderbuffer(rbo);
            gl.DeleteTexture((uint)textureHandle);
        }
        _fboCache.Clear();
    }
}
