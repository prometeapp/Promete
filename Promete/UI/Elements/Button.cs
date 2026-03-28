using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// クリック可能なボタンUI要素です。
/// Shape ベースの背景と Text ラベルで構成されます。
/// </summary>
public class Button : UIElement
{
	private readonly Text _label;
	private IUIStyle<Button> _style;

	/// <summary>
	/// Button の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="text">ボタンに表示するテキスト。</param>
	/// <param name="style">適用するスタイル。null の場合は DefaultButtonStyle が使用されます。</param>
	public Button(string text = "", IUIStyle<Button>? style = null)
	{
		_style = style ?? new DefaultButtonStyle();
		_label = new Text(text);
		Add(_label);
		Size = (120, 40);
	}

	/// <summary>
	/// ボタンのテキスト内容を取得または設定します。
	/// </summary>
	public string TextContent
	{
		get => _label.Content;
		set => _label.Content = value;
	}

	/// <summary>
	/// ラベルの Text ノードを取得します。
	/// フォントや色の直接制御に使用できます。
	/// </summary>
	public Text Label => _label;

	/// <summary>
	/// ボタンのスタイルを取得または設定します。
	/// </summary>
	public IUIStyle<Button> Style
	{
		get => _style;
		set
		{
			_style = value;
			NotifyStyleDirty();
		}
	}

	protected override void ApplyStyle(UIElementState state)
	{
		_style.Apply(this, state);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// ラベルをボタンの中央に配置
		var labelSize = _label.Size;
		if (labelSize.X > 0 && labelSize.Y > 0)
		{
			_label.Location = new Vector(
				(Size.X - labelSize.X) / 2f,
				(Size.Y - labelSize.Y) / 2f
			);
		}
	}
}
