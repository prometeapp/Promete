using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// スプライト等が0, 0に描画されるバグのデバッグシーンです。
/// </summary>
[Demo("debug/SpriteLocationBugDebugScene.demo", "スプライト等が0, 0に描画されるバグのデバッグシーン")]
public class SpriteLocationBugDebugScene(Keyboard keyboard) : Scene
{
    private readonly Container _container = new();
    private Texture2D _texture;

    public override void OnStart()
    {
        Root.Add(_container);
        _texture = Window.TextureFactory.Load("./assets/ichigo.png");
        _container.Location = (100, 100);
        _container.Add(new Sprite(_texture));
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape) App.LoadScene<MainScene>();
    }
}
