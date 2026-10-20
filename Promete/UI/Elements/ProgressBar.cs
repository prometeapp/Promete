using System;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// 進捗を表示するプログレスバーUI要素です。
/// 横方向に、<see cref="Minimum"/> から <see cref="Maximum"/> の範囲で <see cref="Value"/> の割合を塗ります。
/// </summary>
/// <remarks>
/// 表示専用の要素で、フォーカスもクリックも受け付けません。
/// </remarks>
public class ProgressBar : UIElement
{
    private float _value;
    private float _minimum;
    private float _maximum = 1f;
    private IUIStyle<ProgressBar> _style;

    /// <summary>
    /// ProgressBar の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="minimum">最小値。</param>
    /// <param name="maximum">最大値。</param>
    /// <param name="value">初期値。</param>
    /// <param name="style">適用するスタイル。null の場合は DefaultProgressBarStyle が使用されます。</param>
    public ProgressBar(
        float minimum = 0f,
        float maximum = 1f,
        float value = 0f,
        IUIStyle<ProgressBar>? style = null
    )
    {
        _style = style ?? new DefaultProgressBarStyle();
        _minimum = minimum;
        _maximum = maximum;
        _value = Math.Clamp(value, minimum, Math.Max(minimum, maximum));
        IsFocusable = false;
        Size = (200, 16);
    }

    /// <summary>現在の値を取得または設定します。範囲外の値は、最小値から最大値の間に収められます。</summary>
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, Math.Max(_minimum, _maximum));
            if (Math.Abs(_value - clamped) < float.Epsilon)
                return;

            _value = clamped;
            NotifyStyleDirty();
        }
    }

    /// <summary>最小値を取得または設定します。</summary>
    public float Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            Value = _value;
            NotifyStyleDirty();
        }
    }

    /// <summary>最大値を取得または設定します。</summary>
    public float Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            Value = _value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// 値の割合 (0〜1) を取得します。
    /// 最大値が最小値以下の場合は 0 です。
    /// </summary>
    public float Fraction =>
        _maximum <= _minimum ? 0f : Math.Clamp((_value - _minimum) / (_maximum - _minimum), 0f, 1f);

    /// <summary>スタイルを取得または設定します。</summary>
    public IUIStyle<ProgressBar> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// 表示専用の要素なので、常に false を返し、クリックやホバーの対象になりません。
    /// 重なっている下の要素への操作も、遮りません。
    /// </summary>
    public override bool HitTest(VectorInt point) => false;

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }
}
