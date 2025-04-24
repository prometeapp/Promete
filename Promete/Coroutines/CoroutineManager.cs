using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Promete.Windowing;

namespace Promete.Coroutines;

/// <summary>
/// コルーチンの実行を制御するクラスです。
/// </summary>
public class CoroutineManager
{
    private readonly Dictionary<Coroutine, YieldInstruction?> _coroutines = new();
    private readonly IWindow _window;

    internal CoroutineManager(PrometeApp app, IWindow window)
    {
        _window = window;
        _window.Update += Update;

        app.SceneWillChange += ClearAllNonKeepAliveCoroutines;
    }

    /// <summary>
    /// 指定されたコルーチンを開始します。
    /// </summary>
    public Coroutine Start(IEnumerator coroutine)
    {
        var c = new Coroutine(coroutine);

        _coroutines[c] = null;
        c.Start();
        return c;
    }

    /// <summary>
    /// 指定されたコルーチンを停止します。
    /// </summary>
    public void Stop(Coroutine coroutine)
    {
        _coroutines.Remove(coroutine);
        coroutine.Stop();
    }

    /// <summary>
    /// 実行中の全てのコルーチンを停止します。
    /// </summary>
    public void Clear()
    {
        // Stop
        _coroutines.Keys.ToList().ForEach(Stop);
    }

    private void Update()
    {
        foreach (var (coroutine, instruction) in _coroutines.Select(c => (c.Key, c.Value)).ToArray())
        {
            if (instruction is { KeepWaiting: true }) continue;
            try
            {
                if (coroutine.MoveNext())
                {
                    _coroutines[coroutine] = ToYieldInstruction(coroutine.Current, coroutine.IsKeepAlive);
                }
                else
                {
                    Stop(coroutine);
                    coroutine.ThenAction?.Invoke();
                }
            }
            catch (Exception ex)
            {
                coroutine.Stop();
                if (coroutine.ErrorAction == null) throw;
                coroutine.ErrorAction.Invoke(ex);
            }
        }
    }

    private YieldInstruction ToYieldInstruction(object? obj, bool isKeepAlive = false)
    {
        return obj switch
        {
            YieldInstruction y => y,
            IEnumerator ie => Start(ie).KeepAlive(isKeepAlive),
            Task t => new WaitForTask(t),
            ValueTask t => new WaitForTask(t),
            _ => new WaitUntilNextFrame()
        };
    }

    private void ClearAllNonKeepAliveCoroutines()
    {
        var nonKeepAliveCoroutines = _coroutines.Keys.Where(c => !c.IsKeepAlive).ToList();
        nonKeepAliveCoroutines.ForEach(Stop);
    }
}
