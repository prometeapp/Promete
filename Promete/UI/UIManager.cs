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
public sealed class UIManager(PrometeApp app, Mouse mouse, Keyboard keyboard)
    : IInitializable,
        IUpdatable
{
    private const string FocusNextAction = "ui.focus_next";
    private const string ActivateAction = "ui.activate";
    private const string CancelAction = "ui.cancel";
    private const string FocusUpAction = "ui.focus_up";
    private const string FocusDownAction = "ui.focus_down";
    private const string FocusLeftAction = "ui.focus_left";
    private const string FocusRightAction = "ui.focus_right";
    private const float SecondaryAxisWeight = 2f;

    private readonly InputMap _inputMap = CreateInputMap();
    private readonly List<UIElement> _elementBuffer = [];
    private readonly List<UILayer> _layers = [];
    private UIElement? _capturedElement;
    private UIElement? _focusedElement;
    private UIElement? _hoveredElement;
    private Vector _lastScroll;
    private UIElement? _pressedElement;

    /// <summary>最上位のモーダルの外側（オーバーレイ領域）がクリックされたときに発火します。</summary>
    public event Action? ModalOverlayClicked;

    /// <summary>
    /// 方向キーによるフォーカス移動を、要素の位置関係に基づく空間ナビゲーションにするかどうかを取得または設定します。
    /// false（デフォルト）の場合、上キーは前の要素、下キーは次の要素への移動になります（左右キーでは移動しません）。
    /// true の場合、押した方向にある最も近い要素へ移動します。
    /// </summary>
    public bool SpatialNavigation { get; set; }

    /// <summary>
    /// このフレームの Esc 入力が、レイヤー (ポップアップなど) の閉じる操作として処理されたかどうかを取得します。
    /// シーン側で Esc を別の用途 (シーンの終了など) に使っている場合、この値が true のフレームは無視してください。
    /// </summary>
    public bool IsCancelConsumed { get; private set; }

    /// <summary>ポップアップなど、最前面に表示する要素の追加先。</summary>
    internal Container PopupHost => app.GlobalForeground;

    /// <summary>画面のサイズ。</summary>
    internal Vector ViewSize => app.View.Size;

    /// <summary>現在マウスがホバーしているUI要素。</summary>
    public UIElement? HoveredElement => _hoveredElement;

    /// <summary>現在フォーカスされているUI要素。</summary>
    public UIElement? FocusedElement => _focusedElement;

    /// <summary>現在のモーダルの最上位要素。モーダルがなければ null。</summary>
    public UIElement? CurrentModal => _layers.LastOrDefault(l => l.Options.IsModal)?.Root;

    /// <summary>現在マウスキャプチャされている要素。</summary>
    public UIElement? CapturedElement => _capturedElement;

    public void OnStart() { }

    public void OnUpdate()
    {
        _inputMap.Update(keyboard);
        IsCancelConsumed = false;

        // UI要素を収集
        _elementBuffer.Clear();
        CollectUIElements(app.GlobalBackground, _elementBuffer);
        if (app.Root != null)
            CollectUIElements(app.Root, _elementBuffer);
        CollectUIElements(app.GlobalForeground, _elementBuffer);

        // レイヤーが存在する場合、最上位のレイヤー内の要素のみを対象にする
        List<UIElement> activeElements;
        if (_layers.Count > 0)
        {
            activeElements = [];
            CollectUIElements(_layers[^1].Root, activeElements);
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

        // Esc で最上位のレイヤーをキャンセル
        ProcessCancel();

        // Enter/Space でフォーカス中の要素をアクティベート（TextInput/Slider フォーカス中は除外）
        ProcessFocusActivation();
    }

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
        PushLayer(modalRoot, new UILayerOptions { IsModal = true, RootIsBackdrop = true });
    }

    /// <summary>
    /// 最上位のモーダルをスタックからポップします。
    /// </summary>
    /// <returns>ポップされたモーダル要素。スタックが空の場合は null。</returns>
    public UIElement? PopModal()
    {
        var layer = _layers.LastOrDefault(l => l.Options.IsModal);
        if (layer == null)
            return null;
        RemoveLayer(layer.Root);
        return layer.Root;
    }

    /// <summary>
    /// 指定したノードが、アプリのノードツリー (シーンのルートまたはグローバルな前景・背景) に属しているかどうかを判定します。
    /// </summary>
    internal bool IsInTree(Node node)
    {
        var top = node;
        while (top.Parent != null)
            top = top.Parent;

        return top == app.Root || top == app.GlobalForeground || top == app.GlobalBackground;
    }

    /// <summary>
    /// レイヤーを最前面に積みます。以降、最上位のレイヤーの子孫だけが入力を受け取ります。
    /// </summary>
    internal void PushLayer(UIElement root, UILayerOptions options)
    {
        var layer = new UILayer(root, options)
        {
            ReturnFocusTo = options.RestoreFocus ? _focusedElement : null,
        };
        _layers.Add(layer);

        // レイヤー外の要素のホバー状態をクリア
        ClearHover();
    }

    /// <summary>
    /// 指定したルートのレイヤーを、スタックのどこにあっても取り除きます。
    /// 最上位のレイヤーだった場合、設定に応じてフォーカスを元の要素へ戻します。
    /// </summary>
    /// <returns>取り除いた場合は true。</returns>
    internal bool RemoveLayer(UIElement root)
    {
        var index = _layers.FindLastIndex(l => l.Root == root);
        if (index < 0)
            return false;

        var layer = _layers[index];
        var wasTop = index == _layers.Count - 1;
        _layers.RemoveAt(index);
        ClearHover();

        if (
            wasTop
            && layer.ReturnFocusTo is { IsEnabled: true, IsVisible: true, IsFocusable: true } target
        )
        {
            SetFocus(target);
        }

        return true;
    }

    /// <summary>
    /// 指定した要素にフォーカスを設定します。
    /// </summary>
    /// <param name="element">フォーカスする要素。null でフォーカスを解除します。</param>
    public void SetFocus(UIElement? element)
    {
        if (_focusedElement == element)
            return;

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
        if (focusable.Count == 0)
            return;
        MoveFocus(focusable, 1);
    }

    /// <summary>
    /// フォーカスを前の要素に移動します（NavigationOrder 逆順）。
    /// </summary>
    public void MoveFocusPrevious()
    {
        var focusable = GetFocusableElements();
        if (focusable.Count == 0)
            return;
        MoveFocus(focusable, -1);
    }

    private void UpdateHover(UIElement? hitElement)
    {
        if (_hoveredElement == hitElement)
            return;

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

        // レイヤーの外側へのクリックか。ルートが背景を兼ねるレイヤーでは、ルート自体へのクリックも外側として扱う
        var topLayer = _layers.Count > 0 ? _layers[^1] : null;
        var isOutsideClick =
            topLayer != null
            && _capturedElement == null
            && (
                hitElement == null
                || (topLayer.Options.RootIsBackdrop && hitElement == topLayer.Root)
            );

        if (leftButton.IsButtonDown)
        {
            if (isOutsideClick)
            {
                topLayer!.Options.OnOutsideClick?.Invoke();
                if (topLayer.Options.IsModal)
                    ModalOverlayClicked?.Invoke();
            }
            else if (effectiveTarget != null)
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

    private void ProcessCancel()
    {
        if (_layers.Count == 0 || !_inputMap.GetAction(CancelAction).IsJustPressed)
            return;

        var onCancel = _layers[^1].Options.OnCancel;
        if (onCancel == null)
            return;

        IsCancelConsumed = true;
        onCancel();
    }

    private void ProcessKeyboardNavigation()
    {
        // TextInput / Slider は左右キーを自身の操作に使うため、左右ではフォーカスを移動しない
        var consumesHorizontal = _focusedElement is TextInput or Slider;

        if (_inputMap.GetAction(FocusUpAction).IsJustPressed)
            MoveFocusDirection(0, -1);
        if (_inputMap.GetAction(FocusDownAction).IsJustPressed)
            MoveFocusDirection(0, 1);
        if (!consumesHorizontal && _inputMap.GetAction(FocusLeftAction).IsJustPressed)
            MoveFocusDirection(-1, 0);
        if (!consumesHorizontal && _inputMap.GetAction(FocusRightAction).IsJustPressed)
            MoveFocusDirection(1, 0);

        if (_inputMap.GetAction(FocusNextAction).IsJustPressed)
        {
            if (keyboard.ShiftLeft.IsPressed || keyboard.ShiftRight.IsPressed)
                MoveFocusPrevious();
            else
                MoveFocusNext();
        }
    }

    private void ProcessScroll(UIElement? hitElement)
    {
        var scroll = -mouse.Scroll;
        _lastScroll = scroll;
        if (Math.Abs(scroll.X) < 0.001f && Math.Abs(scroll.Y) < 0.001f)
            return;

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
                textInput.ProcessInput(keyboard, app.Time.DeltaTime);
                break;
            case Slider slider:
                slider.ProcessKeyboardInput(keyboard);
                break;
        }
    }

    private void ProcessFocusActivation()
    {
        if (_focusedElement == null)
            return;
        // TextInput/Slider フォーカス中は Enter/Space でアクティベートしない
        if (_focusedElement is TextInput or Slider)
            return;

        if (_inputMap.GetAction(ActivateAction).IsJustPressed)
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
            if (!element.IsEnabled || !element.IsVisible)
                continue;
            if (!element.HitTest(point) || IsClippedByScrollView(element, point))
                continue;

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
    /// 要素が、祖先の ScrollView の表示範囲の外にあるために、指定した座標では見えない状態かどうかを判定します。
    /// </summary>
    private static bool IsClippedByScrollView(UIElement element, VectorInt point)
    {
        for (var node = element.Parent; node != null; node = node.Parent)
        {
            if (node is ScrollView scrollView && !scrollView.HitTest(point))
                return true;
        }

        return false;
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

    private void MoveFocusDirection(int dx, int dy)
    {
        if (!SpatialNavigation)
        {
            // 空間ナビゲーションが無効な場合、上下キーは NavigationOrder 順の移動として扱う
            if (dy < 0)
                MoveFocusPrevious();
            else if (dy > 0)
                MoveFocusNext();
            return;
        }

        var focusable = GetFocusableElements();
        if (focusable.Count == 0)
            return;

        if (_focusedElement == null || !focusable.Contains(_focusedElement))
        {
            SetFocus(focusable[0]);
            return;
        }

        var next = FindSpatialNeighbor(_focusedElement, focusable, dx, dy);
        if (next != null)
            SetFocus(next);
    }

    private List<UIElement> GetFocusableElements()
    {
        var focusGroup = _focusedElement?.FocusGroup;
        var source = _layers.Count > 0 ? GetLayerElements() : _elementBuffer;

        return source
            .Where(e =>
                e is { IsEnabled: true, IsFocusable: true, IsVisible: true }
                && e.FocusGroup == focusGroup
            )
            .OrderBy(e => e.NavigationOrder)
            .ToList();
    }

    private List<UIElement> GetLayerElements()
    {
        var list = new List<UIElement>();
        if (_layers.Count > 0)
            CollectUIElements(_layers[^1].Root, list);
        return list;
    }

    private void MoveFocus(List<UIElement> focusable, int direction)
    {
        if (focusable.Count == 0)
            return;

        var currentIndex = _focusedElement != null ? focusable.IndexOf(_focusedElement) : -1;
        var nextIndex = currentIndex + direction;

        // ループ
        if (nextIndex < 0)
            nextIndex = focusable.Count - 1;
        else if (nextIndex >= focusable.Count)
            nextIndex = 0;

        SetFocus(focusable[nextIndex]);
    }

    /// <summary>
    /// 指定した方向にある、中心間距離が最も近い要素を探します。
    /// 進行方向の距離に、直交方向のずれを重み付きで加えたものをスコアとし、最小のものを選びます。
    /// </summary>
    /// <param name="from">移動元の要素。</param>
    /// <param name="candidates">移動先の候補。<paramref name="from"/> 自身は無視されます。</param>
    /// <param name="dx">X方向（-1: 左, 1: 右, 0: なし）。</param>
    /// <param name="dy">Y方向（-1: 上, 1: 下, 0: なし）。</param>
    /// <returns>見つかった要素。該当がなければ null。</returns>
    internal static UIElement? FindSpatialNeighbor(
        UIElement from,
        IEnumerable<UIElement> candidates,
        int dx,
        int dy
    )
    {
        var origin = GetCenter(from);
        UIElement? best = null;
        var bestScore = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (candidate == from)
                continue;

            var center = GetCenter(candidate);
            var primary = dx != 0 ? (center.X - origin.X) * dx : (center.Y - origin.Y) * dy;
            if (primary <= 0)
                continue;

            var secondary = dx != 0 ? Math.Abs(center.Y - origin.Y) : Math.Abs(center.X - origin.X);
            var score = primary + secondary * SecondaryAxisWeight;
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static Vector GetCenter(UIElement element)
    {
        var rect = element.GetHitRect();
        return new Vector(rect.Location.X + rect.Size.X / 2, rect.Location.Y + rect.Size.Y / 2);
    }

    private static InputMap CreateInputMap()
    {
        var map = new InputMap();
        map.AddAction(FocusNextAction);
        map.BindKey(FocusNextAction, KeyCode.Tab);
        map.AddAction(ActivateAction);
        map.BindKey(ActivateAction, KeyCode.Enter).BindKey(ActivateAction, KeyCode.Space);
        map.AddAction(CancelAction);
        map.BindKey(CancelAction, KeyCode.Escape);
        map.AddAction(FocusUpAction);
        map.BindKey(FocusUpAction, KeyCode.Up);
        map.AddAction(FocusDownAction);
        map.BindKey(FocusDownAction, KeyCode.Down);
        map.AddAction(FocusLeftAction);
        map.BindKey(FocusLeftAction, KeyCode.Left);
        map.AddAction(FocusRightAction);
        map.BindKey(FocusRightAction, KeyCode.Right);
        return map;
    }

    private static void CollectUIElements(Node node, List<UIElement> results)
    {
        if (node is UIElement element && element.IsVisible)
        {
            results.Add(element);
        }

        if (node is ContainableNode containable)
        {
            foreach (var child in containable.SortedChildren)
            {
                CollectUIElements(child, results);
            }
        }
    }
}
