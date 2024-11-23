using System;

namespace Promete.Coroutines;

/// <summary>
/// A yield instruction that keeps waiting while the specified condition is met.
/// </summary>
public class WaitWhile(Func<bool> condition) : YieldInstruction
{
	public override bool KeepWaiting => condition();
}
