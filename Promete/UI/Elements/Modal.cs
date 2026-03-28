using System;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// モーダルダイアログUI要素です。
/// UIManager のモーダルスタックと連携し、モーダル外の操作をブロックします。
/// </summary>
public class Modal : UIElement
{
	private readonly Panel _body;
	private IUIStyle<Modal> _style;
	private UIManager? _boundManager;

	/// <summary>
	/// Modal の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="bodySize">モーダル本体のサイズ。</param>
	/// <param name="style">適用するスタイル。null の場合は DefaultModalStyle が使用されます。</param>
	public Modal(VectorInt? bodySize = null, IUIStyle<Modal>? style = null)
	{
		_style = style ?? new DefaultModalStyle();
		IsFocusable = false;

		var size = bodySize ?? (300, 200);
		_body = new Panel();
		_body.Size = size;
		Add(_body);
	}

	/// <summary>モーダル本体のパネル。子要素はここに追加します。</summary>
	public Panel Body => _body;

	/// <summary>オーバーレイ外クリックで自動的に閉じるかどうか。</summary>
	public bool CloseOnOverlayClick { get; set; } = true;

	/// <summary>スタイルを取得または設定します。</summary>
	public IUIStyle<Modal> Style
	{
		get => _style;
		set
		{
			_style = value;
			NotifyStyleDirty();
		}
	}

	/// <summary>モーダルが閉じられたときに発火します。</summary>
	public event Action? Closed;

	/// <summary>
	/// モーダルを表示します。UIManager のモーダルスタックにプッシュします。
	/// </summary>
	/// <param name="uiManager">UIManager インスタンス。</param>
	/// <param name="parent">モーダルを追加する親ノード。</param>
	public void Show(UIManager uiManager, Container parent)
	{
		parent.Add(this);
		uiManager.PushModal(this);
		_boundManager = uiManager;

		if (CloseOnOverlayClick)
		{
			uiManager.ModalOverlayClicked += OnOverlayClicked;
		}
	}

	/// <summary>
	/// モーダルを閉じます。UIManager のモーダルスタックからポップします。
	/// </summary>
	public void Close()
	{
		if (_boundManager == null) return;

		_boundManager.ModalOverlayClicked -= OnOverlayClicked;
		_boundManager.PopModal();
		_boundManager = null;

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

		// モーダル本体を中央に配置
		_body.Location = new Vector(
			(Size.X - _body.Size.X) / 2f,
			(Size.Y - _body.Size.Y) / 2f
		);
	}

	private void OnOverlayClicked()
	{
		Close();
	}
}
