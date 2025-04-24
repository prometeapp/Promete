using Promete.Windowing;

namespace Promete.Coroutines;

/// <summary>
/// 指定時間待機するイールド命令です。
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
