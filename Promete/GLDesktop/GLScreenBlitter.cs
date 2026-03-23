using System;
using System.Collections.Generic;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.GLDesktop;

/// <summary>
/// 全描画を一度スクリーンサイズの <see cref="RenderTexture"/> にキャプチャし、
/// ポストプロセスを適用した後にデフォルト FBO (画面) へブリットするクラスです。
/// </summary>
internal sealed class GLScreenBlitter : IScreenBlitter, IDisposable
{
    /// <summary>
    /// 全描画のキャプチャ先 RenderTexture を取得します。
    /// </summary>
    public RenderTexture ScreenRenderTexture { get; private set; } = null!;

    private readonly OpenGLDesktopGameView _view;
    private readonly IRenderTextureProvider _provider;

    private Material _defaultMaterial = null!;

    // フルスクリーンクワッド
    private uint _vao, _vbo;

    // ピンポンバッファ（複数パス時に遅延生成）
    private RenderTexture? _pingPong0;
    private RenderTexture? _pingPong1;

    private bool _initialized;
    private bool _disposed;

    public GLScreenBlitter(IGameView view, IRenderTextureProvider provider)
    {
        _view = (OpenGLDesktopGameView)view;
        _provider = provider;
        _view.Resize += OnViewResize;
    }

    public void InitializeScreenRenderTexture()
    {
        ScreenRenderTexture = _provider.Create(_view.Size);
    }

    /// <summary>
    /// ポストプロセスマテリアルを順番に適用してスクリーンへブリットします。
    /// </summary>
    /// <param name="materials">
    /// 適用するマテリアルのリスト。空の場合はデフォルトシェーダーで直接ブリットします。
    /// 各マテリアルのシェーダーは <c>uScreenTexture</c>（sampler2D, slot 0）で前パスの結果を参照できます。
    /// </param>
    public void BlitToScreen(IReadOnlyList<Material> materials)
    {
        EnsureInitialized();
        var gl = _view.GL;
        gl.Disable(GLEnum.Blend);

        EnsurePingPongBuffers();
        var src = ScreenRenderTexture;
        Span<RenderTexture> pingPongs = [_pingPong0!, _pingPong1!];
        var pingIdx = 0;

        // 2枚のバッファを交互に参照して描画。マテリアル数が0なら実行されない
        foreach (var t in materials)
        {
            var dst = pingPongs[pingIdx];
            using var capture = dst.BeginCapture();
            BlitQuad(gl, src, material: t);
            src = dst;
            pingIdx = 1 - pingIdx;
        }

        // バッファへの描画結果をスクリーンへ描画
        gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        var size = _view.ActualSize;
        gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        BlitQuad(gl, src, material: _defaultMaterial);

        gl.Enable(GLEnum.Blend);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _view.Resize -= OnViewResize;
        ScreenRenderTexture.Dispose();
        _pingPong0?.Dispose();
        _pingPong1?.Dispose();

        if (_initialized)
        {
            var gl = _view.GL;
            _defaultMaterial.Shader.Dispose();
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
        }
    }

    // --- private ---

    private void BlitQuad(GL gl, RenderTexture src, Material material)
    {
        var program = (uint)material.Shader.Handle;
        gl.UseProgram(program);

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(GLEnum.Texture2D, (uint)src.Texture.Handle);

        var uLoc = GLMaterialApplier.GetLocation(gl, program, "uScreenTexture");
        if (uLoc >= 0) gl.Uniform1(uLoc, 0);

        GLMaterialApplier.Apply(gl, program, material, firstTextureSlot: 1);

        gl.BindVertexArray(_vao);
        gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        gl.BindVertexArray(0);

        gl.BindTexture(GLEnum.Texture2D, 0);
    }

    private void EnsurePingPongBuffers()
    {
        var size = ScreenRenderTexture.Size;
        _pingPong0 ??= _provider.Create(size);
        _pingPong1 ??= _provider.Create(size);
    }

    private void OnViewResize()
    {
        var size = _view.Size;
        ScreenRenderTexture.Resize(size);
        _pingPong0?.Resize(size);
        _pingPong1?.Resize(size);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        Initialize();
        _initialized = true;
    }

    private void Initialize()
    {
        var gl = _view.GL;

        var shader = ShaderProgram.Create()
            .Vertex(EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.blit.vert"))
            .Fragment(EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.blit.frag"))
            .Compile();

        _defaultMaterial = new Material(shader);


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

        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);
    }
}
