using System;
using Promete.Input;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// スクロール可能なコンテナUI要素です。
/// 子ノードのコンテンツ領域がビューポートを超えた場合にスクロールします。
/// </summary>
public class ScrollView : UIElement
{
	private readonly Container _content;
	private Vector _scrollOffset;
	private bool _isDragging;
	private VectorInt _dragStartPos;
	private Vector _dragStartOffset;
	private IUIStyle<ScrollView> _style;

	/// <summary>
	/// ScrollView の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="style">適用するスタイル。null の場合は DefaultScrollViewStyle が使用されます。</param>
	public ScrollView(IUIStyle<ScrollView>? style = null)
	{
		_style = style ?? new DefaultScrollViewStyle();
		IsTrimmable = true;
		IsFocusable = false;

		_content = new Container();
		base.Add(_content);

		Size = (200, 200);

		PointerPressed += OnPointerPressed;
		PointerMoved += OnPointerMoved;
		PointerReleased += OnPointerReleased;
	}

	/// <summary>コンテンツの論理サイズ。スクロール可能な範囲を決定します。</summary>
	public VectorInt ContentSize { get; set; } = (200, 400);

	/// <summary>現在のスクロールオフセットを取得します。</summary>
	public Vector ScrollOffset => _scrollOffset;

	/// <summary>マウスホイールによるスクロール量（ピクセル/ノッチ）。</summary>
	public float ScrollSpeed { get; set; } = 30f;

	/// <summary>水平スクロールを有効にするかどうか。</summary>
	public bool HorizontalScrollEnabled { get; set; } = false;

	/// <summary>垂直スクロールを有効にするかどうか。</summary>
	public bool VerticalScrollEnabled { get; set; } = true;

	/// <summary>コンテンツコンテナ。子ノードはここに追加します。</summary>
	public Container Content => _content;

	/// <summary>
	/// 垂直スクロールの正規化された位置 (0〜1)。
	/// </summary>
	public float NormalizedVerticalScroll
	{
		get
		{
			var maxY = Math.Max(0, ContentSize.Y - Size.Y);
			return maxY > 0 ? (float)(-_scrollOffset.Y / maxY) : 0;
		}
	}

	/// <summary>
	/// 水平スクロールの正規化された位置 (0〜1)。
	/// </summary>
	public float NormalizedHorizontalScroll
	{
		get
		{
			var maxX = Math.Max(0, ContentSize.X - Size.X);
			return maxX > 0 ? (float)(-_scrollOffset.X / maxX) : 0;
		}
	}

	/// <summary>スタイルを取得または設定します。</summary>
	public IUIStyle<ScrollView> Style
	{
		get => _style;
		set
		{
			_style = value;
			NotifyStyleDirty();
		}
	}

	/// <summary>スクロール位置が変更されたときに発火します。</summary>
	public event Action<Vector>? ScrollChanged;

	/// <summary>
	/// スクロール位置を設定します。
	/// </summary>
	public void ScrollTo(float x, float y)
	{
		SetScrollOffset(x, y);
	}

	/// <summary>
	/// マウスホイール入力を処理します。
	/// UIManager から呼ばれます。
	/// </summary>
	internal void ProcessScroll(Vector scroll)
	{
		var dx = HorizontalScrollEnabled ? -scroll.X * ScrollSpeed : 0;
		var dy = VerticalScrollEnabled ? -scroll.Y * ScrollSpeed : 0;
		SetScrollOffset(_scrollOffset.X + dx, _scrollOffset.Y + dy);
	}

	/// <summary>ドラッグ中かどうか。</summary>
	public bool IsDragging => _isDragging;

	protected override void ApplyStyle(UIElementState state)
	{
		_style.Apply(this, state);
	}

	private void OnPointerPressed(VectorInt position)
	{
		_isDragging = true;
		_dragStartPos = position;
		_dragStartOffset = _scrollOffset;
	}

	private void OnPointerMoved(VectorInt position)
	{
		if (!_isDragging) return;
		var delta = position - _dragStartPos;
		var dx = HorizontalScrollEnabled ? delta.X : 0;
		var dy = VerticalScrollEnabled ? delta.Y : 0;
		SetScrollOffset(_dragStartOffset.X + dx, _dragStartOffset.Y + dy);
	}

	private void OnPointerReleased(VectorInt _)
	{
		_isDragging = false;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		_content.Location = _scrollOffset;
	}

	private void SetScrollOffset(float x, float y)
	{
		var maxX = Math.Max(0, ContentSize.X - Size.X);
		var maxY = Math.Max(0, ContentSize.Y - Size.Y);

		x = Math.Clamp(x, -maxX, 0);
		y = Math.Clamp(y, -maxY, 0);

		var newOffset = new Vector(x, y);
		if (Math.Abs(_scrollOffset.X - newOffset.X) < 0.01f &&
			Math.Abs(_scrollOffset.Y - newOffset.Y) < 0.01f)
			return;

		_scrollOffset = newOffset;
		NotifyStyleDirty();
		ScrollChanged?.Invoke(_scrollOffset);
	}
}
