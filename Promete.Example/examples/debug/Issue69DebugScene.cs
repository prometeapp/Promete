using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// Container: Window.Scaleを1より大きくしているときにIsTrimmableの挙動がおかしい
/// https://github.com/prometeapp/Promete/issues/44
/// </summary>
[Demo("/debug/issue69", "Issue69: Debug Scene")]
public class Issue69DebugScene : Scene
{
    private readonly Keyboard _keyboard;
    private readonly Texture2D _texture;

    private readonly Sprite _obj = new();

    private readonly Container _container = new Container()
        .Location(32, 32)
        .Size(32, 32);

    public Issue69DebugScene(Keyboard keyboard)
    {
        _keyboard = keyboard;
        _texture = Window.TextureFactory.Load("assets/ichigo2.png");
        _obj.Texture = _texture;
    }

    public override void OnStart()
    {
        _container.Add(Shape.CreateRect(0, 0, 31, 31, Color.IndianRed));
        _container.Add(_obj);
        Root.Add(_container);
    }

    public override void OnUpdate()
    {
        // Trimmableきりかえ
        if (_keyboard.X.IsKeyDown)
        {
            _container.IsTrimmable ^= true;
        }

        // キャラを動かす
        if (_keyboard.Up) _obj.Location += Vector.Up;
        if (_keyboard.Down) _obj.Location += Vector.Down;
        if (_keyboard.Left) _obj.Location += Vector.Left;
        if (_keyboard.Right) _obj.Location += Vector.Right;

        // 戻る
        if (_keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }

        if (_keyboard.C.IsKeyDown)
        {
            Window.Scale = Window.Scale == 1 ? 2 : 1;
        }
    }

    public override void OnDestroy()
    {
        _texture.Dispose();
    }
}
