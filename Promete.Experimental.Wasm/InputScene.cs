using System.Drawing;
using Promete;
using Promete.Experimental.Wasm.Web;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Experimental.Wasm;

/// <summary>
/// 入力の検証用シーン。キーボード (押下中のキー、矢印キーで移動) とマウス (座標、ボタン、ホイール) を表示する。
/// </summary>
public class InputScene(Keyboard keyboard, Mouse mouse) : Scene
{
    private Text _keys = null!;
    private Text _mouseInfo = null!;
    private Text _events = null!;
    private Shape _player = null!;
    private Shape _cursor = null!;
    private Vector _playerPosition = (300, 300);
    private float _wheelY;
    private string _lastEvent = "(なし)";

    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        var font = Font.FromGlyphSource(new CanvasGlyphSource("Misaki"), 16);
        _keys = new Text("keys:", font, Color.White).Location(10, 10);
        _mouseInfo = new Text("mouse:", font, Color.Lime).Location(10, 40);
        _events = new Text("event:", font, Color.Gold).Location(10, 70);
        _player = Shape.CreateRect(0, 0, 31, 31, Color.Orange);
        _cursor = Shape.CreateRect(-4, -4, 4, 4, Color.White);
        Root.AddRange(_keys, _mouseInfo, _events, _player, _cursor);

        keyboard.KeyDown += e => _lastEvent = $"KeyDown {e.Key}";
        keyboard.KeyUp += e => _lastEvent = $"KeyUp {e.Key}";
        keyboard.KeyPress += e => _lastEvent = $"KeyPress '{e.KeyChar}'";
        mouse.ButtonDown += e => _lastEvent = $"MouseDown {e.ButtonType} at {e.Position}";
        mouse.Click += e => _lastEvent = $"Click {e.ButtonType}";
    }

    public override void OnUpdate()
    {
        var speed = 200 * Time.DeltaTime;
        if (keyboard.Left)
            _playerPosition += (-speed, 0);
        if (keyboard.Right)
            _playerPosition += (speed, 0);
        if (keyboard.Up)
            _playerPosition += (0, -speed);
        if (keyboard.Down)
            _playerPosition += (0, speed);
        _player.Location = _playerPosition;

        _wheelY += mouse.Scroll.Y;
        _cursor.Location = (mouse.Position.X, mouse.Position.Y);

        _keys.Content = "keys: " + string.Join(' ', keyboard.AllPressedKeys);
        _mouseInfo.Content =
            $"mouse: {mouse.Position} L={(bool)mouse[MouseButtonType.Left]} R={(bool)mouse[MouseButtonType.Right]} wheelY={_wheelY}";
        _events.Content = "event: " + _lastEvent;
    }
}
