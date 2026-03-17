#pragma warning disable RECS0018
using System;
using System.Collections.Generic;
using System.Numerics;
using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Helper;

/// <summary>
/// <see cref="DrawTextureBatchedCommand"/> をGPUインスタンシングで描画します。
/// </summary>
public class GLBatchTextureRenderer : IDisposable
{
    private const int InitialInstanceCapacity = 512;
    // per-instance: mat4(16) + vec4 tintColor(4) = 20 floats
    private const int InstanceStride = 20;

    private readonly OpenGLDesktopWindow _window;

    private float[] _instanceData = new float[InitialInstanceCapacity * InstanceStride];
    private bool _initialized;
    private uint _shader;
    private uint _vao, _vbo, _ebo, _instanceVbo;

    public GLBatchTextureRenderer(IWindow window)
    {
        _window = window as OpenGLDesktopWindow
            ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");
    }

    /// <summary>
    /// バッチをインスタンシングで描画します。件数が1の場合もこちらを使用します。
    /// </summary>
    public unsafe void DrawInstanced(List<DrawTextureCommand> items)
    {
        if (items.Count == 0) return;
        PrometeApp.Current.ThrowIfNotMainThread();

        EnsureInitialized();

        var gl = _window.GL;
        var count = items.Count;

        EnsureInstanceBufferCapacity(count);

        // プロジェクション行列を計算
        var viewport = GLHelper.GetViewport(gl);
        var currentFrameBufferId = gl.GetInteger(GLEnum.FramebufferBinding);
        if (currentFrameBufferId == 0)
            viewport /= _window.Scale;
        var projection = Matrix4x4.CreateOrthographicOffCenter(0, viewport.X, viewport.Y, 0, 0.1f, 100f);

        // per-instanceデータを構築
        for (var i = 0; i < count; i++)
        {
            var cmd = items[i];
            var model =
                Matrix4x4.CreateScale(cmd.Width, cmd.Height, 1)
                * Matrix4x4.CreateTranslation(cmd.Pivot.X, cmd.Pivot.Y, 0)
                * cmd.ModelMatrix;

            var offset = i * InstanceStride;
            _instanceData[offset +  0] = model.M11; _instanceData[offset +  1] = model.M12;
            _instanceData[offset +  2] = model.M13; _instanceData[offset +  3] = model.M14;
            _instanceData[offset +  4] = model.M21; _instanceData[offset +  5] = model.M22;
            _instanceData[offset +  6] = model.M23; _instanceData[offset +  7] = model.M24;
            _instanceData[offset +  8] = model.M31; _instanceData[offset +  9] = model.M32;
            _instanceData[offset + 10] = model.M33; _instanceData[offset + 11] = model.M34;
            _instanceData[offset + 12] = model.M41; _instanceData[offset + 13] = model.M42;
            _instanceData[offset + 14] = model.M43; _instanceData[offset + 15] = model.M44;
            var c = cmd.TintColor;
            _instanceData[offset + 16] = c.R / 255f;
            _instanceData[offset + 17] = c.G / 255f;
            _instanceData[offset + 18] = c.B / 255f;
            _instanceData[offset + 19] = c.A / 255f;
        }

        // インスタンスデータをGPUに転送
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        gl.BufferSubData<float>(BufferTargetARB.ArrayBuffer, 0,
            new ReadOnlySpan<float>(_instanceData, 0, count * InstanceStride));
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // 描画
        gl.Enable(GLEnum.Blend);
        gl.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

        gl.UseProgram(_shader);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, (uint)items[0].Texture.Handle);

        var uProjection = gl.GetUniformLocation(_shader, "uProjection");
        gl.UniformMatrix4(uProjection, 1, false, (float*)&projection);
        var uTexture0 = gl.GetUniformLocation(_shader, "uTexture0");
        gl.Uniform1(uTexture0, 0);

        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null, (uint)count);
        gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (!_initialized) return;
        var gl = _window.GL;
        gl.DeleteProgram(_shader);
        gl.DeleteVertexArray(_vao);
        gl.DeleteBuffer(_vbo);
        gl.DeleteBuffer(_ebo);
        gl.DeleteBuffer(_instanceVbo);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        Initialize();
        _initialized = true;
    }

    private unsafe void Initialize()
    {
        var gl = _window.GL;

        // シェーダーをコンパイル・リンク
        var vsh = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.texture_instanced.vert"));
        gl.CompileShader(vsh);

        var fsh = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.texture_instanced.frag"));
        gl.CompileShader(fsh);

        _shader = gl.CreateProgram();
        gl.AttachShader(_shader, vsh);
        gl.AttachShader(_shader, fsh);
        gl.LinkProgram(_shader);

        gl.DetachShader(_shader, vsh);
        gl.DetachShader(_shader, fsh);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        // 単位クワッドの頂点データ（位置 + UV）
        Span<float> vertices =
        [
            1.0f, 0.0f, 1.0f, 0.0f, // 右下
            1.0f, 1.0f, 1.0f, 1.0f, // 右上
            0.0f, 1.0f, 0.0f, 1.0f, // 左上
            0.0f, 0.0f, 0.0f, 0.0f, // 左下
        ];
        Span<uint> indices = [0, 1, 3, 1, 2, 3];

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        // 頂点VBO
        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        // インスタンスVBO（per-instance: mat4 + vec4）
        _instanceVbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_instanceData.Length * sizeof(float)), null, BufferUsageARB.DynamicDraw);

        for (uint i = 0; i < 4; i++)
        {
            var loc = 2 + i;
            gl.VertexAttribPointer(loc, 4, VertexAttribPointerType.Float, false,
                (uint)(InstanceStride * sizeof(float)), (int)(i * 4 * sizeof(float)));
            gl.EnableVertexAttribArray(loc);
            gl.VertexAttribDivisor(loc, 1);
        }
        // TintColor (location 6)
        gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false,
            (uint)(InstanceStride * sizeof(float)), 16 * sizeof(float));
        gl.EnableVertexAttribArray(6);
        gl.VertexAttribDivisor(6, 1);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        // EBO
        _ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    private unsafe void EnsureInstanceBufferCapacity(int count)
    {
        var required = count * InstanceStride;
        if (_instanceData.Length >= required) return;

        var newSize = _instanceData.Length;
        while (newSize < required) newSize *= 2;
        _instanceData = new float[newSize];

        var gl = _window.GL;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(newSize * sizeof(float)), null, BufferUsageARB.DynamicDraw);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }
}
