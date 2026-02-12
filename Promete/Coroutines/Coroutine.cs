using System;
using System.Collections;

namespace Promete.Coroutines;

/// <summary>
/// 実行中のコルーチンを表すクラスです。
/// 自身を<c>yield return</c>することで、このコルーチンが完了するまで待機できます。
/// </summary>
public class Coroutine : YieldInstruction
{
    private readonly IEnumerator _runningAction;
    private bool _isExecuting;
    private bool _needsDisposal;

    internal bool IsKeepAlive;

    /// <summary>
    /// コルーチンが実行中かどうかを示す値を取得します。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public override bool KeepWaiting => IsRunning;

    /// <summary>
    /// このコルーチンが完了した後に実行されるコールバックを取得します。
    /// </summary>
    public Action? ThenAction { get; private set; }

    /// <summary>
    /// このコルーチンが例外をスローした場合に実行されるコールバックを取得します。
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

        if (_isExecuting)
        {
            _needsDisposal = true;
        }
        else
        {
            (_runningAction as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// コルーチンが完了した後に実行されるコールバックを設定します。
    /// </summary>
    public Coroutine Then(Action callback)
    {
        ThenAction = callback;
        return this;
    }


    /// <summary>
    /// コルーチンが例外をスローした場合に実行されるコールバックを設定します。
    /// </summary>
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
        _isExecuting = true;
        try
        {
            return _runningAction.MoveNext();
        }
        finally
        {
            _isExecuting = false;
            if (_needsDisposal)
            {
                (_runningAction as IDisposable)?.Dispose();
                _needsDisposal = false;
            }
        }
    }
}
