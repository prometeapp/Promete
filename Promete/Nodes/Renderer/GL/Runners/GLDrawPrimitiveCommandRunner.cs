using System;
using System.Drawing;
using System.Numerics;
using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="DrawPrimitiveCommand"/> でプリミティブ図形を描画するランナーです。
/// </summary>
public class GLDrawPrimitiveCommandRunner(IWindow window) : CommandRunner<DrawPrimitiveCommand>
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow ??
                                                   throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");
    private uint _ebo;
    private bool _initialized;
    private uint _shader;
    private int _uTintColor;
    private uint _vao;
    private uint _vbo;

    public override void Execute(DrawPrimitiveCommand command)
    {
        Draw(command.WorldVertices, command.ShapeType, command.Color, command.LineWidth, command.LineColor, command.Material);
    }

    /// <summary>
    /// 図形の描画を行います。
    /// </summary>
    /// <param name="worldVertices">頂点（ワールド座標）</param>
    /// <param name="type">図形のタイプ</param>
    /// <param name="color">図形の塗りつぶし色</param>
    /// <param name="lineWidth">線の幅。GPUによってはサポートされません。</param>
    /// <param name="lineColor">線の色。</param>
    /// <param name="material">適用するマテリアル。null の場合はデフォルトシェーダーを使用します。</param>
    public unsafe void Draw(Span<Vector> worldVertices, ShapeType type, Color color, int lineWidth = 0,
        Color? lineColor = null, Material? material = null)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        if (worldVertices.Length == 0)
            return;

        EnsureInitialized();
        var gl = _window.GL;

        // ビューポートの大きさを取得する
        var viewport = GLHelper.GetViewport(gl);

        // フレームバッファが0の場合は、ウィンドウのスケールを反映する
        var currentFrameBufferId = gl.GetInteger(GLEnum.FramebufferBinding);
        if (currentFrameBufferId == 0)
        {
            viewport /= _window.Scale;
        }

        // 図形の頂点を、ワールド座標からビューポート座標に変換（事前変換済みなのでPixelRatioを乗算するだけ）
        Span<float> vertices = stackalloc float[worldVertices.Length * 2];
        for (var i = 0; i < worldVertices.Length; i++)
        {
            var vertex = worldVertices[i] * _window.PixelRatio;

            var (x, y) = vertex.ToViewportPoint(viewport.X / 2, viewport.Y / 2);
            vertices[i * 2 + 0] = x;
            vertices[i * 2 + 1] = y;
        }

        // シェーダー選択: カスタムマテリアルがある場合はそのプログラムを使用
        var program = material is { } mat ? (uint)mat.Shader.Handle : _shader;

        // 描画開始
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, // RGB
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha // Alpha
        );

        DrawFill(vertices, type, color, lineWidth, program, material);
        DrawStroke(vertices, lineWidth, lineColor, program, material);

        gl.Disable(EnableCap.Blend);
    }

    /// <summary>
    /// 図形の線を描画します。
    /// </summary>
    private unsafe void DrawStroke(Span<float> vertices, int lineWidth, Color? lineColor, uint program, Material? material)
    {
        if (lineWidth <= 0 || lineColor is not { } lc) return;
        var gl = _window.GL;

        gl.LineWidth(lineWidth);

        // すべての頂点データをバッファに書き込む
        gl.BindVertexArray(_vao);
        gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        gl.BufferData<float>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);

        // シェーダーを利用する
        gl.UseProgram(program);

        // 頂点属性を設定
        gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
        gl.EnableVertexAttribArray(0);

        // シェーダーに線の色を渡す
        var tintLoc = material is not null ? GLMaterialApplier.GetLocation(gl, program, "uTintColor") : _uTintColor;
        if (tintLoc >= 0)
            gl.Uniform4(tintLoc, new Vector4(lc.R / 255f, lc.G / 255f, lc.B / 255f, lc.A / 255f));

        if (material is not null)
            GLMaterialApplier.Apply(gl, program, material);

        // 描画
        gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)vertices.Length / 2);
    }

    /// <summary>
    /// 図形の塗りつぶし領域を描画します。
    /// </summary>
    private unsafe void DrawFill(Span<float> vertices, ShapeType type, Color color, int lineWidth, uint program, Material? material)
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
        gl.UseProgram(program);

        // 頂点属性を設定
        gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
        gl.EnableVertexAttribArray(0);

        // シェーダーに色データを渡す
        var tintLoc = material is not null ? GLMaterialApplier.GetLocation(gl, program, "uTintColor") : _uTintColor;
        if (tintLoc >= 0)
            gl.Uniform4(tintLoc, new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));

        if (material is not null)
            GLMaterialApplier.Apply(gl, program, material);

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

        // uniform location をキャッシュ
        _uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
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
