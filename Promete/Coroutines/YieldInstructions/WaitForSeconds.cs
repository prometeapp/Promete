using Promete.Windowing;

namespace Promete.Coroutines;

/// <summary>
/// A yield instruction that waits for a specified number of seconds.
/// </summary>
public class WaitForSeconds(float time) : YieldInstruction
{
	private IWindow Window => PrometeApp.Current.Window;

	public override bool KeepWaiting
	{
		get
		{
			startTime ??= Window.TotalTime;
			return Window.TotalTime - startTime.Value < targetTime;
		}
	}

	private double? startTime;
	private readonly double targetTime = time;
}
