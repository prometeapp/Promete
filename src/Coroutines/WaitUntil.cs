using System;

namespace Promete.Coroutines;

/// <summary>
/// A yield instruction that keeps waiting until the specified condition is met.
/// </summary>
public class WaitUntil(Func<bool> condition) : YieldInstruction
{
	public override bool KeepWaiting => !condition();
}
