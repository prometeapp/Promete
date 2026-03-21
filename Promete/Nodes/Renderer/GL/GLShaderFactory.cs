using System;
using Promete.Graphics;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL;

/// <summary>
/// OpenGL バックエンドにおける <see cref="IShaderFactory"/> の実装です。
/// </summary>
internal class GLShaderFactory(IWindow window) : IShaderFactory
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow
        ?? throw new InvalidOperationException("Window is not an OpenGLDesktopWindow");

    /// <inheritdoc/>
    public void Compile(ShaderProgram program)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        var gl = _window.GL;

        var vSrc = program.VertexShaderSource
            ?? throw new InvalidOperationException("頂点シェーダーのソースコードが設定されていません。");
        var fSrc = program.FragmentShaderSource
            ?? throw new InvalidOperationException("フラグメントシェーダーのソースコードが設定されていません。");

        var vsh = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vsh, vSrc);
        gl.CompileShader(vsh);
        CheckShaderCompile(gl, vsh, "vertex");

        var fsh = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fsh, fSrc);
        gl.CompileShader(fsh);
        CheckShaderCompile(gl, fsh, "fragment");

        var prog = gl.CreateProgram();
        gl.AttachShader(prog, vsh);
        gl.AttachShader(prog, fsh);
        gl.LinkProgram(prog);
        CheckProgramLink(gl, prog);

        gl.DetachShader(prog, vsh);
        gl.DetachShader(prog, fsh);
        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

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
        _window.GL.DeleteProgram((uint)p.Handle);
        GLMaterialApplier.InvalidateProgram(p.Handle);
    }
}
