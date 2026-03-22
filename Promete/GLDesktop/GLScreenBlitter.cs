using System;
using Promete.Graphics;
using Promete.Internal;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.GLDesktop;

/// <summary>
/// 全描画を一度スクリーンサイズの <see cref="RenderTexture"/> にキャプチャし、
/// その後デフォルト FBO (画面) へブリットするクラスです。
/// </summary>
internal sealed class GLScreenBlitter : IDisposable
{
    /// <summary>
    /// 全描画のキャプチャ先 RenderTexture を取得します。
    /// </summary>
    public RenderTexture ScreenRenderTexture { get; }

    private readonly OpenGLDesktopWindow _window;
    private uint _shader;
    private uint _vao, _vbo;
    private int _uScreenTexture;
    private bool _initialized;
    private bool _disposed;

    public GLScreenBlitter(IWindow window, IRenderTextureProvider provider)
    {
        _window = (OpenGLDesktopWindow)window;
        ScreenRenderTexture = provider.Create(_window.Size);
        _window.Resize += () => ScreenRenderTexture.Resize(_window.Size);
    }

    /// <summary>
    /// スクリーン RenderTexture の内容をデフォルト FBO に描画します。
    /// </summary>
    public void BlitToScreen()
    {
        EnsureInitialized();
        var gl = _window.GL;

        // デフォルト FBO へバインド
        gl.BindFramebuffer(GLEnum.Framebuffer, 0);

        // 物理ピクセルサイズでビューポートを設定
        var size = _window.ActualSize;
        gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);

        gl.Disable(GLEnum.Blend);

        gl.UseProgram(_shader);

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(GLEnum.Texture2D, (uint)ScreenRenderTexture.Texture.Handle);
        gl.Uniform1(_uScreenTexture, 0);

        gl.BindVertexArray(_vao);
        gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        gl.BindVertexArray(0);

        gl.BindTexture(GLEnum.Texture2D, 0);
        gl.Enable(GLEnum.Blend);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ScreenRenderTexture.Dispose();

        if (_initialized)
        {
            var gl = _window.GL;
            gl.DeleteProgram(_shader);
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        Initialize();
        _initialized = true;
    }

    private void Initialize()
    {
        var gl = _window.GL;

        // シェーダーコンパイル
        var vsh = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.blit.vert"));
        gl.CompileShader(vsh);
        var vshLog = gl.GetShaderInfoLog(vsh);
        if (!string.IsNullOrWhiteSpace(vshLog))
            LogHelper.Bug($"Blit vertex shader compilation error: {vshLog}");

        var fsh = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.blit.frag"));
        gl.CompileShader(fsh);
        var fshLog = gl.GetShaderInfoLog(fsh);
        if (!string.IsNullOrWhiteSpace(fshLog))
            LogHelper.Bug($"Blit fragment shader compilation error: {fshLog}");

        _shader = gl.CreateProgram();
        gl.AttachShader(_shader, vsh);
        gl.AttachShader(_shader, fsh);
        gl.LinkProgram(_shader);
        var linkLog = gl.GetProgramInfoLog(_shader);
        if (!string.IsNullOrWhiteSpace(linkLog))
            LogHelper.Bug($"Blit shader linking error: {linkLog}");

        gl.DetachShader(_shader, vsh);
        gl.DetachShader(_shader, fsh);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        _uScreenTexture = gl.GetUniformLocation(_shader, "uScreenTexture");

        // NDC フルスクリーンクワッド (TriangleStrip): pos(x,y) + uv(u,v)
        Span<float> vertices =
        [
            -1.0f,  1.0f, 0.0f, 1.0f, // 左上
            -1.0f, -1.0f, 0.0f, 0.0f, // 左下
             1.0f,  1.0f, 1.0f, 1.0f, // 右上
             1.0f, -1.0f, 1.0f, 0.0f, // 右下
        ];

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

        // 頂点座標属性
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);

        // テクスチャ座標属性
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);
    }
}
