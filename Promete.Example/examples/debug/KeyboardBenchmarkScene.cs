using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Promete.Backends.SilkNetCommon;
using Promete.Example.Kernel;
using Promete.Input;
using Silk.NET.Input;
using SilkKey = Silk.NET.Input.Key;

namespace Promete.Example.examples.debug;

[Demo("/debug/keyboard-benchmark", "BA-001: Keyboard.OnUpdate ベンチマーク")]
public class KeyboardBenchmarkScene(ConsoleLayer console, InputProvider inputProvider, Keyboard keyboard) : Scene
{
    private const int Iterations = 200;
    private const int HistorySize = 60;

    private IKeyboard? _silkKeyboard;

    private readonly SilkKey[] _allKeys = Enum.GetValues<SilkKey>()
        .Where(k => k != SilkKey.Unknown)
        .ToArray();

    private readonly Queue<double> _parallelHistory = new();
    private readonly Queue<double> _foreachHistory = new();

    public override void OnStart()
    {
        var ctx = inputProvider.CreateInput();
        _silkKeyboard = ctx.Keyboards.Count > 0 ? ctx.Keyboards[0] : null;
    }

    public override void OnUpdate()
    {
        if (_silkKeyboard is not { } kb)
        {
            console.Clear();
            console.Print("キーボードが見つかりませんでした。");
            return;
        }

        // Parallel.ForEach 実装
        var t0 = Stopwatch.GetTimestamp();
        for (var i = 0; i < Iterations; i++)
        {
            Parallel.ForEach(_allKeys, k => kb.IsKeyPressed(k));
        }

        var parallelUs = TicksToMicroseconds(Stopwatch.GetTimestamp() - t0) / Iterations;

        // foreach 実装
        t0 = Stopwatch.GetTimestamp();
        for (var i = 0; i < Iterations; i++)
        {
            foreach (SilkKey k in _allKeys)
            {
                kb.IsKeyPressed(k);
            }
        }

        var foreachUs = TicksToMicroseconds(Stopwatch.GetTimestamp() - t0) / Iterations;

        EnqueueHistory(_parallelHistory, parallelUs);
        EnqueueHistory(_foreachHistory, foreachUs);

        var parallelAvg = _parallelHistory.Average();
        var foreachAvg = _foreachHistory.Average();

        console.Clear();
        console.Print($"BA-001: Keyboard.OnUpdate ベンチマーク  ({Iterations} iter/frame, avg {HistorySize}f)");
        console.Print($"対象キー数: {_allKeys.Length}");
        console.Print("");
        console.Print($"  Parallel.ForEach : {parallelUs,8:F2} us  (avg: {parallelAvg:F2} us)");
        console.Print($"  foreach          : {foreachUs,8:F2} us  (avg: {foreachAvg:F2} us)");
        console.Print("");
        console.Print($"  Parallel / foreach 比率: {parallelAvg / foreachAvg:F2}x");
        console.Print("");
        console.Print("[ESC] 戻る");

        if (keyboard.Escape.IsKeyDown)
            App.LoadScene<MainScene>();
    }

    private static double TicksToMicroseconds(long ticks) =>
        (double)ticks / Stopwatch.Frequency * 1_000_000;

    private static void EnqueueHistory(Queue<double> queue, double value)
    {
        queue.Enqueue(value);
        if (queue.Count > HistorySize)
            queue.Dequeue();
    }
}
