namespace Promete.Coroutines;

/// <summary>
/// A yield instruction that waits for a specified number of seconds.
/// </summary>
public class WaitForSeconds(float time) : YieldInstruction
{
	public override bool KeepWaiting
	{
		get
		{
			// TODO
			return false;
			// startTime ??= Time.Now;
			// return Time.Now - startTime.Value < targetTime;
		}
	}

	private double? startTime;
	private readonly double targetTime = time;
}
