using System;
using System.Drawing;
using System.Numerics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Helper;

/// <summary>
/// プリミティブ図形の描画関数をまとめています。
/// </summary>
public class GLPrimitiveRendererHelper
{
    /// <summary>
    /// シェーダーへのハンドル
    /// </summary>
    private readonly uint _shader;

    /// <summary>
    /// Element Buffer Object
    /// </summary>
    private readonly uint _ebo;

    /// <summary>
    /// Vertex Array Object
    /// </summary>
    private readonly uint _vao;

    /// <summary>
    /// Vertex Buffer Object
    /// </summary>
    private readonly uint _vbo;

    /// <summary>
    /// 描画対象のウィンドウ
    /// </summary>
    private readonly OpenGLDesktopWindow _window;

    public GLPrimitiveRendererHelper(IWindow window)
    {
        _window = window as OpenGLDesktopWindow ??
                  throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");
        var gl = _window.GL;

        // 頂点シェーダーをリソースから読み込んでコンパイルする
        var vsh = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.primitive.vert"));
        gl.CompileShader(vsh);

        // フラグメントシェーダーをリソースから読み込んでコンパイルする
        var fsh = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.primitive.frag"));
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

        // VAO, VBO, EBOを生成
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();
    }

    /// <summary>
    /// 図形の描画を行います。
    /// </summary>
    /// <param name="node">図形を表現するノード。</param>
    /// <param name="worldVertices">頂点（ワールド座標）</param>
    /// <param name="type">図形のタイプ</param>
    /// <param name="color">図形の塗りつぶし色</param>
    /// <param name="lineWidth">線の幅。GPUによってはサポートされません。</param>
    /// <param name="lineColor">線の色。</param>
    public unsafe void Draw(Node node, Span<VectorInt> worldVertices, ShapeType type, Color color, int lineWidth = 0,
        Color? lineColor = null)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        if (worldVertices.Length == 0)
            return;

        var gl = _window.GL;

        // ビューポートの大きさを取得する
        var viewport = RenderingHelper.GetViewport(gl);

        // 図形の頂点を、ワールド座標からビューポート座標に変換
        Span<float> vertices = stackalloc float[worldVertices.Length * 2];
        for (var i = 0; i < worldVertices.Length; i++)
        {
            var vertex = RenderingHelper.Transform(worldVertices[i], node) * _window.PixelRatio;

            var (x, y) = vertex.ToViewportPoint(viewport.X / 2, viewport.Y / 2);
            vertices[i * 2 + 0] = x;
            vertices[i * 2 + 1] = y;
        }

        // 描画開始
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,  // RGB
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha // Alpha
        );

        DrawFill(vertices, type, color, lineWidth);
        DrawStroke(vertices, lineWidth, lineColor);

        gl.Disable(EnableCap.Blend);
    }

    /// <summary>
    /// 図形の線を描画します。
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="lineWidth"></param>
    /// <param name="lineColor"></param>
    private unsafe void DrawStroke(Span<float> vertices, int lineWidth, Color? lineColor)
    {
        if (lineWidth <= 0 || lineColor is not { } lc) return;
        var gl = _window.GL;

        gl.LineWidth(lineWidth);

        // すべての頂点データをバッファに書き込む
        gl.BindVertexArray(_vao);
        gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        gl.BufferData<float>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);

        // シェーダーを利用する
        gl.UseProgram(_shader);

        // 頂点属性を設定
        gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
        gl.EnableVertexAttribArray(0);

        // シェーダーに線の色を渡す
        var uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
        gl.Uniform4(uTintColor, new Vector4(lc.R / 255f, lc.G / 255f, lc.B / 255f, lc.A / 255f));

        // 描画
        gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)vertices.Length / 2);
    }

    /// <summary>
    /// 図形の塗りつぶし領域を描画します。
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="type"></param>
    /// <param name="color"></param>
    /// <param name="lineWidth"></param>
    private unsafe void DrawFill(Span<float> vertices, ShapeType type, Color color, int lineWidth)
    {
        // 透明度が0未満の場合は、塗りつぶし領域の描画をスキップする
        if (color.A <= 0) return;

        var gl = _window.GL;
        if (type == ShapeType.Line) gl.LineWidth(lineWidth);

        // すべての頂点データをバッファに書き込む
        gl.BindVertexArray(_vao);
        gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        gl.BufferData<float>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);

        // シェーダーを利用する
        gl.UseProgram(_shader);

        // 頂点属性を設定
        gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
        gl.EnableVertexAttribArray(0);

        // シェーダーに色データを渡す
        var uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
        gl.Uniform4(uTintColor, new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));

        // 矩形の場合は、インデックスバッファを利用してドローコールを減らす
        // TODO: 他のタイプに対してもEBOを利用したい
        if (type == ShapeType.Rect)
        {
            gl.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
            Span<uint> indices =
            [
                0, 1, 2,
                0, 2, 3
            ];
            gl.BufferData<uint>(GLEnum.ElementArrayBuffer, indices, GLEnum.StaticDraw);
            gl.DrawElements(GLEnum.Triangles, (uint)indices.Length, GLEnum.UnsignedInt, null);
            return;
        }

        // 描画
        gl.DrawArrays(ToGLType(type), 0, (uint)vertices.Length / 2);
    }

    /// <summary>
    /// Prometeの<see cref="ShapeType"/>を、OpenGLの<see cref="PrimitiveType"/>に変換します。
    /// </summary>
    private static PrimitiveType ToGLType(ShapeType type)
    {
        return type switch
        {
            ShapeType.Pixel => PrimitiveType.Points,
            ShapeType.Line => PrimitiveType.Lines,
            ShapeType.Rect => PrimitiveType.TriangleStrip,
            ShapeType.Triangle => PrimitiveType.Triangles,
            ShapeType.Polygon => PrimitiveType.TriangleStrip,
            _ => throw new ArgumentException(null, nameof(type))
        };
    }
}
