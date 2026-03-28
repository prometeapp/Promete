using System;
using Promete.Nodes;

namespace Promete.UI;

/// <summary>
/// 全てのUI要素の基底クラスです。
/// <see cref="Container"/> を継承し、ヒットテスト、フォーカス管理、スタイル適用の仕組みを提供します。
/// </summary>
public abstract class UIElement() : Container(isTrimmable: false)
{
	private UIElementState _state = UIElementState.Normal;
	private bool _isStyleDirty = true;
	private Node? _backgroundNode;

    /// <summary>
	/// 現在のインタラクション状態を取得します。
	/// UIManager によって更新されます。
	/// </summary>
	public UIElementState State
	{
		get => _state;
		internal set
		{
			if (_state == value) return;
			_state = value;
			_isStyleDirty = true;
		}
	}

	/// <summary>
	/// 要素が操作可能かどうかを取得または設定します。
	/// false の場合、ヒットテストとフォーカスの対象外となり、Disabled 状態になります。
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// 要素がフォーカス可能かどうかを取得または設定します。
	/// </summary>
	public bool IsFocusable { get; set; } = true;

	/// <summary>
	/// Tab/Shift+Tab によるフォーカスナビゲーションの順序を取得または設定します。
	/// 値が小さいほど先にフォーカスされます。
	/// </summary>
	public int NavigationOrder { get; set; }

	/// <summary>
	/// この要素が属するフォーカスグループを取得または設定します。
	/// null の場合、デフォルトのグローバルスコープに属します。
	/// </summary>
	public FocusGroup? FocusGroup { get; set; }

	/// <summary>Hover 状態かどうか。</summary>
	public bool IsHovered => State.HasFlag(UIElementState.Hovered);

	/// <summary>フォーカス状態かどうか。</summary>
	public bool IsFocused => State.HasFlag(UIElementState.Focused);

	/// <summary>押下状態かどうか。</summary>
	public bool IsPressed => State.HasFlag(UIElementState.Pressed);

	/// <summary>要素がクリックされたときに発火します。</summary>
	public event Action? Clicked;

	/// <summary>マウスカーソルが要素に入ったときに発火します。</summary>
	public event Action? PointerEntered;

	/// <summary>マウスカーソルが要素から出たときに発火します。</summary>
	public event Action? PointerLeft;

	/// <summary>要素にフォーカスが当たったときに発火します。</summary>
	public event Action? GotFocus;

	/// <summary>要素からフォーカスが外れたときに発火します。</summary>
	public event Action? LostFocus;

	/// <summary>要素上でマウスボタンが押されたときに発火します。引数はワールド座標です。</summary>
	public event Action<VectorInt>? PointerPressed;

	/// <summary>要素上でマウスが移動したとき（ドラッグ中）に発火します。引数はワールド座標です。</summary>
	public event Action<VectorInt>? PointerMoved;

	/// <summary>要素上でマウスボタンが離されたときに発火します。引数はワールド座標です。</summary>
	public event Action<VectorInt>? PointerReleased;

	/// <summary>
	/// ヒットテスト用の矩形を取得します。
	/// デフォルトではノードの絶対位置とサイズに基づくAABBを返します。
	/// </summary>
	public virtual Rect GetHitRect()
	{
		var absScale = AbsoluteScale;
		return new Rect(AbsoluteLocation, new Vector(Size.X * absScale.X, Size.Y * absScale.Y));
	}

	/// <summary>
	/// 指定された座標がこの要素のヒット領域内にあるかどうかを判定します。
	/// </summary>
	/// <param name="point">判定する座標（ワールド座標）。</param>
	/// <returns>領域内にある場合は true。</returns>
	public virtual bool HitTest(VectorInt point)
	{
		var rect = GetHitRect();
		return point.X >= rect.Left && point.X < rect.Left + rect.Width
			&& point.Y >= rect.Top && point.Y < rect.Top + rect.Height;
	}

	/// <summary>
	/// 背景ノードを差し替えます。
	/// 古い背景ノードは自動的に削除されます。
	/// </summary>
	/// <param name="node">新しい背景ノード。null で背景を削除します。</param>
	public void SetBackgroundNode(Node? node)
	{
		if (_backgroundNode != null)
		{
			Remove(_backgroundNode);
		}

		_backgroundNode = node;

		if (_backgroundNode != null)
		{
			Insert(0, _backgroundNode);
		}
	}

	/// <summary>
	/// 現在の状態に応じてスタイルを適用します。
	/// サブクラスで実装し、内部のビジュアルノードを更新します。
	/// </summary>
	/// <param name="state">現在のインタラクション状態。</param>
	protected abstract void ApplyStyle(UIElementState state);

	/// <summary>
	/// スタイルの再適用を要求します。
	/// </summary>
	internal void NotifyStyleDirty() => _isStyleDirty = true;

	internal void RaiseClicked() => Clicked?.Invoke();
	internal void RaisePointerEntered() => PointerEntered?.Invoke();
	internal void RaisePointerLeft() => PointerLeft?.Invoke();
	internal void RaiseGotFocus() => GotFocus?.Invoke();
	internal void RaiseLostFocus() => LostFocus?.Invoke();
	internal void RaisePointerPressed(VectorInt position) => PointerPressed?.Invoke(position);
	internal void RaisePointerMoved(VectorInt position) => PointerMoved?.Invoke(position);
	internal void RaisePointerReleased(VectorInt position) => PointerReleased?.Invoke(position);

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (_isStyleDirty)
		{
			ApplyStyle(_state);
			_isStyleDirty = false;
		}
	}
}
