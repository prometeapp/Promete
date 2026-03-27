namespace Promete;

/// <summary>
/// シーン遷移の種類を表します。
/// </summary>
public enum SceneTransitionType
{
	/// <summary>
	/// 現在のシーンを破棄し、新しいシーンを読み込みます。
	/// </summary>
	Load,

	/// <summary>
	/// 現在のシーンをスタックに保存し、新しいシーンを読み込みます。
	/// </summary>
	Push,

	/// <summary>
	/// 現在のシーンを破棄し、スタックからシーンを復帰させます。
	/// </summary>
	Pop,
}
