using System;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// スライダーUI要素です。
/// ドラッグ操作で値を変更します。
/// </summary>
public class Slider : UIElement
{
	/// <summary>現在の値を取得または設定します。</summary>
	public float Value
	{
		get => _value;
		set
		{
			var clamped = Math.Clamp(value, _minimum, _maximum);
			if (Math.Abs(_value - clamped) < float.Epsilon) return;
			_value = clamped;
			NotifyStyleDirty();
			ValueChanged?.Invoke(_value);
		}
	}

	/// <summary>最小値を取得または設定します。</summary>
	public float Minimum
	{
		get => _minimum;
		set
		{
			_minimum = value;
			Value = Math.Clamp(_value, _minimum, _maximum);
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
			Value = Math.Clamp(_value, _minimum, _maximum);
			NotifyStyleDirty();
		}
	}

	/// <summary>値が 0〜1 で正規化された割合。</summary>
	public float NormalizedValue => (_maximum - _minimum) > 0 ? (_value - _minimum) / (_maximum - _minimum) : 0;

	/// <summary>つまみのサイズ（幅、高さ）。</summary>
	public VectorInt ThumbSize { get; set; } = (16, 20);

	/// <summary>トラックの高さ。</summary>
	public int TrackHeight { get; set; } = 6;

	/// <summary>値を整数に丸めるかどうか。</summary>
	public bool IntegerOnly { get; set; } = true;

	/// <summary>キーボード操作時の1ステップあたりの変化量。</summary>
	public float Step { get; set; } = 1f;

	/// <summary>スタイルを取得または設定します。</summary>
	public IUIStyle<Slider> Style
	{
		get => _style;
		set
		{
			_style = value;
			NotifyStyleDirty();
		}
	}

	/// <summary>ドラッグ中かどうか。</summary>
	public bool IsDragging => _isDragging;

	/// <summary>値が変更されたときに発火します。</summary>
	public event Action<float>? ValueChanged;

	private float _value;
	private float _minimum;
	private float _maximum = 1f;
	private bool _isDragging;
	private IUIStyle<Slider> _style;

	/// <summary>
	/// Slider の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="minimum">最小値。</param>
	/// <param name="maximum">最大値。</param>
	/// <param name="value">初期値。</param>
	/// <param name="style">適用するスタイル。null の場合は DefaultSliderStyle が使用されます。</param>
	public Slider(float minimum = 0f, float maximum = 1f, float value = 0f, IUIStyle<Slider>? style = null)
	{
		_style = style ?? new DefaultSliderStyle();
		_minimum = minimum;
		_maximum = maximum;
		_value = Math.Clamp(value, minimum, maximum);
		Size = (200, 24);

		PointerPressed += OnPointerPressed;
		PointerMoved += OnPointerMoved;
		PointerReleased += OnPointerReleased;
	}

	/// <summary>
	/// フォーカス中に左右キーで値を変更します。
	/// UIManager から呼ばれます。
	/// </summary>
	internal void ProcessKeyboardInput(Promete.Input.Keyboard keyboard)
	{
		if (keyboard.Left.IsKeyDown)
			Value -= Step;
		if (keyboard.Right.IsKeyDown)
			Value += Step;
	}

	protected override void ApplyStyle(UIElementState state)
	{
		_style.Apply(this, state);
	}

	private void OnPointerPressed(VectorInt position)
	{
		_isDragging = true;
		UpdateValueFromPosition(position);
	}

	private void OnPointerMoved(VectorInt position)
	{
		if (!_isDragging) return;
		UpdateValueFromPosition(position);
	}

	private void OnPointerReleased(VectorInt _)
	{
		_isDragging = false;
	}

	private void UpdateValueFromPosition(VectorInt worldPos)
	{
		var rect = GetHitRect();
		var thumbHalf = ThumbSize.X / 2f;
		var trackStart = rect.Left + thumbHalf;
		var trackEnd = rect.Left + rect.Width - thumbHalf;
		var trackWidth = trackEnd - trackStart;

		if (trackWidth <= 0) return;

		var normalized = Math.Clamp((worldPos.X - trackStart) / trackWidth, 0f, 1f);
		var raw = _minimum + normalized * (_maximum - _minimum);
		Value = IntegerOnly ? MathF.Round(raw) : raw;
	}
}
