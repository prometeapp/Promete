namespace Promete;

/// <summary>
/// シーン遷移イベントの引数を表します。
/// </summary>
/// <param name="Type">遷移の種類。</param>
/// <param name="Previous">遷移前のシーン。存在しない場合は <see langword="null" />。</param>
/// <param name="Next">遷移後のシーン。存在しない場合は <see langword="null" />。</param>
public record SceneTransitionEventArgs(SceneTransitionType Type, Scene? Previous, Scene? Next);
