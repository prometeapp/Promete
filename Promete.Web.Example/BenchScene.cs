using System.Diagnostics;
using System.Drawing;
using Promete;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Nodes;

namespace Promete.Web.Example;

/// <summary>
/// 描画性能の計測用シーン。スプライトを 20000 個動かしながら、ウォームアップの 30 フレームの後、10 秒間の fps の最小・最大・平均を出す。
/// 結果はコンソールと <see cref="Report"/> に出る。インタプリタと AOT の比較などに使う。
/// </summary>
public class BenchScene : Scene
{
    private const int SpriteCount = 20000;
    private const int WarmupFrames = 30;
    private const double MeasureSeconds = 10;

    private static readonly Angle OneDegree = Angle.FromDegrees(1);

    private readonly List<Sprite> _sprites = [];
    private readonly List<double> _frameTimes = [];
    private readonly Stopwatch _stopwatch = new();
    private readonly Random _random = new(42);
    private Text _status = null!;
    private double _lastSeconds;
    private int _frame;
    private double _measureStart = -1;

    /// <summary>計測の結果。計測中は空文字列。</summary>
    public static string Report { get; private set; } = string.Empty;

    public override void OnStart()
    {
        var texture = App.TextureFactory.Load("assets/ichigo.png");
        var size = App.View.Size;
        for (var i = 0; i < SpriteCount; i++)
        {
            var sprite = new Sprite(texture).Location(_random.Next(size.X), _random.Next(size.Y));
            _sprites.Add(sprite);
            Root.Add(sprite);
        }

        _status = new Text("warming up...", Font.GetDefault(16), Color.White).Location(8, 8);
        Root.Add(_status);
        _stopwatch.Start();
    }

    public override void OnUpdate()
    {
        // 毎フレーム、すべてのスプライトを動かして、更新と描画命令の収集に負荷をかける
        var size = App.View.Size;
        foreach (var sprite in _sprites)
        {
            var x = sprite.Location.X + 1;
            sprite.Location = (x > size.X ? 0 : x, sprite.Location.Y);
            sprite.Angle += OneDegree;
        }

        var now = _stopwatch.Elapsed.TotalSeconds;
        var delta = now - _lastSeconds;
        _lastSeconds = now;
        if (Report.Length > 0)
            return;

        // 起動直後の数フレームは極端に重いので、時間ではなくフレーム数でウォームアップする
        if (++_frame <= WarmupFrames)
        {
            _status.Content = $"warming up... {_frame}/{WarmupFrames}";
            return;
        }

        if (_measureStart < 0)
            _measureStart = now;
        else
            _frameTimes.Add(delta);

        if (now - _measureStart < MeasureSeconds)
        {
            _status.Content = $"measuring... {now - _measureStart:0.0}s";
            return;
        }

        var fps = _frameTimes.Select(t => 1 / t).ToList();
        Report =
            $"sprites={SpriteCount} frames={_frameTimes.Count} "
            + $"fps min={fps.Min():0.0} max={fps.Max():0.0} avg={_frameTimes.Count / _frameTimes.Sum():0.0}";
        _status.Content = Report;
        Console.WriteLine($"[bench] {Report}");
    }
}
