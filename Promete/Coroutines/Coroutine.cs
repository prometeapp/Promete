using System;
using System.Collections;

namespace Promete.Coroutines;

/// <summary>
/// Coroutine class.
/// </summary>
public class Coroutine : YieldInstruction
{
    private readonly IEnumerator _runningAction;

    internal bool IsKeepAlive;

    /// <summary>
    /// Get whether the coroutine is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    public override bool KeepWaiting => IsRunning;

    /// <summary>
    /// Get the callback to execute after exiting.
    /// </summary>
    public Action? ThenAction { get; private set; }

    /// <summary>
    /// Get the callback that executes when an unhandled exception occurs.
    /// </summary>
    public Action<Exception>? ErrorAction { get; private set; }

    internal object? Current => _runningAction.Current;

    internal Coroutine(IEnumerator runningAction)
    {
        _runningAction = runningAction;
    }

    internal void Start()
    {

        IsRunning = true;
    }

    internal void Stop()
    {
        IsRunning = false;

        // Dispose objects generated in the coroutine if possible
        (_runningAction as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Set the callback after the coroutine ends.
    /// </summary>
    /// <param name="callback">Callback.</param>
    /// <returns></returns>
    public Coroutine Then(Action callback)
    {
        ThenAction = callback;
        return this;
    }


    /// <summary>
    /// Set the callback when the coroutine throws an exception
    /// </summary>
    /// <param name="callback">Callback.</param>
    /// <returns></returns>
    public Coroutine Error(Action<Exception> callback)
    {
        ErrorAction = callback;
        return this;
    }

    /// <summary>
    /// シーンが切り替わっても、コルーチンを破棄せず継続するよう設定します。
    /// </summary>
    public Coroutine KeepAlive(bool keepAlive = true)
    {
        IsKeepAlive = keepAlive;
        return this;
    }

    internal bool MoveNext()
    {
        return _runningAction.MoveNext();
    }
}
