using System.Drawing;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// 背景付きのコンテナUI要素です。
/// 子ノードをグループ化し、背景色を持たせるために使用します。
/// </summary>
public class Panel : UIElement
{
	private IUIStyle<Panel> _style;

	/// <summary>
	/// Panel の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="style">適用するスタイル。null の場合は DefaultPanelStyle が使用されます。</param>
	public Panel(IUIStyle<Panel>? style = null)
	{
		_style = style ?? new DefaultPanelStyle();
		IsFocusable = false;
	}

	/// <summary>
	/// パネルのスタイルを取得または設定します。
	/// </summary>
	public IUIStyle<Panel> Style
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
}
