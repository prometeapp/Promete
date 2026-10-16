using System;
using System.Numerics;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL.Runners;

/// <summary>
/// <see cref="DrawDeformedTextureCommand"/> を実行するランナーです。
/// </summary>
public class GLDrawDeformedTextureCommandRunner(IGameView view)
    : CommandRunner<DrawDeformedTextureCommand>
{
    private readonly IGLGameView _view = (IGLGameView)view;

    private bool _initialized;
    private uint _shader;
    private int _uModel;
    private int _uProjection;
    private int _uTexture0;
    private int _uTintColor;
    private uint _vbo;
    private uint _vao;
    private uint _ebo;

    public override unsafe void Execute(DrawDeformedTextureCommand command)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        EnsureInitialized();
        var gl = _view.GL;
        var c = command.TintColor;
        var material = command.Material;

        Span<float> vertices = stackalloc float[DeformedQuadHelper.VertexFloats];
        Span<uint> indices = stackalloc uint[DeformedQuadHelper.IndexCount];
        DeformedQuadHelper.Build(command, vertices, indices);

        // 頂点はワールド座標なので、モデル行列は単位行列でよい
        var modelMatrix = Matrix4x4.Identity;
        var viewport = GLHelper.GetViewport(gl);
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0,
            viewport.X,
            viewport.Y,
            0,
            0.1f,
            100f
        );

        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha,
            BlendingFactor.OneMinusSrcAlpha, // RGB
            BlendingFactor.One,
            BlendingFactor.OneMinusSrcAlpha // Alpha
        );

        // シェーダー選択: カスタムマテリアルがある場合はそのプログラムを使用
        var program = material is { } mat ? (uint)mat.Shader.Handle : _shader;
        gl.UseProgram(program);

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)command.Texture.Handle);

        var tint = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        if (material is not null)
        {
            // カスタムシェーダー: ロケーションをキャッシュ付きで取得 (-1 はスキップ)
            var uModel = GLMaterialApplier.GetLocation(gl, program, "uModel");
            var uProj = GLMaterialApplier.GetLocation(gl, program, "uProjection");
            var uTex = GLMaterialApplier.GetLocation(gl, program, "uTexture0");
            var uTint = GLMaterialApplier.GetLocation(gl, program, "uTintColor");
            if (uModel >= 0)
                gl.UniformMatrix4(uModel, 1, false, (float*)&modelMatrix);
            if (uProj >= 0)
                gl.UniformMatrix4(uProj, 1, false, (float*)&projectionMatrix);
            if (uTex >= 0)
                gl.Uniform1(uTex, 0);
            if (uTint >= 0)
                gl.Uniform4(uTint, tint);
            GLMaterialApplier.Apply(gl, program, material);
        }
        else
        {
            gl.UniformMatrix4(_uModel, 1, false, (float*)&modelMatrix);
            gl.UniformMatrix4(_uProjection, 1, false, (float*)&projectionMatrix);
            gl.Uniform1(_uTexture0, 0);
            gl.Uniform4(_uTintColor, tint);
        }

        // 頂点は毎回変わりうるので、描画のたびに書き込む
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.DynamicDraw);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(
            BufferTargetARB.ElementArrayBuffer,
            indices,
            BufferUsageARB.DynamicDraw
        );
        gl.DrawElements(
            PrimitiveType.Triangles,
            DeformedQuadHelper.IndexCount,
            DrawElementsType.UnsignedInt,
            null
        );
        gl.BindVertexArray(0);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;
        Initialize();
        _initialized = true;
    }

    private unsafe void Initialize()
    {
        var gl = _view.GL;

        // シェーダーをリソースから読み込んでコンパイル・リンクする
        var vsh = gl.CreateShader(GLEnum.VertexShader);
        GLHelper.ShaderSource(
            gl,
            vsh,
            EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.deformed.vert")
        );
        gl.CompileShader(vsh);

        var fsh = gl.CreateShader(GLEnum.FragmentShader);
        GLHelper.ShaderSource(
            gl,
            fsh,
            EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.deformed.frag")
        );
        gl.CompileShader(fsh);

        _shader = gl.CreateProgram();
        gl.AttachShader(_shader, vsh);
        gl.AttachShader(_shader, fsh);
        gl.LinkProgram(_shader);

        gl.DetachShader(_shader, vsh);
        gl.DetachShader(_shader, fsh);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        // 頂点レイアウト: (x, y) + (u * q, v * q, q)
        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);
        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        const uint stride = DeformedQuadHelper.FloatsPerVertex * sizeof(float);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(
            1,
            3,
            VertexAttribPointerType.Float,
            false,
            stride,
            2 * sizeof(float)
        );
        gl.EnableVertexAttribArray(1);

        // インデックスバッファは VAO に紐付ける
        _ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        _uModel = gl.GetUniformLocation(_shader, "uModel");
        _uProjection = gl.GetUniformLocation(_shader, "uProjection");
        _uTexture0 = gl.GetUniformLocation(_shader, "uTexture0");
        _uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
    }
}
