using System;
using System.Collections.Generic;
using System.Linq;
using Promete.Input;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI;

/// <summary>
/// UIシステムの中核プラグインです。
/// ヒットテスト、フォーカス管理、モーダルスタック、入力ディスパッチを一元管理します。
/// </summary>
public sealed class UIManager(PrometeApp app, Mouse mouse, Keyboard keyboard) : IInitializable, IUpdatable
{
	private UIElement? _hoveredElement;
	private UIElement? _focusedElement;
	private UIElement? _pressedElement;
	private UIElement? _capturedElement;
	private Vector _lastScroll;
	private readonly Stack<UIElement> _modalStack = new();
	private readonly List<UIElement> _elementBuffer = [];

	/// <summary>現在マウスがホバーしているUI要素。</summary>
	public UIElement? HoveredElement => _hoveredElement;

	/// <summary>現在フォーカスされているUI要素。</summary>
	public UIElement? FocusedElement => _focusedElement;

	/// <summary>現在のモーダルスタックの最上位要素。</summary>
	public UIElement? CurrentModal => _modalStack.Count > 0 ? _modalStack.Peek() : null;

	/// <summary>現在マウスキャプチャされている要素。</summary>
	public UIElement? CapturedElement => _capturedElement;

	/// <summary>モーダルのオーバーレイ領域がクリックされたときに発火します。</summary>
	public event Action? ModalOverlayClicked;

	/// <summary>
	/// マウスキャプチャを設定します。
	/// キャプチャ中はマウスが要素外に出てもドラッグイベントが発生し続けます。
	/// </summary>
	public void SetCapture(UIElement? element)
	{
		_capturedElement = element;
	}

	/// <summary>
	/// マウスキャプチャを解放します。
	/// </summary>
	public void ReleaseCapture()
	{
		_capturedElement = null;
	}

	/// <summary>
	/// モーダルをスタックにプッシュします。
	/// プッシュされたモーダルとその子孫のみがイベントを受け取るようになります。
	/// </summary>
	public void PushModal(UIElement modalRoot)
	{
		_modalStack.Push(modalRoot);
		// モーダル外の要素のホバー状態をクリア
		ClearHover();
	}

	/// <summary>
	/// 最上位のモーダルをスタックからポップします。
	/// </summary>
	/// <returns>ポップされたモーダル要素。スタックが空の場合は null。</returns>
	public UIElement? PopModal()
	{
		if (_modalStack.Count == 0) return null;
		var popped = _modalStack.Pop();
		ClearHover();
		return popped;
	}

	/// <summary>
	/// 指定した要素にフォーカスを設定します。
	/// </summary>
	/// <param name="element">フォーカスする要素。null でフォーカスを解除します。</param>
	public void SetFocus(UIElement? element)
	{
		if (_focusedElement == element) return;

		if (_focusedElement != null)
		{
			_focusedElement.State &= ~UIElementState.Focused;
			_focusedElement.RaiseLostFocus();
		}

		_focusedElement = element;

		if (_focusedElement != null)
		{
			_focusedElement.State |= UIElementState.Focused;
			_focusedElement.RaiseGotFocus();
		}
	}

	/// <summary>
	/// フォーカスを次の要素に移動します（NavigationOrder 順）。
	/// </summary>
	public void MoveFocusNext()
	{
		var focusable = GetFocusableElements();
		if (focusable.Count == 0) return;
		MoveFocus(focusable, 1);
	}

	/// <summary>
	/// フォーカスを前の要素に移動します（NavigationOrder 逆順）。
	/// </summary>
	public void MoveFocusPrevious()
	{
		var focusable = GetFocusableElements();
		if (focusable.Count == 0) return;
		MoveFocus(focusable, -1);
	}

	public void OnStart()
	{
	}

	public void OnUpdate()
	{
		// UI要素を収集
		_elementBuffer.Clear();
		CollectUIElements(app.GlobalBackground, _elementBuffer);
		if (app.Root != null) CollectUIElements(app.Root, _elementBuffer);
		CollectUIElements(app.GlobalForeground, _elementBuffer);

		// モーダルが存在する場合、モーダル内の要素のみを対象にする
		List<UIElement> activeElements;
		if (_modalStack.Count > 0)
		{
			var modal = _modalStack.Peek();
			activeElements = [];
			CollectUIElements(modal, activeElements);
			// モーダル自身も対象に含める
			if (modal.IsEnabled)
				activeElements.Add(modal);
		}
		else
		{
			activeElements = _elementBuffer;
		}

		// Disabled 状態の同期
		foreach (var element in _elementBuffer)
		{
			if (!element.IsEnabled)
				element.State |= UIElementState.Disabled;
			else
				element.State &= ~UIElementState.Disabled;
		}

		// ヒットテスト（マウス位置、ZIndex を考慮して最前面の要素を選択）
		var mousePos = mouse.Position;
		var hitElement = HitTest(mousePos, activeElements);

		// Hover 状態の更新
		UpdateHover(hitElement);

		// マウスボタンによる Pressed / Click 処理
		var leftButton = mouse[MouseButtonType.Left];
		ProcessMouseButton(leftButton, hitElement);

		// スクロール処理
		ProcessScroll(hitElement);

		// フォーカス中の要素固有の入力処理
		ProcessFocusedElementInput();

		// キーボードによるフォーカスナビゲーション
		ProcessKeyboardNavigation();

		// Enter/Space でフォーカス中の要素をアクティベート（TextInput/Slider フォーカス中は除外）
		ProcessFocusActivation();
	}

	private void UpdateHover(UIElement? hitElement)
	{
		if (_hoveredElement == hitElement) return;

		if (_hoveredElement != null)
		{
			_hoveredElement.State &= ~UIElementState.Hovered;
			_hoveredElement.RaisePointerLeft();
		}

		_hoveredElement = hitElement;

		if (_hoveredElement != null)
		{
			_hoveredElement.State |= UIElementState.Hovered;
			_hoveredElement.RaisePointerEntered();
		}
	}

	private void ProcessMouseButton(MouseButton leftButton, UIElement? hitElement)
	{
		// キャプチャ中はキャプチャ先にイベントを集中させる
		var effectiveTarget = _capturedElement ?? hitElement;

		if (leftButton.IsButtonDown)
		{
			if (effectiveTarget != null)
			{
				_pressedElement = effectiveTarget;
				effectiveTarget.State |= UIElementState.Pressed;
				effectiveTarget.RaisePointerPressed(mouse.Position);

				// ドラッグ可能な要素はキャプチャを自動設定
				if (effectiveTarget is Slider or ScrollView)
					SetCapture(effectiveTarget);

				// クリックでフォーカスも移動
				if (effectiveTarget.IsFocusable)
					SetFocus(effectiveTarget);
			}
			else if (_modalStack.Count > 0)
			{
				// モーダル外のクリック
				ModalOverlayClicked?.Invoke();
			}
			else
			{
				// 何もない場所をクリック → フォーカス解除
				SetFocus(null);
			}
		}

		// ドラッグ中（ボタン押下中）のマウス移動
		if (leftButton.IsPressed && _pressedElement != null)
		{
			_pressedElement.RaisePointerMoved(mouse.Position);
		}

		if (leftButton.IsButtonUp && _pressedElement != null)
		{
			_pressedElement.State &= ~UIElementState.Pressed;
			_pressedElement.RaisePointerReleased(mouse.Position);

			// キャプチャを解放
			if (_capturedElement == _pressedElement)
				ReleaseCapture();

			// ボタンダウン時と同じ要素上でリリースされた場合のみ Click
			if (_pressedElement == hitElement)
			{
				_pressedElement.RaiseClicked();
			}

			_pressedElement = null;
		}
	}

	private void ProcessKeyboardNavigation()
	{
		if (keyboard.Tab.IsKeyDown)
		{
			if (keyboard.ShiftLeft.IsPressed || keyboard.ShiftRight.IsPressed)
				MoveFocusPrevious();
			else
				MoveFocusNext();
		}
	}

	private void ProcessScroll(UIElement? hitElement)
	{
		var rawScroll = mouse.Scroll;
		var scroll = rawScroll - _lastScroll;
		_lastScroll = rawScroll;
		if (Math.Abs(scroll.X) < 0.001f && Math.Abs(scroll.Y) < 0.001f) return;

		// ヒットした要素、またはその祖先で最も近い ScrollView を探す
		var current = hitElement as Node;
		while (current != null)
		{
			if (current is ScrollView scrollView)
			{
				scrollView.ProcessScroll(scroll);
				return;
			}
			current = current.Parent;
		}
	}

	private void ProcessFocusedElementInput()
	{
		switch (_focusedElement)
		{
			case TextInput textInput:
				textInput.ProcessInput(keyboard);
				break;
			case Slider slider:
				slider.ProcessKeyboardInput(keyboard);
				break;
		}
	}

	private void ProcessFocusActivation()
	{
		if (_focusedElement == null) return;
		// TextInput/Slider フォーカス中は Enter/Space でアクティベートしない
		if (_focusedElement is TextInput or Slider) return;

		if (keyboard.Enter.IsKeyDown || keyboard.Space.IsKeyDown)
		{
			_focusedElement.RaiseClicked();
		}
	}

	private UIElement? HitTest(VectorInt point, List<UIElement> elements)
	{
		// 逆順で走査（後から追加された / ZIndex が高い要素が優先）
		// まず ZIndex 降順でソート
		UIElement? best = null;
		var bestDepth = -1;
		var bestZIndex = int.MinValue;

		for (var i = 0; i < elements.Count; i++)
		{
			var element = elements[i];
			if (!element.IsEnabled || !element.IsVisible) continue;
			if (!element.HitTest(point)) continue;

			var (depth, zIndex) = ComputeEffectiveOrder(element);
			if (zIndex > bestZIndex || (zIndex == bestZIndex && depth > bestDepth))
			{
				best = element;
				bestDepth = depth;
				bestZIndex = zIndex;
			}
		}

		return best;
	}

	/// <summary>
	/// ノードの実効的なZIndex（親のZIndexを累積）とツリー深度を計算します。
	/// </summary>
	private static (int depth, int zIndex) ComputeEffectiveOrder(Node node)
	{
		var depth = 0;
		var zIndex = 0;
		var current = node;
		while (current != null)
		{
			zIndex += current.ZIndex;
			depth++;
			current = current.Parent;
		}
		return (depth, zIndex);
	}

	private void ClearHover()
	{
		if (_hoveredElement != null)
		{
			_hoveredElement.State &= ~UIElementState.Hovered;
			_hoveredElement.RaisePointerLeft();
			_hoveredElement = null;
		}
	}

	private List<UIElement> GetFocusableElements()
	{
		var focusGroup = _focusedElement?.FocusGroup;
		var source = _modalStack.Count > 0 ? GetModalElements() : _elementBuffer;

		return source
			.Where(e => e.IsEnabled && e.IsFocusable && e.IsVisible && e.FocusGroup == focusGroup)
			.OrderBy(e => e.NavigationOrder)
			.ToList();
	}

	private List<UIElement> GetModalElements()
	{
		var list = new List<UIElement>();
		if (_modalStack.Count > 0)
			CollectUIElements(_modalStack.Peek(), list);
		return list;
	}

	private void MoveFocus(List<UIElement> focusable, int direction)
	{
		if (focusable.Count == 0) return;

		var currentIndex = _focusedElement != null ? focusable.IndexOf(_focusedElement) : -1;
		var nextIndex = currentIndex + direction;

		// ループ
		if (nextIndex < 0) nextIndex = focusable.Count - 1;
		else if (nextIndex >= focusable.Count) nextIndex = 0;

		SetFocus(focusable[nextIndex]);
	}

	private static void CollectUIElements(Node node, List<UIElement> results)
	{
		if (node is UIElement element && element.IsVisible)
		{
			results.Add(element);
		}

		if (node is ContainableNode containable)
		{
			foreach (var child in containable.sortedChildren)
			{
				CollectUIElements(child, results);
			}
		}
	}
}
