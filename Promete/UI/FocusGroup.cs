namespace Promete.UI;

/// <summary>
/// フォーカスナビゲーションのスコープを定義するグループです。
/// 同じ FocusGroup に属するUI要素間でのみフォーカスが移動します。
/// </summary>
/// <param name="name">グループの識別名。</param>
public class FocusGroup(string name)
{
	/// <summary>
	/// グループの識別名を取得します。
	/// </summary>
	public string Name { get; } = name;

	public override string ToString() => $"FocusGroup({Name})";
}
