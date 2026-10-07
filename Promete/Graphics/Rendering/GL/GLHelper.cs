using System;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL;

public static class GLHelper
{
    private const string Glsl330Header = "#version 330 core";

    private const string GlslEs300Header =
        "#version 300 es\nprecision highp float;\nprecision highp int;\nprecision highp sampler2D;";

    private static readonly ConditionalWeakTable<Silk.NET.OpenGL.GL, StrongBox<bool>> EsCache = [];

    public static VectorInt GetViewport(Silk.NET.OpenGL.GL gl)
    {
        Span<int> viewport = stackalloc int[4];
        gl.GetInteger(GetPName.Viewport, viewport);
        return new VectorInt(viewport[2], viewport[3]);
    }

    /// <summary>
    /// 指定した GL が OpenGL ES (WebGL を含む) のコンテキストかどうかを返す。結果は GL ごとにキャッシュする。
    /// </summary>
    internal static bool IsOpenGlEs(Silk.NET.OpenGL.GL gl)
    {
        return EsCache
            .GetValue(
                gl,
                g => new StrongBox<bool>(
                    g.GetStringS(StringName.Version)
                        ?.StartsWith("OpenGL ES", StringComparison.Ordinal) == true
                )
            )
            .Value;
    }

    /// <summary>
    /// シェーダーのソースを設定する。OpenGL ES のコンテキストでは、GLSL 330 core のソースを GLSL ES 3.00 に書き換える。
    /// </summary>
    internal static void ShaderSource(Silk.NET.OpenGL.GL gl, uint shader, string source)
    {
        gl.ShaderSource(shader, IsOpenGlEs(gl) ? ToGlslEs(source) : source);
    }

    /// <summary>
    /// <c>#version 330 core</c> で始まるソースを GLSL ES 3.00 に書き換える。それ以外のソースはそのまま返す。
    /// </summary>
    internal static string ToGlslEs(string source)
    {
        var trimmed = source.TrimStart();
        return trimmed.StartsWith(Glsl330Header, StringComparison.Ordinal)
            ? GlslEs300Header + trimmed[Glsl330Header.Length..]
            : source;
    }
}
