using System;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// アンカー (ドロップダウン本体など) の近くに表示するポップアップUI要素です。
/// 最前面に表示され、画面からはみ出さないよう位置が自動で調整されます。
/// </summary>
/// <remarks>
/// 子ノードを追加して中身を作ります。サイズは中身に合わせて自動で決まります。<br />
/// 表示中は、ポップアップの内側だけが入力を受け取ります。
/// 外側のクリックと Esc で閉じます。外側のクリックは、下にある要素には届きません。
/// </remarks>
public class Popup : UIElement
{
    private IUIStyle<Popup> _style;
    private UIManager? _boundManager;
    private UIElement? _anchor;
    private Rect _fixedAnchor;

    /// <summary>
    /// Popup の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="style">適用するスタイル。null の場合は DefaultPopupStyle が使用されます。</param>
    public Popup(IUIStyle<Popup>? style = null)
    {
        _style = style ?? new DefaultPopupStyle();
        IsFocusable = false;
        AutoSize = true;
        ZIndex = 10000;
    }

    /// <summary>ポップアップが表示されたときに発火します。</summary>
    public event Action? Opened;

    /// <summary>ポップアップが閉じられたときに発火します。</summary>
    public event Action? Closed;

    /// <summary>アンカーに対する優先する表示位置。収まらない場合は反対側に表示されます。</summary>
    public PopupSide Side { get; set; } = PopupSide.Bottom;

    /// <summary>アンカーに対する揃え位置。</summary>
    public PopupAlign Align { get; set; } = PopupAlign.Start;

    /// <summary>アンカーとの間隔。</summary>
    public float Gap { get; set; }

    /// <summary>ポップアップの外側をクリックしたときに閉じるかどうか。</summary>
    public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>
    /// Esc キーで閉じるかどうか。
    /// false でも、表示中の Esc 入力はポップアップが受け取ったものとして扱われます
    /// (<see cref="UIManager.IsCancelConsumed"/> が true になります)。
    /// </summary>
    public bool CloseOnCancel { get; set; } = true;

    /// <summary>表示時にフォーカスを移す要素。null の場合はフォーカスを移しません。</summary>
    public UIElement? FocusTarget { get; set; }

    /// <summary>表示中かどうか。</summary>
    public bool IsOpen => _boundManager != null;

    /// <summary>表示中のアンカー。<see cref="ShowAt"/> で表示した場合や、閉じている場合は null。</summary>
    public UIElement? Anchor => _anchor;

    /// <summary>
    /// 現在の表示位置に収まる最大のサイズ。毎フレーム更新されます。
    /// 中身がこれを超える場合は、スクロールなどで収めてください。
    /// </summary>
    public Vector AvailableSize { get; private set; }

    /// <summary>スタイルを取得または設定します。</summary>
    public IUIStyle<Popup> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// アンカーの近くにポップアップを表示します。
    /// 表示中にアンカーが移動すると、ポップアップも追従します。アンカーがツリーから外れると、自動で閉じます。
    /// </summary>
    /// <param name="uiManager">UIManager インスタンス。</param>
    /// <param name="anchor">基準にする要素。</param>
    public void Show(UIManager uiManager, UIElement anchor)
    {
        Open(uiManager, anchor, default);
    }

    /// <summary>
    /// 画面上の指定した位置を基準に、ポップアップを表示します。コンテキストメニューなどに使います。
    /// </summary>
    /// <param name="uiManager">UIManager インスタンス。</param>
    /// <param name="point">基準にする画面上の座標。</param>
    public void ShowAt(UIManager uiManager, Vector point)
    {
        Open(uiManager, null, new Rect(point, Vector.Zero));
    }

    /// <summary>
    /// ポップアップを閉じます。表示前にフォーカスしていた要素がある場合、フォーカスをその要素に戻します。
    /// </summary>
    public void Close()
    {
        if (_boundManager == null)
            return;

        var manager = _boundManager;
        _boundManager = null;
        _anchor = null;

        manager.RemoveLayer(this);
        (Parent as Container)?.Remove(this);
        Closed?.Invoke();
    }

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (_boundManager == null)
            return;

        if (!UpdatePlacement(_boundManager))
            Close();
    }

    private void Open(UIManager uiManager, UIElement? anchor, Rect fixedAnchor)
    {
        if (_boundManager != null)
            return;

        _boundManager = uiManager;
        _anchor = anchor;
        _fixedAnchor = fixedAnchor;

        uiManager.PopupHost.Add(this);
        UpdatePlacement(uiManager);
        uiManager.PushLayer(
            this,
            new UILayerOptions
            {
                OnOutsideClick = () =>
                {
                    if (CloseOnOutsideClick)
                        Close();
                },
                OnCancel = () =>
                {
                    if (CloseOnCancel)
                        Close();
                },
                RestoreFocus = true,
            }
        );

        if (FocusTarget != null)
            uiManager.SetFocus(FocusTarget);

        Opened?.Invoke();
    }

    /// <summary>
    /// アンカーの位置を基に、表示位置を更新します。
    /// </summary>
    /// <returns>アンカーがツリーから外れていて表示を続けられない場合は false。</returns>
    private bool UpdatePlacement(UIManager uiManager)
    {
        Rect anchorRect;
        if (_anchor != null)
        {
            if (!uiManager.IsInTree(_anchor))
                return false;

            anchorRect = _anchor.GetHitRect();
        }
        else
        {
            anchorRect = _fixedAnchor;
        }

        var result = PopupPlacement.Compute(
            anchorRect,
            new Vector(Size.X, Size.Y),
            uiManager.ViewSize,
            Side,
            Align,
            Gap
        );
        Location = result.Location;
        AvailableSize = result.AvailableSize;
        return true;
    }
}
