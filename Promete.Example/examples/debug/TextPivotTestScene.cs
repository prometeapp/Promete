using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

[Demo("debug/text-pivot-test", "")]
public class TextPivotTestScene : Scene
{
    private readonly Keyboard _keyboard;
    private readonly Text _textTopLeft, _textCenter, _textBottomRight;
    private int _counter;
    private float _time;

    public TextPivotTestScene(Keyboard keyboard)
    {
        _keyboard = keyboard;
        _textTopLeft = new Text("")
            .Location(320, 64)
            .Pivot(HorizontalAlignment.Left, VerticalAlignment.Center);

        _textCenter = new Text("")
            .Location(192, 128)
            .Pivot(HorizontalAlignment.Center, VerticalAlignment.Center);

        _textBottomRight = new Text("")
            .Location(288, 192)
            .Pivot(HorizontalAlignment.Right, VerticalAlignment.Center);

        Root =
        [
            _textTopLeft,
            _textCenter,
            _textBottomRight
        ];
    }

    public override void OnUpdate()
    {
        _time += Window.DeltaTime;
        if (_time >= 0.5f)
        {
            _time = 0;
            _counter = (_counter + 1) % 3;
            var text = "テスト"[..(_counter + 1)];
            _textTopLeft.Content = text;
            _textCenter.Content = text;
            _textBottomRight.Content = text;
        }

        if (_keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }
}
