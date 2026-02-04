using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

[Demo("/debug/container_trim_mode_with_nesting", "コンテナをネストした状態でトリムモードにしたときの挙動確認")]
public class ContainerTrimModeWithNestingTestScene : Scene
{
    private readonly Keyboard _keyboard;
    private readonly Texture2D _texture;

    private readonly Sprite _obj = new();

    private readonly Container _container = new Container()
        .Location(32, 32)
        .Size(200, 150);

    private readonly Container _container2 = new Container()
        .Location(32, 32)
        .Size(32, 32);

    public ContainerTrimModeWithNestingTestScene(Keyboard keyboard)
    {
        _keyboard = keyboard;
        _texture = Window.TextureFactory.Load("assets/ichigo2.png");
        _obj.Texture = _texture;
    }

    public override void OnStart()
    {
        _container2.Add(Shape.CreateRect(0, 0, 31, 31, Color.IndianRed));
        _container2.Add(_obj);

        _container.Add(Shape.CreateRect(0, 0, 199, 149, Color.LightGoldenrodYellow));
        _container.Add(_container2);

        Root.Add(_container);
    }

    public override void OnUpdate()
    {
        // Trimmableきりかえ
        if (_keyboard.X.IsKeyDown)
        {
            _container.IsTrimmable ^= true;
        }
        // Trimmableきりかえ
        if (_keyboard.F.IsKeyDown)
        {
            _container2.IsTrimmable ^= true;
        }

        // キャラを動かす
        if (_keyboard.Up) _obj.Location += Vector.Up;
        if (_keyboard.Down) _obj.Location += Vector.Down;
        if (_keyboard.Left) _obj.Location += Vector.Left;
        if (_keyboard.Right) _obj.Location += Vector.Right;

        // コンテナを動かす
        if (_keyboard.W) _container2.Location += Vector.Up;
        if (_keyboard.S) _container2.Location += Vector.Down;
        if (_keyboard.A) _container2.Location += Vector.Left;
        if (_keyboard.D) _container2.Location += Vector.Right;

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
