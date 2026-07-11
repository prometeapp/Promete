using System.Collections;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.window;

[Demo("window/time_scale.demo", "時間スケールを変更する")]
public class Time_scale(Keyboard keyboard, ConsoleLayer console, CoroutineManager coroutine) : Scene
{
    private Sprite? _sprite;
    private Texture2D _ichigo;
    private int _timeScale = 10;

    public override void OnStart()
    {
        _ichigo = App.TextureFactory.Load("assets/ichigo.png");
        _sprite = new Sprite(_ichigo).Name("ichigo").Size(32, 32).Location(120, 120);

        Root.Add(_sprite);

        coroutine.Start(MoveIchigo());
    }

    public override void OnUpdate()
    {
        // キーボードで←を押したら_timeScale--、→を押したら_timeScale++する
        // ただし、0から20の範囲にする
        var addition = keyboard.ShiftLeft.IsPressed ? 10 : 1;
        if (keyboard.Left.IsKeyDown)
        {
            _timeScale = Math.Max(0, _timeScale - addition);
        }
        else if (keyboard.Right.IsKeyDown)
        {
            _timeScale += addition;
        }

        // 時間スケールを変更する
        Time.TimeScale = _timeScale / 10f;

        console.Clear();
        console.Print($"TimeScale: {Time.TimeScale}");
        console.Print($"Time: {Time.TotalTime}");
        console.Print($"Time without Scale: {Time.TotalTimeWithoutScale}");
        console.Print($"DeltaTime: {Time.DeltaTime}");
        console.Print($"FPS: {Time.FramePerSeconds}");
        console.Print($"UPS: {Time.UpdatePerSeconds}");
        console.Print("Press [ESC] to return");

        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }

    public override void OnDestroy()
    {
        _ichigo.Dispose();
    }

    private IEnumerator MoveIchigo()
    {
        // 正方形を描くように移動し続ける
        while (true)
        {
            yield return Move128px(_sprite, Vector.Right, 1f);
            yield return Move128px(_sprite, Vector.Down, 1f);
            yield return Move128px(_sprite, Vector.Left, 1f);
            yield return Move128px(_sprite, Vector.Up, 1f);
        }
    }

    private IEnumerator Move128px(Sprite sprite, Vector direction, float duration)
    {
        var time = 0f;
        var start = sprite.Location;
        while (time < duration)
        {
            sprite.Location = start + (direction * (time / duration) * 128);
            time += Time.DeltaTime;
            yield return null;
        }
    }
}
