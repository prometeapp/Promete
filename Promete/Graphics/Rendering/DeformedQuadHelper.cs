using System;
using Promete.Graphics.Rendering.Commands;

namespace Promete.Graphics.Rendering;

/// <summary>
/// <see cref="DrawDeformedTextureCommand"/> の頂点データを組み立てるヘルパーです。
/// </summary>
/// <remarks>
/// 凸四角形では、対角線の交点から各頂点の同次座標 q を求め、射影補間 (uv / q) で継ぎ目なくテクスチャを貼ります。<br />
/// 凹四角形・辺が交差する四角形・退化した四角形では q をすべて 1 とし、2つの三角形によるアフィン補間にします。
/// </remarks>
internal static class DeformedQuadHelper
{
    /// <summary>1頂点あたりの float 数 (x, y, u*q, v*q, q)。</summary>
    public const int FloatsPerVertex = 5;

    /// <summary>頂点配列の float 数。</summary>
    public const int VertexFloats = FloatsPerVertex * 4;

    /// <summary>インデックス数。</summary>
    public const int IndexCount = 6;

    /// <summary>
    /// 頂点 (TL, TR, BL, BR の順) とインデックスを書き込みます。
    /// </summary>
    /// <param name="command">描画コマンド。</param>
    /// <param name="vertices">書き込み先。長さは <see cref="VertexFloats"/> 以上。</param>
    /// <param name="indices">書き込み先。長さは <see cref="IndexCount"/> 以上。</param>
    public static void Build(
        DrawDeformedTextureCommand command,
        Span<float> vertices,
        Span<uint> indices
    )
    {
        var uvStart = command.Texture.UvStart;
        var uvEnd = command.Texture.UvEnd;
        Build(
            command.TopLeft,
            command.TopRight,
            command.BottomLeft,
            command.BottomRight,
            uvStart,
            uvEnd,
            vertices,
            indices
        );
    }

    /// <summary>
    /// 頂点 (TL, TR, BL, BR の順) とインデックスを書き込みます。
    /// </summary>
    public static void Build(
        Vector topLeft,
        Vector topRight,
        Vector bottomLeft,
        Vector bottomRight,
        Vector uvStart,
        Vector uvEnd,
        Span<float> vertices,
        Span<uint> indices
    )
    {
        var (q, splitByTopLeft) = ComputeQ(topLeft, topRight, bottomLeft, bottomRight);

        Write(vertices, 0, topLeft, uvStart.X, uvStart.Y, q.TopLeft);
        Write(vertices, 1, topRight, uvEnd.X, uvStart.Y, q.TopRight);
        Write(vertices, 2, bottomLeft, uvStart.X, uvEnd.Y, q.BottomLeft);
        Write(vertices, 3, bottomRight, uvEnd.X, uvEnd.Y, q.BottomRight);

        if (splitByTopLeft)
        {
            // TL–BR の対角線で分割する
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 3;
            indices[3] = 0;
            indices[4] = 3;
            indices[5] = 2;
        }
        else
        {
            // TR–BL の対角線で分割する
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;
        }
    }

    /// <summary>
    /// 各頂点の同次座標 q と、三角形に分割する対角線を求めます。
    /// </summary>
    /// <returns>
    /// 各頂点の q と、TL–BR の対角線で分割するかどうか (false の場合は TR–BL で分割)。
    /// </returns>
    public static (QuadQ Q, bool SplitByTopLeft) ComputeQ(
        Vector topLeft,
        Vector topRight,
        Vector bottomLeft,
        Vector bottomRight
    )
    {
        // 対角線 TL→BR (r) と TR→BL (s) の交点を、それぞれの媒介変数 t, u で求める
        var r = bottomRight - topLeft;
        var s = bottomLeft - topRight;
        var denom = Cross(r, s);
        if (MathF.Abs(denom) < 1e-6f)
            return (QuadQ.Affine, true);

        var d = topRight - topLeft;
        var t = Cross(d, s) / denom;
        var u = Cross(d, r) / denom;
        var tInside = t is > 0 and < 1;
        var uInside = u is > 0 and < 1;

        // 両方の対角線上で交わるなら凸四角形
        if (tInside && uInside)
        {
            var q = new QuadQ(1 / (1 - t), 1 / (1 - u), 1 / u, 1 / t);
            return (q, true);
        }

        // 凹四角形では、反射頂点を通る対角線だけが内側にある。
        // その対角線の延長線上で、もう一方の対角線と交わる。
        return (QuadQ.Affine, !tInside || uInside);
    }

    private static void Write(Span<float> vertices, int index, Vector p, float u, float v, float q)
    {
        var i = index * FloatsPerVertex;
        vertices[i + 0] = p.X;
        vertices[i + 1] = p.Y;
        vertices[i + 2] = u * q;
        vertices[i + 3] = v * q;
        vertices[i + 4] = q;
    }

    private static float Cross(Vector a, Vector b) => (a.X * b.Y) - (a.Y * b.X);

    /// <summary>
    /// 四角形の各頂点の同次座標 q です。
    /// </summary>
    public readonly record struct QuadQ(
        float TopLeft,
        float TopRight,
        float BottomLeft,
        float BottomRight
    )
    {
        /// <summary>アフィン補間 (q がすべて 1)。</summary>
        public static QuadQ Affine => new(1, 1, 1, 1);
    }
}
