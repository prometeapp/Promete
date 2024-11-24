using Promete.Windowing;

namespace Promete.Coroutines;

/// <summary>
///     A yield instruction that waits for a specified number of seconds.
/// </summary>
public class WaitForSeconds(float time) : YieldInstruction
{
    private readonly double _targetTime = time;

    private double? _startTime;
    private IWindow Window => PrometeApp.Current.Window;

    public override bool KeepWaiting
    {
        get
        {
            _startTime ??= Window.TotalTime;
            return Window.TotalTime - _startTime.Value < _targetTime;
        }
    }
}
