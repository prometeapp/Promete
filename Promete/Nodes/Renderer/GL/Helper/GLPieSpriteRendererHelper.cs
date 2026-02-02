#pragma warning disable RECS0018 // 等値演算子による浮動小数点値の比較
using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Helper;

/// <summary>
/// <see cref="PieSprite" /> オブジェクトをバッファ上に描画する機能を提供します。
/// </summary>
public class GLPieSpriteRendererHelper
{
    private readonly uint _shader;

    private readonly uint _vbo, _vao, _ebo;

    private readonly OpenGLDesktopWindow _window;

    public GLPieSpriteRendererHelper(IWindow window)
    {
        _window = window as OpenGLDesktopWindow ??
                  throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

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
            0.0f, 0.0f, 0.0f, 0.0f // 左下
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
        // テクスチャ座標属性
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        // 頂点数を減らすため、インデックスバッファを用意する
        _ebo = gl.GenBuffer();
        Span<uint> indices = [0, 1, 3, 1, 2, 3];
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
    }

    /// <summary>
    /// PieSpriteを画面上に描画します。
    /// </summary>
    /// <param name="texture">描画対象のテクスチャ。</param>
    /// <param name="node">位置やサイズなどの情報を保持するノード。</param>
    /// <param name="color">テクスチャに反映するティントカラー。</param>
    /// <param name="startPercent">描画開始位置のパーセント（0.0 ~ 100.0）。</param>
    /// <param name="percent">描画終了位置のパーセント（0.0 ~ 100.0）。</param>
    public unsafe void Draw(Texture2D texture, Node node, Color? color, float startPercent, float percent)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        var gl = _window.GL;
        var c = color ?? Color.White;
        var finalWidth = node.Size.X;
        var finalHeight = node.Size.Y;

        // モデル行列を計算
        var modelMatrix =
            Matrix4x4.CreateScale(new Vector3(finalWidth, finalHeight, 1))
            * Matrix4x4.CreateTranslation(new Vector3(Vector.Zero.ToNumerics(), 0))
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

        // パーセント→ラジアン変換（12時方向を0%にするため-90度オフセット）
        var startAngle = (startPercent / 100.0f * 360.0f - 90.0f) * MathF.PI / 180.0f;
        var endAngle = (percent / 100.0f * 360.0f - 90.0f) * MathF.PI / 180.0f;

        // 描画開始
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,  // RGB
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha // Alpha
        );

        // シェーダーおよびテクスチャを利用する
        gl.UseProgram(_shader);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)texture.Handle);

        // シェーダーに行列情報を渡す
        var uModel = gl.GetUniformLocation(_shader, "uModel");
        gl.UniformMatrix4(uModel, 1, false, (float*)&modelMatrix);
        var uProjection = gl.GetUniformLocation(_shader, "uProjection");
        gl.UniformMatrix4(uProjection, 1, false, (float*)&projectionMatrix);
        var uTexture0 = gl.GetUniformLocation(_shader, "uTexture0");
        gl.Uniform1(uTexture0, 0);
        var uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
        gl.Uniform4(uTintColor, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));

        // パーセント角度をシェーダーに渡す
        var uStartAngle = gl.GetUniformLocation(_shader, "uStartAngle");
        gl.Uniform1(uStartAngle, startAngle);
        var uEndAngle = gl.GetUniformLocation(_shader, "uEndAngle");
        gl.Uniform1(uEndAngle, endAngle);

        // 描画
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        gl.BindVertexArray(0);
    }
}
