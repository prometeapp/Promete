namespace Promete.Coroutines;

/// <summary>
/// 次のフレームまで待機するイールド命令です。
/// <c>yield return null;</c> でも同じ動作をします。
/// </summary>
public class WaitUntilNextFrame : YieldInstruction
{
    public override bool KeepWaiting => false;
}
