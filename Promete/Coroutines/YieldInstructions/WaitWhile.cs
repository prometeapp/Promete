using System;

namespace Promete.Coroutines;

/// <summary>
/// 指定した条件が <c>true</c> の間、待機するイールド命令です。
/// </summary>
public class WaitWhile(Func<bool> condition) : YieldInstruction
{
    public override bool KeepWaiting => condition();
}
