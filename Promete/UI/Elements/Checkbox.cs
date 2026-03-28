using System;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// チェックボックスUI要素です。
/// クリックで IsChecked をトグルします。
/// </summary>
public class Checkbox : UIElement
{
	private readonly Text _label;
	private bool _isChecked;
	private IUIStyle<Checkbox> _style;

	/// <summary>
	/// Checkbox の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="label">ラベルテキスト。</param>
	/// <param name="isChecked">初期チェック状態。</param>
	/// <param name="style">適用するスタイル。null の場合は DefaultCheckboxStyle が使用されます。</param>
	public Checkbox(string label = "", bool isChecked = false, IUIStyle<Checkbox>? style = null)
	{
		_style = style ?? new DefaultCheckboxStyle();
		_isChecked = isChecked;
		_label = new Text(label);
		Add(_label);
		Size = (200, 24);

		Clicked += OnClicked;
	}

	/// <summary>チェック状態を取得または設定します。</summary>
	public bool IsChecked
	{
		get => _isChecked;
		set
		{
			if (_isChecked == value) return;
			_isChecked = value;
			NotifyStyleDirty();
			CheckedChanged?.Invoke(_isChecked);
		}
	}

	/// <summary>ラベルテキストを取得または設定します。</summary>
	public string LabelText
	{
		get => _label.Content;
		set => _label.Content = value;
	}

	/// <summary>ラベルの Text ノードを取得します。</summary>
	public Text Label => _label;

	/// <summary>
	/// チェックボックスのスタイルを取得または設定します。
	/// </summary>
	public IUIStyle<Checkbox> Style
	{
		get => _style;
		set
		{
			_style = value;
			NotifyStyleDirty();
		}
	}

	/// <summary>チェック状態が変更されたときに発火します。</summary>
	public event Action<bool>? CheckedChanged;

	/// <summary>チェックボックスのサイズ（正方形の辺の長さ）。</summary>
	public int BoxSize { get; set; } = 20;

	protected override void ApplyStyle(UIElementState state)
	{
		_style.Apply(this, state);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// ラベル位置をチェックボックスの右に配置
		var labelSize = _label.Size;
		if (labelSize.X > 0 && labelSize.Y > 0)
		{
			_label.Location = new Vector(
				BoxSize + 6,
				(BoxSize - labelSize.Y) / 2f
			);
		}
	}

	private void OnClicked()
	{
		IsChecked = !IsChecked;
	}
}
