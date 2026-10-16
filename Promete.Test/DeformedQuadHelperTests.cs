using FluentAssertions;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Vulkan;
using Silk.NET.Shaderc;

namespace Promete.Test;

/// <summary>
/// <see cref="DeformedQuadHelper"/> の射影補間用の q と三角形分割のテスト。
/// </summary>
public class DeformedQuadHelperTests
{
    [Fact]
    public void 平行四辺形ではqがすべて等しい()
    {
        var (q, _) = DeformedQuadHelper.ComputeQ((0, 0), (10, 0), (5, 10), (15, 10));

        q.TopRight.Should().BeApproximately(q.TopLeft, 1e-5f);
        q.BottomLeft.Should().BeApproximately(q.TopLeft, 1e-5f);
        q.BottomRight.Should().BeApproximately(q.TopLeft, 1e-5f);
    }

    [Fact]
    public void 台形では対角線の交点からqを求める()
    {
        // 対角線はどちらも 1:3 の位置で交わる
        var (q, _) = DeformedQuadHelper.ComputeQ((0, 0), (10, 0), (-10, 10), (20, 10));

        q.TopLeft.Should().BeApproximately(4 / 3f, 1e-5f);
        q.TopRight.Should().BeApproximately(4 / 3f, 1e-5f);
        q.BottomLeft.Should().BeApproximately(4f, 1e-5f);
        q.BottomRight.Should().BeApproximately(4f, 1e-5f);
    }

    [Fact]
    public void 台形の対角線の交点はテクスチャの中心になる()
    {
        Span<float> v = stackalloc float[DeformedQuadHelper.VertexFloats];
        Span<uint> i = stackalloc uint[DeformedQuadHelper.IndexCount];
        DeformedQuadHelper.Build((0, 0), (10, 0), (-10, 10), (20, 10), (0, 0), (1, 1), v, i);

        // TL (0) と BR (3) を交点の位置 t = 0.25 で線形補間し、q で割る
        const float t = 0.25f;
        var uq = ((1 - t) * v[2]) + (t * v[17]);
        var vq = ((1 - t) * v[3]) + (t * v[18]);
        var qq = ((1 - t) * v[4]) + (t * v[19]);
        (uq / qq).Should().BeApproximately(0.5f, 1e-5f);
        (vq / qq).Should().BeApproximately(0.5f, 1e-5f);
    }

    [Fact]
    public void 右下が凹んだ四角形はアフィンでTLとBRの対角線で分割する()
    {
        var (q, splitByTopLeft) = DeformedQuadHelper.ComputeQ((0, 0), (10, 0), (0, 10), (1, 1));

        q.Should().Be(DeformedQuadHelper.QuadQ.Affine);
        splitByTopLeft.Should().BeTrue();
    }

    [Fact]
    public void 右上が凹んだ四角形はアフィンでTRとBLの対角線で分割する()
    {
        var (q, splitByTopLeft) = DeformedQuadHelper.ComputeQ((0, 0), (3, 7), (0, 10), (10, 10));

        q.Should().Be(DeformedQuadHelper.QuadQ.Affine);
        splitByTopLeft.Should().BeFalse();

        Span<float> v = stackalloc float[DeformedQuadHelper.VertexFloats];
        Span<uint> i = stackalloc uint[DeformedQuadHelper.IndexCount];
        DeformedQuadHelper.Build((0, 0), (3, 7), (0, 10), (10, 10), (0, 0), (1, 1), v, i);
        i.ToArray().Should().Equal(0u, 1u, 2u, 1u, 3u, 2u);
    }

    [Fact]
    public void 対角線が平行な四角形はアフィンになる()
    {
        // 下の2頂点を入れ替えた、辺が交差する四角形
        var (q, _) = DeformedQuadHelper.ComputeQ((0, 0), (10, 0), (10, 10), (0, 10));

        q.Should().Be(DeformedQuadHelper.QuadQ.Affine);
    }

    [Fact]
    public void UVはテクスチャのUV範囲に合わせる()
    {
        Span<float> v = stackalloc float[DeformedQuadHelper.VertexFloats];
        Span<uint> i = stackalloc uint[DeformedQuadHelper.IndexCount];
        DeformedQuadHelper.Build(
            (0, 0),
            (10, 0),
            (0, 10),
            (10, 10),
            (0.25f, 0.5f),
            (0.75f, 1f),
            v,
            i
        );

        for (var n = 0; n < 4; n++)
        {
            var q = v[(n * 5) + 4];
            var u = v[(n * 5) + 2] / q;
            var w = v[(n * 5) + 3] / q;
            var (eu, ev) = n switch
            {
                0 => (0.25f, 0.5f),
                1 => (0.75f, 0.5f),
                2 => (0.25f, 1f),
                _ => (0.75f, 1f),
            };
            u.Should().BeApproximately(eu, 1e-5f);
            w.Should().BeApproximately(ev, 1e-5f);
        }
    }

    [Theory]
    [InlineData("deformed.vert", ShaderKind.VertexShader)]
    [InlineData("deformed.frag", ShaderKind.FragmentShader)]
    public void Vulkan用シェーダーがコンパイルできる(string name, ShaderKind kind)
    {
        using var compiler = new VulkanShaderCompiler();
        var source = EmbeddedResource.GetResourceAsString(
            $"Promete.Resources.shaders.vulkan.{name}"
        );

        var act = () => compiler.Compile(source, kind, name);

        act.Should().NotThrow();
    }
}
