#pragma warning disable RECS0018 // 等値演算子による浮動小数点値の比較
using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="DrawPieTextureCommand"/> を実行するランナーです。
/// </summary>
public class GLDrawPieTextureCommandRunner(IWindow window) : CommandRunner<DrawPieTextureCommand>
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow ??
                                                   throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");
    private bool _initialized;
    private uint _shader;
    private int _uModel, _uProjection, _uTexture0, _uTintColor, _uStartAngle, _uEndAngle;
    private uint _vbo, _vao, _ebo;

    public override void Execute(DrawPieTextureCommand command)
    {
        Draw(command.Texture, command.ModelMatrix, command.TintColor, command.Width, command.Height, command.StartPercent, command.Percent, command.Material);
    }

    /// <summary>
    /// PieSpriteを画面上に描画します。
    /// </summary>
    /// <param name="texture">描画対象のテクスチャ。</param>
    /// <param name="modelMatrix">モデル行列。</param>
    /// <param name="color">テクスチャに反映するティントカラー。</param>
    /// <param name="width">描画幅。</param>
    /// <param name="height">描画高さ。</param>
    /// <param name="startPercent">描画開始位置のパーセント（0.0 ~ 100.0）。</param>
    /// <param name="percent">描画終了位置のパーセント（0.0 ~ 100.0）。</param>
    /// <param name="material">適用するマテリアル。null の場合はデフォルトシェーダーを使用します。</param>
    public unsafe void Draw(Texture2D texture, Matrix4x4 modelMatrix, Color color, float width, float height, float startPercent, float percent, Material? material = null)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        EnsureInitialized();
        var gl = _window.GL;
        var c = color;

        // モデル行列を計算
        modelMatrix =
            Matrix4x4.CreateScale(new Vector3(width, height, 1))
            * Matrix4x4.CreateTranslation(new Vector3(Vector.Zero.ToNumerics(), 0))
            * modelMatrix;

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

        // パーセント→ラジアン変換（12時方向を0%にするため-90度オフセット）
        var startAngle = (startPercent / 100.0f * 360.0f - 90.0f) * MathF.PI / 180.0f;
        var endAngle = (percent / 100.0f * 360.0f - 90.0f) * MathF.PI / 180.0f;

        // 描画開始
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, // RGB
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha // Alpha
        );

        // シェーダー選択: カスタムマテリアルがある場合はそのプログラムを使用
        var program = material is { } mat ? (uint)mat.Shader.Handle : _shader;
        gl.UseProgram(program);

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)texture.Handle);

        if (material is not null)
        {
            // カスタムシェーダー: ロケーションをキャッシュ付きで取得 (-1 はスキップ)
            var uModel = GLMaterialApplier.GetLocation(gl, program, "uModel");
            var uProj = GLMaterialApplier.GetLocation(gl, program, "uProjection");
            var uTex = GLMaterialApplier.GetLocation(gl, program, "uTexture0");
            var uTint = GLMaterialApplier.GetLocation(gl, program, "uTintColor");
            var uStart = GLMaterialApplier.GetLocation(gl, program, "uStartAngle");
            var uEnd = GLMaterialApplier.GetLocation(gl, program, "uEndAngle");
            if (uModel >= 0) gl.UniformMatrix4(uModel, 1, false, (float*)&modelMatrix);
            if (uProj >= 0) gl.UniformMatrix4(uProj, 1, false, (float*)&projectionMatrix);
            if (uTex >= 0) gl.Uniform1(uTex, 0);
            if (uTint >= 0) gl.Uniform4(uTint, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));
            if (uStart >= 0) gl.Uniform1(uStart, startAngle);
            if (uEnd >= 0) gl.Uniform1(uEnd, endAngle);
            GLMaterialApplier.Apply(gl, program, material);
        }
        else
        {
            // デフォルトシェーダー: キャッシュ済みロケーションを使用
            gl.UniformMatrix4(_uModel, 1, false, (float*)&modelMatrix);
            gl.UniformMatrix4(_uProjection, 1, false, (float*)&projectionMatrix);
            gl.Uniform1(_uTexture0, 0);
            gl.Uniform4(_uTintColor, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));
            gl.Uniform1(_uStartAngle, startAngle);
            gl.Uniform1(_uEndAngle, endAngle);
        }

        // 描画
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        gl.BindVertexArray(0);
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

        // 頂点シェーダーをリソースから読み込んでコンパイルする
        var vsh = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.pie.vert"));
        gl.CompileShader(vsh);

        // フラグメントシェーダーをリソースから読み込んでコンパイルする
        var fsh = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.pie.frag"));
        gl.CompileShader(fsh);

        // コンパイルした2つのシェーダーをリンクする
        _shader = gl.CreateProgram();
        gl.AttachShader(_shader, vsh);
        gl.AttachShader(_shader, fsh);
        gl.LinkProgram(_shader);

        // シェーダーのリンクが終わったので、不要なリソースを解放
        gl.DetachShader(_shader, vsh);
        gl.DetachShader(_shader, fsh);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        // スプライトは基本のポリゴンが四角形に決まっているので、あらかじめ頂点情報を用意しておく
        Span<float> vertices =
        [
            1.0f, 0.0f, 1.0f, 0.0f, // 右下
            1.0f, 1.0f, 1.0f, 1.0f, // 右上
            0.0f, 1.0f, 0.0f, 1.0f, // 左上
            0.0f, 0.0f, 0.0f, 0.0f  // 左下
        ];

        // バッファに頂点情報を書き込む
        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);
        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

        // 4つのfloat値のうち、最初の2つを頂点の座標として登録する
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);

        // 4つのfloat値のうち、次の2つをテクスチャ座標として登録する
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        // 頂点数を減らすため、インデックスバッファを用意する
        _ebo = gl.GenBuffer();
        Span<uint> indices = [0, 1, 3, 1, 2, 3];
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

        // uniform location をキャッシュ
        _uModel = gl.GetUniformLocation(_shader, "uModel");
        _uProjection = gl.GetUniformLocation(_shader, "uProjection");
        _uTexture0 = gl.GetUniformLocation(_shader, "uTexture0");
        _uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
        _uStartAngle = gl.GetUniformLocation(_shader, "uStartAngle");
        _uEndAngle = gl.GetUniformLocation(_shader, "uEndAngle");
    }
}
