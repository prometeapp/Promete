using System;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL;

/// <summary>
/// OpenGL バックエンドにおける <see cref="IShaderFactory"/> の実装です。
/// </summary>
internal class GLShaderFactory : IShaderFactory
{
    public Silk.NET.OpenGL.GL GL { get; set; }

    /// <inheritdoc/>
    public void Compile(ShaderProgram program)
    {
        PrometeApp.Current.ThrowIfNotMainThread();

        var vSrc = program.VertexShaderSource
            ?? throw new InvalidOperationException("頂点シェーダーのソースコードが設定されていません。");
        var fSrc = program.FragmentShaderSource
            ?? throw new InvalidOperationException("フラグメントシェーダーのソースコードが設定されていません。");

        var vsh = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vsh, vSrc);
        GL.CompileShader(vsh);
        CheckShaderCompile(GL, vsh, "vertex");

        var fsh = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fsh, fSrc);
        GL.CompileShader(fsh);
        CheckShaderCompile(GL, fsh, "fragment");

        var prog = GL.CreateProgram();
        GL.AttachShader(prog, vsh);
        GL.AttachShader(prog, fsh);
        GL.LinkProgram(prog);
        CheckProgramLink(GL, prog);

        GL.DetachShader(prog, vsh);
        GL.DetachShader(prog, fsh);
        GL.DeleteShader(vsh);
        GL.DeleteShader(fsh);

        program.SetCompiledData((int)prog, OnDispose);
    }

    private static void CheckShaderCompile(Silk.NET.OpenGL.GL gl, uint shader, string stage)
    {
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status != 0) return;
        var log = gl.GetShaderInfoLog(shader);
        throw new InvalidOperationException($"シェーダーのコンパイルエラー ({stage}): {log}");
    }

    private static void CheckProgramLink(Silk.NET.OpenGL.GL gl, uint program)
    {
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status != 0) return;
        var log = gl.GetProgramInfoLog(program);
        throw new InvalidOperationException($"シェーダーのリンクエラー: {log}");
    }

    private void OnDispose(ShaderProgram p)
    {
        GL.DeleteProgram((uint)p.Handle);
        GLMaterialApplier.InvalidateProgram(p.Handle);
    }
}
