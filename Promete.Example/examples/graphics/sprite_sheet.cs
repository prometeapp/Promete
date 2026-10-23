using Promete.Assets;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/sprite_sheet.demo", "Sprite Sheet Test")]
public class Sprite_sheet : Scene
{
    private readonly Keyboard _keyboard;
    private Texture2D[] _textures;

    public Sprite_sheet(Keyboard keyboard, AssetManager assets)
    {
        _keyboard = keyboard;
        _textures = assets.LoadSpriteSheet("assets/icons.png", 3, 1, (32, 32), AssetScope.Scene);
        for (int i = 0; i < _textures.Length; i++)
        {
            var sprite = new Sprite(_textures[i]).Location((64 * i) + 16, 16).Scale(2, 2);
            Root.Add(sprite);
        }
    }

    public override void OnUpdate()
    {
        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy() { }
}
