using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Promete.Graphics;

namespace Promete.Nodes;

/// <summary>
/// シェーダーと Uniform パラメーターをまとめたマテリアルを表します。
/// <see cref="Node.Material"/> に設定することで、そのノードにカスタムシェーダーを適用できます。
/// </summary>
public sealed class Material : IEquatable<Material>
{
    /// <summary>このマテリアルが使用するシェーダープログラムを取得します。</summary>
    public ShaderProgram Shader { get; }

    private readonly Dictionary<string, object> _uniforms = new();

    /// <summary>Uniform 値の読み取り専用ビュー（バックエンドのランナーが使用）。</summary>
    internal IReadOnlyDictionary<string, object> Uniforms => _uniforms;

    /// <param name="shader">使用するシェーダープログラム。</param>
    public Material(ShaderProgram shader)
    {
        Shader = shader;
    }

    public object this[string key]
    {
        get => _uniforms[key];
        set => _uniforms[key] = value;
    }

    /// <inheritdoc/>
    public bool Equals(Material? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!ReferenceEquals(Shader, other.Shader)) return false;
        if (_uniforms.Count != other._uniforms.Count) return false;
        foreach (var (k, v) in _uniforms)
        {
            if (!other._uniforms.TryGetValue(k, out var ov)) return false;
            if (!v.Equals(ov)) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Material m && Equals(m);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Shader は参照ハッシュ、Uniform は XOR で順序非依存
        var h = RuntimeHelpers.GetHashCode(Shader);
        foreach (var (k, v) in _uniforms)
            h ^= HashCode.Combine(k, v);
        return h;
    }
}
