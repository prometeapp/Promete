using System.Collections.Generic;
using System.Numerics;
using Promete.Graphics;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Helper;

/// <summary>
/// マテリアルの Uniform 値を OpenGL プログラムに適用するヘルパーです。
/// Uniform ロケーションをプログラムハンドルと名前のペアでキャッシュし、
/// フレームごとの <c>glGetUniformLocation</c> 呼び出しを最小化します。
/// </summary>
internal static class GLMaterialApplier
{
    private static readonly Dictionary<(int programHandle, string name), int> _locationCache = new();

    /// <summary>
    /// Uniform のロケーションをキャッシュ付きで取得します。
    /// 初回のみ <c>glGetUniformLocation</c> を呼び出し、以降はキャッシュから返します。
    /// </summary>
    public static int GetLocation(Silk.NET.OpenGL.GL gl, uint program, string name)
    {
        var key = ((int)program, name);
        if (!_locationCache.TryGetValue(key, out var loc))
        {
            loc = gl.GetUniformLocation(program, name);
            _locationCache[key] = loc;
        }
        return loc;
    }

    /// <summary>
    /// マテリアルのカスタム Uniform 値を GL プログラムに適用します。
    /// </summary>
    /// <param name="gl">GL コンテキスト。</param>
    /// <param name="program">適用先の GL プログラムハンドル。</param>
    /// <param name="material">適用するマテリアル。</param>
    /// <param name="firstTextureSlot">
    /// Texture2D Uniform に使用を開始するテクスチャスロット番号。
    /// スロット 0 はメインテクスチャ（<c>uTexture0</c>）用に予約されているため、デフォルトは 1 です。
    /// </param>
    public static unsafe void Apply(Silk.NET.OpenGL.GL gl, uint program, Material material, int firstTextureSlot = 1)
    {
        var textureSlot = firstTextureSlot;
        foreach (var (name, value) in material.Uniforms)
        {
            var loc = GetLocation(gl, program, name);
            if (loc < 0) continue;
            switch (value)
            {
                case float f:
                    gl.Uniform1(loc, f);
                    break;
                case int i:
                    gl.Uniform1(loc, i);
                    break;
                case Vector2 v2:
                    gl.Uniform2(loc, v2.X, v2.Y);
                    break;
                case Vector3 v3:
                    gl.Uniform3(loc, v3.X, v3.Y, v3.Z);
                    break;
                case Vector4 v4:
                    gl.Uniform4(loc, v4.X, v4.Y, v4.Z, v4.W);
                    break;
                case Matrix4x4 m:
                    gl.UniformMatrix4(loc, 1, false, (float*)&m);
                    break;
                case Texture2D t:
                    gl.ActiveTexture(TextureUnit.Texture0 + textureSlot);
                    gl.BindTexture(TextureTarget.Texture2D, (uint)t.Handle);
                    gl.Uniform1(loc, textureSlot);
                    textureSlot++;
                    break;
            }
        }
    }

    /// <summary>
    /// 指定したプログラムハンドルに関連するキャッシュエントリをすべて削除します。
    /// <see cref="Graphics.ShaderProgram.Dispose"/> 時に呼び出してください。
    /// </summary>
    internal static void InvalidateProgram(int programHandle)
    {
        var keysToRemove = new List<(int programHandle, string name)>();
        foreach (var key in _locationCache.Keys)
            if (key.programHandle == programHandle)
                keysToRemove.Add(key);
        foreach (var key in keysToRemove)
            _locationCache.Remove(key);
    }
}
