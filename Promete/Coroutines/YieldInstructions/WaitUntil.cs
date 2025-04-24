using System;

namespace Promete.Coroutines;

/// <summary>
/// 指定した条件が <c>true</c> を返すまで待機するイールド命令です。
/// </summary>
public class WaitUntil(Func<bool> condition) : YieldInstruction
{
    public override bool KeepWaiting => !condition();
}
