using System;
using System.Numerics;

namespace Promete.Nodes;

/// <summary>
/// 子ノードを縦または横に積み重ねて配置する <see cref="Container"/> です。 (v2.4~)
/// </summary>
/// <remarks>
/// 子ノードは追加した順に並びます。子ノードの <see cref="Node.Location"/> は自動で上書きされます。<br />
/// 子ノードの回転・スケール・ピボットを考慮した外接矩形で配置します。<br />
/// サイズは常に自動で決まります。(<see cref="Container.AutoSize"/> は常に有効です)
/// </remarks>
/// <example>
/// <code>
/// return new StackContainer(StackDirection.Vertical, gap: 3)
/// {
///     new Sprite(texture1),
///     new Sprite(texture2),
/// };
/// </code>
/// </example>
public class StackContainer : Container
{
    private StackDirection _direction;
    private int _gap;
    private Padding _padding;
    private StackAlignment _align;

    /// <summary>
    /// Initializes a new instance of the <see cref="StackContainer"/> class.
    /// StackContainer の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="direction">子ノードを並べる方向。</param>
    /// <param name="gap">子ノード同士の間隔。</param>
    /// <param name="padding">周囲の余白。</param>
    /// <param name="align">並べる方向と直交する方向の揃え位置。</param>
    /// <param name="isTrimmable">範囲外に出た子ノードを描画しないかどうか。</param>
    public StackContainer(
        StackDirection direction = StackDirection.Vertical,
        int gap = 0,
        Padding padding = default,
        StackAlignment align = StackAlignment.Start,
        bool isTrimmable = false
    )
        : base(isTrimmable)
    {
        _direction = direction;
        _gap = gap;
        _padding = padding;
        _align = align;
        RequestLayout();
    }

    /// <summary>
    /// 常に true です。 <see cref="StackContainer"/> のサイズは常に自動で決まるため、設定は無視されます。
    /// </summary>
    public override bool AutoSize
    {
        get => true;
        set { }
    }

    /// <summary>
    /// 子ノードを並べる方向を取得または設定します。
    /// </summary>
    public StackDirection Direction
    {
        get => _direction;
        set
        {
            if (_direction == value)
                return;
            _direction = value;
            RequestLayout();
        }
    }

    /// <summary>
    /// 子ノード同士の間隔を取得または設定します。
    /// </summary>
    public int Gap
    {
        get => _gap;
        set
        {
            if (_gap == value)
                return;
            _gap = value;
            RequestLayout();
        }
    }

    /// <summary>
    /// 周囲の余白を取得または設定します。
    /// </summary>
    public Padding Padding
    {
        get => _padding;
        set
        {
            if (_padding == value)
                return;
            _padding = value;
            RequestLayout();
        }
    }

    /// <summary>
    /// 並べる方向と直交する方向の揃え位置を取得または設定します。
    /// </summary>
    public StackAlignment Align
    {
        get => _align;
        set
        {
            if (_align == value)
                return;
            _align = value;
            RequestLayout();
        }
    }

    internal override void OnLayout()
    {
        var isVertical = Direction == StackDirection.Vertical;

        // 各子ノードの外接矩形を求め、交差軸方向の最大幅を決める
        var count = Count;
        var mins = new Vector2[count];
        var maxs = new Vector2[count];
        var crossMax = 0f;
        for (var i = 0; i < count; i++)
        {
            GetBounds(this[i], out mins[i], out maxs[i]);
            var extent = maxs[i] - mins[i];
            crossMax = MathF.Max(crossMax, isVertical ? extent.X : extent.Y);
        }

        // 外接矩形の左上が目標位置に来るよう、子ノードの Location をずらす
        var crossStart = isVertical ? Padding.Left : Padding.Top;
        var mainStart = isVertical ? Padding.Top : Padding.Left;
        var cursor = (float)mainStart;
        for (var i = 0; i < count; i++)
        {
            var extent = maxs[i] - mins[i];
            var crossExtent = isVertical ? extent.X : extent.Y;
            var crossPos =
                crossStart
                + Align switch
                {
                    StackAlignment.Center => (crossMax - crossExtent) / 2f,
                    StackAlignment.End => crossMax - crossExtent,
                    _ => 0f,
                };

            var target = isVertical ? new Vector2(crossPos, cursor) : new Vector2(cursor, crossPos);
            var child = this[i];
            var delta = target - mins[i];
            child.Location = (child.Location.X + delta.X, child.Location.Y + delta.Y);

            cursor += (isVertical ? extent.Y : extent.X) + Gap;
        }

        // 末尾の gap は含めない
        var mainContent = count == 0 ? 0f : cursor - Gap - mainStart;
        var width = isVertical ? crossMax : mainContent;
        var height = isVertical ? mainContent : crossMax;
        SetLayoutSize(
            (
                CeilingWithTolerance(width) + Padding.Horizontal,
                CeilingWithTolerance(height) + Padding.Vertical
            )
        );
    }
}
