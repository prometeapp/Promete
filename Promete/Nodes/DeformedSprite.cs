using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

namespace Promete.Nodes;

/// <summary>
/// 4つの頂点を自由に指定して、テクスチャを変形して描画するスプライトクラスです。
/// </summary>
/// <remarks>
/// 各頂点は <see cref="Node.Location"/> を原点とした相対座標で指定します。<br />
/// 頂点が凸四角形をなす場合は射影補間で描画され、テクスチャが継ぎ目なく貼られます。
/// 凹四角形や辺が交差する場合は、2つの三角形によるアフィン補間で描画されます。
/// </remarks>
public class DeformedSprite : Node
{
    private Texture2D? _texture;
    private Color _tintColor;

    private Vector _topLeft;
    private Vector _topRight;
    private Vector _bottomLeft;
    private Vector _bottomRight;

    private Vector _min;
    private VectorInt _size;

    private DrawDeformedTextureCommand? _cachedCommand;
    private Matrix4x4 _cachedModelMatrix;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeformedSprite"/> class.
    /// 4つの頂点を指定して、<see cref="DeformedSprite"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="texture">描画するテクスチャ。</param>
    /// <param name="topLeft">テクスチャの左上に対応する頂点。</param>
    /// <param name="topRight">テクスチャの右上に対応する頂点。</param>
    /// <param name="bottomLeft">テクスチャの左下に対応する頂点。</param>
    /// <param name="bottomRight">テクスチャの右下に対応する頂点。</param>
    /// <param name="tintColor">テクスチャに反映するティントカラー。</param>
    public DeformedSprite(
        Texture2D? texture,
        Vector topLeft,
        Vector topRight,
        Vector bottomLeft,
        Vector bottomRight,
        Color? tintColor = null
    )
    {
        _texture = texture;
        _tintColor = tintColor ?? Color.White;
        ApplyVertices(topLeft, topRight, bottomLeft, bottomRight);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeformedSprite"/> class.
    /// テクスチャのサイズに合わせた長方形の頂点で、<see cref="DeformedSprite"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="texture">描画するテクスチャ。</param>
    /// <param name="tintColor">テクスチャに反映するティントカラー。</param>
    public DeformedSprite(Texture2D? texture = null, Color? tintColor = null)
        : this(
            texture,
            (0, 0),
            (texture?.Size.X ?? 0, 0),
            (0, texture?.Size.Y ?? 0),
            (texture?.Size.X ?? 0, texture?.Size.Y ?? 0),
            tintColor
        ) { }

    /// <summary>
    /// 描画に使用するテクスチャを取得または設定します。
    /// </summary>
    public Texture2D? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            _cachedCommand = null;
        }
    }

    /// <summary>
    /// テクスチャの色調を取得または設定します。
    /// </summary>
    public Color TintColor
    {
        get => _tintColor;
        set
        {
            _tintColor = value;
            _cachedCommand = null;
        }
    }

    /// <summary>
    /// テクスチャの左上に対応する頂点を取得または設定します。
    /// </summary>
    public Vector TopLeft
    {
        get => _topLeft;
        set => SetVertices(value, _topRight, _bottomLeft, _bottomRight);
    }

    /// <summary>
    /// テクスチャの右上に対応する頂点を取得または設定します。
    /// </summary>
    public Vector TopRight
    {
        get => _topRight;
        set => SetVertices(_topLeft, value, _bottomLeft, _bottomRight);
    }

    /// <summary>
    /// テクスチャの左下に対応する頂点を取得または設定します。
    /// </summary>
    public Vector BottomLeft
    {
        get => _bottomLeft;
        set => SetVertices(_topLeft, _topRight, value, _bottomRight);
    }

    /// <summary>
    /// テクスチャの右下に対応する頂点を取得または設定します。
    /// </summary>
    public Vector BottomRight
    {
        get => _bottomRight;
        set => SetVertices(_topLeft, _topRight, _bottomLeft, value);
    }

    /// <summary>
    /// 頂点を囲む矩形の大きさを取得します。
    /// </summary>
    /// <remarks>
    /// 頂点から自動的に算出されます。値を設定しても無視されます。
    /// </remarks>
    public override VectorInt Size
    {
        get => _size;
        set { }
    }

    /// <summary>
    /// 頂点を囲む矩形に対する、<see cref="Node.Location"/> (頂点の原点) の相対位置を取得します。
    /// </summary>
    /// <remarks>
    /// 頂点から自動的に算出されるため、回転や拡大縮小は常に頂点の原点を中心に行われます。値を設定しても無視されます。<br />
    /// 原点が矩形の左上にあれば (0, 0)、矩形の内側や右下にあれば正、矩形の左上より外にあれば負になります。
    /// </remarks>
    public override Vector Pivot
    {
        get => (_size.X == 0 ? 0 : -_min.X / _size.X, _size.Y == 0 ? 0 : -_min.Y / _size.Y);
        set { }
    }

    /// <summary>
    /// 4つの頂点をまとめて設定します。
    /// </summary>
    /// <param name="topLeft">テクスチャの左上に対応する頂点。</param>
    /// <param name="topRight">テクスチャの右上に対応する頂点。</param>
    /// <param name="bottomLeft">テクスチャの左下に対応する頂点。</param>
    /// <param name="bottomRight">テクスチャの右下に対応する頂点。</param>
    public void SetVertices(Vector topLeft, Vector topRight, Vector bottomLeft, Vector bottomRight)
    {
        if (
            _topLeft == topLeft
            && _topRight == topRight
            && _bottomLeft == bottomLeft
            && _bottomRight == bottomRight
        )
            return;
        ApplyVertices(topLeft, topRight, bottomLeft, bottomRight);
    }

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (Texture is not { } tex)
            return;

        if (
            _cachedCommand == null
            || ModelMatrix != _cachedModelMatrix
            || !ReferenceEquals(Material, _cachedCommand.Material)
        )
        {
            // 頂点は Location 原点の座標なので、ModelMatrix に含まれるピボット分のずれを戻してから変換する
            var pivot = Pivot;
            var offset = new Vector2(pivot.X * _size.X, pivot.Y * _size.Y);
            var matrix = ModelMatrix;
            _cachedCommand = new DrawDeformedTextureCommand
            {
                Texture = tex,
                TopLeft = ToWorld(_topLeft, offset, matrix),
                TopRight = ToWorld(_topRight, offset, matrix),
                BottomLeft = ToWorld(_bottomLeft, offset, matrix),
                BottomRight = ToWorld(_bottomRight, offset, matrix),
                TintColor = TintColor,
                Material = Material,
            };
            _cachedModelMatrix = matrix;
        }

        queue.Enqueue(_cachedCommand);
    }

    private static Vector ToWorld(Vector v, Vector2 offset, Matrix4x4 matrix)
    {
        var p = Vector2.Transform(new Vector2(v.X, v.Y) + offset, matrix);
        return (p.X, p.Y);
    }

    private void ApplyVertices(
        Vector topLeft,
        Vector topRight,
        Vector bottomLeft,
        Vector bottomRight
    )
    {
        _topLeft = topLeft;
        _topRight = topRight;
        _bottomLeft = bottomLeft;
        _bottomRight = bottomRight;
        _cachedCommand = null;

        // 頂点を囲む矩形から、サイズとピボットの基準を求める
        var minX = MathF.Min(
            MathF.Min(topLeft.X, topRight.X),
            MathF.Min(bottomLeft.X, bottomRight.X)
        );
        var minY = MathF.Min(
            MathF.Min(topLeft.Y, topRight.Y),
            MathF.Min(bottomLeft.Y, bottomRight.Y)
        );
        var maxX = MathF.Max(
            MathF.Max(topLeft.X, topRight.X),
            MathF.Max(bottomLeft.X, bottomRight.X)
        );
        var maxY = MathF.Max(
            MathF.Max(topLeft.Y, topRight.Y),
            MathF.Max(bottomLeft.Y, bottomRight.Y)
        );
        _min = (minX, minY);
        _size = ((int)MathF.Ceiling(maxX - minX), (int)MathF.Ceiling(maxY - minY));

        UpdateModelMatrix();
        NotifyLayoutChanged();
    }
}
