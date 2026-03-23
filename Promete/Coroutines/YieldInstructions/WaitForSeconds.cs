using Promete.Backends;
using Promete.Windowing;

namespace Promete.Coroutines;

/// <summary>
/// 指定時間待機するイールド命令です。
/// </summary>
public class WaitForSeconds(float time) : YieldInstruction
{
    private readonly double _targetTime = time;

    private double? _startTime;
    private ITimeProvider Time => PrometeApp.Current.Time;

    public override bool KeepWaiting
    {
        get
        {
            _startTime ??= Time.TotalTime;
            return Time.TotalTime - _startTime.Value < _targetTime;
        }
    }
}
