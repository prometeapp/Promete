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
public sealed class GLDrawDeformedTextureCommandRunner(IGameView view)
    : CommandRunner<DrawDeformedTextureCommand>
{
    private readonly OpenGLDesktopGameView _view = (OpenGLDesktopGameView)view;
    private bool _initialized;
    private uint _shader;
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private int _uModel;
    private int _uProjection;
    private int _uTexture0;
    private int _uTintColor;

    public override unsafe void Execute(DrawDeformedTextureCommand command)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        EnsureInitialized();

        var gl = _view.GL;
        var viewport = GLHelper.GetViewport(gl);
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            viewport.X,
            viewport.Y,
            0,
            0.1f,
            100f
        );

        Span<float> vertices =
        [
            command.BottomRight.X, command.BottomRight.Y, 1, 1,
            command.TopRight.X, command.TopRight.Y, 1, 0,
            command.BottomLeft.X, command.BottomLeft.Y, 0, 1,
            command.TopLeft.X, command.TopLeft.Y, 0, 0,
        ];

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferSubData<float>(BufferTargetARB.ArrayBuffer, 0, vertices);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        var program = command.Material is { } material
            ? (uint)material.Shader.Handle
            : _shader;
        gl.UseProgram(program);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)command.Texture.Handle);

        var uModel = command.Material is null
            ? _uModel
            : GLMaterialApplier.GetLocation(gl, program, "uModel");
        var uProjection = command.Material is null
            ? _uProjection
            : GLMaterialApplier.GetLocation(gl, program, "uProjection");
        var uTexture0 = command.Material is null
            ? _uTexture0
            : GLMaterialApplier.GetLocation(gl, program, "uTexture0");
        var uTintColor = command.Material is null
            ? _uTintColor
            : GLMaterialApplier.GetLocation(gl, program, "uTintColor");

        if (uModel >= 0)
            gl.UniformMatrix4(uModel, 1, false, (float*)&command.ModelMatrix);
        if (uProjection >= 0)
            gl.UniformMatrix4(uProjection, 1, false, (float*)&projection);
        if (uTexture0 >= 0)
            gl.Uniform1(uTexture0, 0);
        if (uTintColor >= 0)
        {
            var color = command.TintColor;
            gl.Uniform4(
                uTintColor,
                new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f)
            );
        }
        if (command.Material is not null)
            GLMaterialApplier.Apply(gl, program, command.Material);

        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha,
            BlendingFactor.OneMinusSrcAlpha,
            BlendingFactor.One,
            BlendingFactor.OneMinusSrcAlpha
        );
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        gl.BindVertexArray(0);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        var gl = _view.GL;
        var vsh = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(
            vsh,
            EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.deformed_texture.vert")
        );
        gl.CompileShader(vsh);
        var fsh = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(
            fsh,
            EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.deformed_texture.frag")
        );
        gl.CompileShader(fsh);

        _shader = gl.CreateProgram();
        gl.AttachShader(_shader, vsh);
        gl.AttachShader(_shader, fsh);
        gl.LinkProgram(_shader);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        Span<uint> indices = [0, 1, 3, 1, 2, 3];
        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);
        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            (nuint)(16 * sizeof(float)),
            null,
            BufferUsageARB.DynamicDraw
        );
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);
        _ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
        gl.BindVertexArray(0);

        _uModel = gl.GetUniformLocation(_shader, "uModel");
        _uProjection = gl.GetUniformLocation(_shader, "uProjection");
        _uTexture0 = gl.GetUniformLocation(_shader, "uTexture0");
        _uTintColor = gl.GetUniformLocation(_shader, "uTintColor");
        _initialized = true;
    }
}
