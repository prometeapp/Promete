using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Promete.Backends.GL;
using Promete.Internal;
using Promete.Nodes;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL;

/// <summary>
/// <see cref="MaskedContainer"/> のアルファブレンディング方式でのレンダリングを支援するヘルパークラスです。
/// </summary>
public class GLMaskedContainerHelper(PrometeApp app, RenderCommandQueue queue, IRenderTextureProvider renderTextureProvider) : IDisposable
{
    // MaskedContainer ごとの RenderTexture キャッシュ
    private readonly Dictionary<MaskedContainer, RenderTexture> _renderTextureCache = [];

    private bool _initialized;
    private uint _maskShader;
    private uint _stencilShader; // ステンシルバッファ書き込み用シェーダー
    private int _uMaskModel, _uMaskProjection, _uContent, _uMask, _uMaskTintColor;
    private int _uStencilModel, _uStencilProjection, _uStencilTexture0, _uStencilTintColor;
    private uint _vao, _vbo, _ebo;

    /// <summary>
    /// 全てのキャッシュをクリアします。
    /// </summary>
    public void Dispose()
    {
        // RenderTexture キャッシュを解放
        foreach (var rt in _renderTextureCache.Values)
            rt.Dispose();
        _renderTextureCache.Clear();

        if (!_initialized) return;
        var gl = ((OpenGLDesktopGameView)app.View).GL;

        // シェーダーとバッファを削除
        gl.DeleteProgram(_maskShader);
        gl.DeleteProgram(_stencilShader);
        gl.DeleteVertexArray(_vao);
        gl.DeleteBuffer(_vbo);
        gl.DeleteBuffer(_ebo);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        Initialize();
        _initialized = true;
    }

    private void Initialize()
    {
        var gl = ((OpenGLDesktopGameView)app.View).GL;

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

        // uniform location をキャッシュ (_maskShader)
        _uMaskModel = gl.GetUniformLocation(_maskShader, "uModel");
        _uMaskProjection = gl.GetUniformLocation(_maskShader, "uProjection");
        _uContent = gl.GetUniformLocation(_maskShader, "uContent");
        _uMask = gl.GetUniformLocation(_maskShader, "uMask");
        _uMaskTintColor = gl.GetUniformLocation(_maskShader, "uTintColor");

        // uniform location をキャッシュ (_stencilShader)
        _uStencilModel = gl.GetUniformLocation(_stencilShader, "uModel");
        _uStencilProjection = gl.GetUniformLocation(_stencilShader, "uProjection");
        _uStencilTexture0 = gl.GetUniformLocation(_stencilShader, "uTexture0");
        _uStencilTintColor = gl.GetUniformLocation(_stencilShader, "uTintColor");
    }

    /// <summary>
    /// MaskedContainerの子要素を独自のフレームバッファにレンダリングし、テクスチャを返します。
    /// </summary>
    public Texture2D RenderToTexture(MaskedContainer container, RenderContext ctx)
    {
        EnsureInitialized();
        var size = container.Size;

        // サイズが0以下の場合はデフォルトサイズを使用
        if (size.X <= 0 || size.Y <= 0)
        {
            size = new VectorInt(1, 1);
        }

        // RenderTexture を取得または作成
        if (!_renderTextureCache.TryGetValue(container, out var rt))
        {
            rt = renderTextureProvider.Create(size);
            _renderTextureCache[container] = rt;
        }
        else if (rt.Size != size)
        {
            rt.Resize(size);
        }

        // キャプチャスコープ（例外安全）
        using var capture = rt.BeginCapture(Color.Transparent);

        // 子要素を相対座標でレンダリングするため、一時的にMaskedContainerの変換を除去
        var originalLocation = container.Location;
        var originalAngle = container.Angle;
        var originalScale = container.Scale;
        var originalParent = container.Parent;

        // MaskedContainerを一時的に原点に配置（Y軸を反転してOpenGLの座標系に合わせる）
        container.Parent = null; // 親の影響を除去
        container.Location = (0, size.Y); // Y軸の原点を下に移動
        container.Angle = 0.Degrees;
        container.Scale = (1, -1); // Y軸を反転

        // 子要素のModelMatrixを再計算させる
        container.BeforeRender();
        var sorted = container.sortedChildren;
        foreach (var child in sorted)
        {
            child.BeforeRender();
        }

        // 子要素をコマンドキュー経由でレンダリング（スコープで外側を保護）
        queue.PushScope();
        foreach (var child in sorted)
            app.CollectNode(child, queue, ctx);
        queue.PopScopeAndFlush();

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

        return rt.Texture;
    }

    /// <summary>
    /// ステンシルバッファにマスクテクスチャを描画します。
    /// </summary>
    public unsafe void DrawMaskToStencil(Texture2D maskTexture, Node node)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        EnsureInitialized();
        var gl = ((OpenGLDesktopGameView)app.View).GL;

        // モデル行列を計算
        var size = node.Size;
        var modelMatrix =
            Matrix4x4.CreateScale(new Vector3(size.X, size.Y, 1))
            * node.ModelMatrix;

        // ビューポートの大きさを取得する
        var viewport = GLHelper.GetViewport(gl);

        // プロジェクション行列を計算
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, viewport.X, viewport.Y, 0, 0.1f, 100f);

        // ステンシル書き込み用シェーダーを使用
        gl.UseProgram(_stencilShader);

        // テクスチャをバインド
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)maskTexture.Handle);

        // Uniformを設定
        gl.UniformMatrix4(_uStencilModel, 1, false, (float*)&modelMatrix);
        gl.UniformMatrix4(_uStencilProjection, 1, false, (float*)&projectionMatrix);
        gl.Uniform1(_uStencilTexture0, 0);
        gl.Uniform4(_uStencilTintColor, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

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
        EnsureInitialized();
        var gl = ((OpenGLDesktopGameView)app.View).GL;

        // モデル行列を計算
        var size = node.Size;
        var modelMatrix =
            Matrix4x4.CreateScale(new Vector3(size.X, size.Y, 1))
            * node.ModelMatrix;

        // ビューポートの大きさを取得する
        var viewport = GLHelper.GetViewport(gl);

        // プロジェクション行列を計算
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, viewport.X, viewport.Y, 0, 0.1f, 100f);

        // ブレンド設定
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, // RGB
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
        gl.UniformMatrix4(_uMaskModel, 1, false, (float*)&modelMatrix);
        gl.UniformMatrix4(_uMaskProjection, 1, false, (float*)&projectionMatrix);
        gl.Uniform1(_uContent, 0);
        gl.Uniform1(_uMask, 1);
        // MaskedContainerは常に白でレンダリング（子要素のTintColorはそのまま保持）
        gl.Uniform4(_uMaskTintColor, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

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
}
