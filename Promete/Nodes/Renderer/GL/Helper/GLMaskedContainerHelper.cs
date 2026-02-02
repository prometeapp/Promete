using System;
using System.Collections.Generic;
using System.Numerics;
using Promete.Graphics;
using Promete.Internal;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Helper;

/// <summary>
/// <see cref="MaskedContainer"/> のアルファブレンディング方式でのレンダリングを支援するヘルパークラスです。
/// </summary>
public class GLMaskedContainerHelper : IDisposable
{
    private readonly OpenGLDesktopWindow _window;
    private readonly IFrameBufferProvider _fbProvider;
    private readonly uint _maskShader;
    private readonly uint _stencilShader; // ステンシルバッファ書き込み用シェーダー
    private readonly uint _vao, _vbo, _ebo;

    // 独自のフレームバッファキャッシュ（OpenGLのFBO、RBO、テクスチャ）
    private readonly Dictionary<MaskedContainer, (uint fbo, uint rbo, uint texture, VectorInt size)> _glFrameBufferCache = [];

    public GLMaskedContainerHelper(IWindow window, IFrameBufferProvider fbProvider)
    {
        _window = window as OpenGLDesktopWindow ??
                  throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");
        _fbProvider = fbProvider;

        var gl = _window.GL;

        // マスク適用用のシェーダーをコンパイル
        var vsh = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.masked.vert"));
        gl.CompileShader(vsh);

        // コンパイルエラーチェック
        var vshLog = gl.GetShaderInfoLog(vsh);
        if (!string.IsNullOrWhiteSpace(vshLog))
        {
            LogHelper.Bug($"Vertex shader compilation error: {vshLog}");
        }

        var fsh = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.masked.frag"));
        gl.CompileShader(fsh);

        // コンパイルエラーチェック
        var fshLog = gl.GetShaderInfoLog(fsh);
        if (!string.IsNullOrWhiteSpace(fshLog))
        {
            LogHelper.Bug($"Fragment shader compilation error: {fshLog}");
        }

        _maskShader = gl.CreateProgram();
        gl.AttachShader(_maskShader, vsh);
        gl.AttachShader(_maskShader, fsh);
        gl.LinkProgram(_maskShader);

        // リンクエラーチェック
        var linkLog = gl.GetProgramInfoLog(_maskShader);
        if (!string.IsNullOrWhiteSpace(linkLog))
        {
            LogHelper.Bug($"Shader program linking error: {linkLog}");
        }

        gl.DetachShader(_maskShader, vsh);
        gl.DetachShader(_maskShader, fsh);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        // ステンシル書き込み用のシェーダーをコンパイル
        var svsh = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(svsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.texture.vert"));
        gl.CompileShader(svsh);

        var svshLog = gl.GetShaderInfoLog(svsh);
        if (!string.IsNullOrWhiteSpace(svshLog))
        {
            LogHelper.Bug($"Stencil vertex shader compilation error: {svshLog}");
        }

        var sfsh = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(sfsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.stencil_mask.frag"));
        gl.CompileShader(sfsh);

        var sfshLog = gl.GetShaderInfoLog(sfsh);
        if (!string.IsNullOrWhiteSpace(sfshLog))
        {
            LogHelper.Bug($"Stencil fragment shader compilation error: {sfshLog}");
        }

        _stencilShader = gl.CreateProgram();
        gl.AttachShader(_stencilShader, svsh);
        gl.AttachShader(_stencilShader, sfsh);
        gl.LinkProgram(_stencilShader);

        var slinkLog = gl.GetProgramInfoLog(_stencilShader);
        if (!string.IsNullOrWhiteSpace(slinkLog))
        {
            LogHelper.Bug($"Stencil shader program linking error: {slinkLog}");
        }

        gl.DetachShader(_stencilShader, svsh);
        gl.DetachShader(_stencilShader, sfsh);
        gl.DeleteShader(svsh);
        gl.DeleteShader(sfsh);

        // 四角形の頂点データを準備
        Span<float> vertices =
        [
            1.0f, 0.0f, 1.0f, 0.0f, // 右下
            1.0f, 1.0f, 1.0f, 1.0f, // 右上
            0.0f, 1.0f, 0.0f, 1.0f, // 左上
            0.0f, 0.0f, 0.0f, 0.0f, // 左下
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

        // インデックスバッファ
        _ebo = gl.GenBuffer();
        Span<uint> indices = [0, 1, 3, 1, 2, 3];
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
    }

    /// <summary>
    /// MaskedContainerの子要素を独自のフレームバッファにレンダリングし、テクスチャを返します。
    /// </summary>
    public Texture2D RenderToTexture(MaskedContainer container)
    {
        var size = container.Size;

        // サイズが0以下の場合はデフォルトサイズを使用
        if (size.X <= 0 || size.Y <= 0)
        {
            size = new VectorInt(1, 1);
        }

        var gl = _window.GL;

        // フレームバッファを取得または作成
        uint fbo, rbo, textureId;
        if (_glFrameBufferCache.TryGetValue(container, out var cached))
        {
            if (cached.size == size)
            {
                // サイズが同じ場合は既存のものを使用
                fbo = cached.fbo;
                rbo = cached.rbo;
                textureId = cached.texture;
            }
            else
            {
                // サイズが変わった場合は古いものを削除して新規作成
                gl.DeleteFramebuffer(cached.fbo);
                gl.DeleteRenderbuffer(cached.rbo);
                gl.DeleteTexture(cached.texture);

                (fbo, rbo, textureId) = CreateFrameBuffer(gl, size);
                _glFrameBufferCache[container] = (fbo, rbo, textureId, size);
            }
        }
        else
        {
            // 新しいフレームバッファを作成
            (fbo, rbo, textureId) = CreateFrameBuffer(gl, size);
            _glFrameBufferCache[container] = (fbo, rbo, textureId, size);
        }

        // 現在のビューポートとフレームバッファを保存
        var previousViewport = GLHelper.GetViewport(gl);
        var previousFrameBuffer = gl.GetInteger(GLEnum.FramebufferBinding);

        // フレームバッファにバインド
        gl.BindFramebuffer(GLEnum.Framebuffer, fbo);

        // ビューポートを設定
        gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);

        // 背景をクリア（透明）
        gl.ClearColor(0, 0, 0, 0);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 子要素を相対座標でレンダリングするため、一時的にMaskedContainerの変換を除去
        var originalLocation = container.Location;
        var originalAngle = container.Angle;
        var originalScale = container.Scale;
        var originalParent = container.Parent;

        // MaskedContainerを一時的に原点に配置（Y軸を反転してOpenGLの座標系に合わせる）
        container.Parent = null; // 親の影響を除去
        container.Location = (0, size.Y); // Y軸の原点を下に移動
        container.Angle = 0;
        container.Scale = (1, -1); // Y軸を反転

        // 子要素のModelMatrixを再計算させる
        container.BeforeRender();
        var sorted = container.sortedChildren;
        foreach (var child in sorted)
        {
            child.BeforeRender();
        }

        // 子要素を直接レンダリング
        var app = PrometeApp.Current;
        foreach (var child in sorted)
        {
            app.RenderNode(child);
        }

        // MaskedContainerの状態を元に戻す
        container.Parent = originalParent;
        container.Location = originalLocation;
        container.Angle = originalAngle;
        container.Scale = originalScale;

        // ModelMatrixを再計算
        container.BeforeRender();
        foreach (var child in sorted)
        {
            child.BeforeRender();
        }

        // フレームバッファのバインドを解除
        gl.BindFramebuffer(GLEnum.Framebuffer, (uint)previousFrameBuffer);

        // ビューポートを元に戻す
        gl.Viewport(0, 0, (uint)previousViewport.X, (uint)previousViewport.Y);

        // テクスチャを返す（Disposeは不要、キャッシュで管理）
        return new Texture2D((int)textureId, size, _ => { });
    }

    /// <summary>
    /// OpenGLのフレームバッファを作成します。
    /// </summary>
    private unsafe (uint fbo, uint rbo, uint texture) CreateFrameBuffer(Silk.NET.OpenGL.GL gl, VectorInt size)
    {
        // テクスチャを作成
        var texture = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, texture);
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba, (uint)size.X, (uint)size.Y, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        gl.BindTexture(GLEnum.Texture2D, 0);

        // レンダーバッファを作成（デプスバッファ用）
        var rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
        gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.DepthComponent24, (uint)size.X, (uint)size.Y);
        gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

        // フレームバッファを作成
        var fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
        gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, texture, 0);
        gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthAttachment, GLEnum.Renderbuffer, rbo);

        // フレームバッファの状態をチェック
        var status = gl.CheckFramebufferStatus(GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            throw new InvalidOperationException($"フレームバッファが不完全です: {status}");
        }

        gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        return (fbo, rbo, texture);
    }

    /// <summary>
    /// ステンシルバッファにマスクテクスチャを描画します。
    /// </summary>
    public unsafe void DrawMaskToStencil(Texture2D maskTexture, Node node)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        var gl = _window.GL;

        // モデル行列を計算
        var size = node.Size;
        var modelMatrix =
            Matrix4x4.CreateScale(new Vector3(size.X, size.Y, 1))
            * node.ModelMatrix;

        // ビューポートの大きさを取得する
        var viewport = GLHelper.GetViewport(gl);

        // フレームバッファが0の場合は、ウィンドウのスケールを反映する
        var currentFrameBufferId = gl.GetInteger(GLEnum.FramebufferBinding);
        if (currentFrameBufferId == 0)
        {
            viewport /= _window.Scale;
        }

        // プロジェクション行列を計算
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, viewport.X, viewport.Y, 0, 0.1f, 100f);

        // ステンシル書き込み用シェーダーを使用
        gl.UseProgram(_stencilShader);

        // テクスチャをバインド
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)maskTexture.Handle);

        // Uniformを設定
        var uModel = gl.GetUniformLocation(_stencilShader, "uModel");
        gl.UniformMatrix4(uModel, 1, false, (float*)&modelMatrix);

        var uProjection = gl.GetUniformLocation(_stencilShader, "uProjection");
        gl.UniformMatrix4(uProjection, 1, false, (float*)&projectionMatrix);

        var uTexture0 = gl.GetUniformLocation(_stencilShader, "uTexture0");
        gl.Uniform1(uTexture0, 0);

        var uTintColor = gl.GetUniformLocation(_stencilShader, "uTintColor");
        gl.Uniform4(uTintColor, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        // 描画
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        gl.BindVertexArray(0);

        // テクスチャのバインドを解除
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// マスクを適用してテクスチャを描画します。
    /// </summary>
    public unsafe void DrawMasked(Texture2D contentTexture, Texture2D maskTexture, Node node)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        var gl = _window.GL;

        // モデル行列を計算
        var size = node.Size;
        var modelMatrix =
            Matrix4x4.CreateScale(new Vector3(size.X, size.Y, 1))
            * node.ModelMatrix;

        // ビューポートの大きさを取得する
        var viewport = GLHelper.GetViewport(gl);

        // フレームバッファが0の場合は、ウィンドウのスケールを反映する
        var currentFrameBufferId = gl.GetInteger(GLEnum.FramebufferBinding);
        if (currentFrameBufferId == 0)
        {
            viewport /= _window.Scale;
        }

        // プロジェクション行列を計算
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, viewport.X, viewport.Y, 0, 0.1f, 100f);

        // ブレンド設定
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,  // RGB
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha // Alpha
        );

        // シェーダーを使用
        gl.UseProgram(_maskShader);

        // コンテンツテクスチャをテクスチャユニット0にバインド
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)contentTexture.Handle);

        // マスクテクスチャをテクスチャユニット1にバインド
        gl.ActiveTexture(TextureUnit.Texture1);
        gl.BindTexture(TextureTarget.Texture2D, (uint)maskTexture.Handle);

        // Uniformを設定
        var uModel = gl.GetUniformLocation(_maskShader, "uModel");
        gl.UniformMatrix4(uModel, 1, false, (float*)&modelMatrix);

        var uProjection = gl.GetUniformLocation(_maskShader, "uProjection");
        gl.UniformMatrix4(uProjection, 1, false, (float*)&projectionMatrix);

        var uContent = gl.GetUniformLocation(_maskShader, "uContent");
        gl.Uniform1(uContent, 0);

        var uMask = gl.GetUniformLocation(_maskShader, "uMask");
        gl.Uniform1(uMask, 1);

        // MaskedContainerは常に白でレンダリング（子要素のTintColorはそのまま保持）
        var uTintColor = gl.GetUniformLocation(_maskShader, "uTintColor");
        gl.Uniform4(uTintColor, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        // 描画
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        gl.BindVertexArray(0);

        // テクスチャのバインドを解除
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, 0);
        gl.ActiveTexture(TextureUnit.Texture1);
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// 全てのキャッシュをクリアします。
    /// </summary>
    public void Dispose()
    {
        var gl = _window.GL;

        // 全てのフレームバッファを破棄
        foreach (var (_, (fbo, rbo, texture, _)) in _glFrameBufferCache)
        {
            gl.DeleteFramebuffer(fbo);
            gl.DeleteRenderbuffer(rbo);
            gl.DeleteTexture(texture);
        }
        _glFrameBufferCache.Clear();

        // シェーダーとバッファを削除
        gl.DeleteProgram(_maskShader);
        gl.DeleteProgram(_stencilShader);
        gl.DeleteVertexArray(_vao);
        gl.DeleteBuffer(_vbo);
        gl.DeleteBuffer(_ebo);
    }
}
