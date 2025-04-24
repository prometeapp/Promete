using System;
using System.Threading.Tasks;

namespace Promete.Coroutines;

/// <summary>
/// 指定した<see cref="Task"/>、または<see cref="ValueTask"/> が完了するまで待機するイールド命令です。
/// </summary>
public class WaitForTask : YieldInstruction
{
    private readonly Task? _task;
    private readonly ValueTask? _valueTask;

    public WaitForTask(Task task)
    {
        _task = task;
    }

    public WaitForTask(ValueTask task)
    {
        _valueTask = task;
    }

    public override bool KeepWaiting
    {
        get
        {
            if (_task != null)
                return !(_task.IsCanceled || _task.IsCompleted || _task.IsCompletedSuccessfully || _task.IsFaulted);

            if (_valueTask is { } v)
                return !(v.IsCanceled || v.IsCompletedSuccessfully || v.IsCompletedSuccessfully || v.IsFaulted);

            throw new InvalidOperationException("BUG: A WaitForTask yield instruction has no task.");
        }
    }
}
