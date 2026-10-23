using Promete.Assets;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// Spriteのピボットを設定した状態でテクスチャを変更すると、描画位置がおかしくなる
/// https://github.com/prometeapp/Promete/issues/44
/// </summary>
[Demo("/debug/issue44", "Issue44: Debug Scene")]
public class Issue44DebugScene : Scene
{
    private float _time;
    private int _counter;

    private readonly Keyboard _keyboard;
    private readonly Sprite _sprite;
    private readonly Texture2D[] _textures;

    public Issue44DebugScene(Keyboard keyboard, AssetManager assets)
    {
        _keyboard = keyboard;
        _sprite = new Sprite()
            .Location(50, 50)
            .Scale(4, 4)
            .Pivot(HorizontalAlignment.Center, VerticalAlignment.Bottom);

        Root = [_sprite];

        _textures =
        [
            assets.LoadTexture("assets/anim1.png", AssetScope.Scene),
            assets.LoadTexture("assets/anim2.png", AssetScope.Scene),
            assets.LoadTexture("assets/anim3.png", AssetScope.Scene),
            assets.LoadTexture("assets/anim4.png", AssetScope.Scene),
            assets.LoadTexture("assets/anim5.png", AssetScope.Scene),
        ];
    }

    public override void OnUpdate()
    {
        _time += Time.DeltaTime;
        if (_time >= 0.2f)
        {
            _time = 0;
            _counter = (_counter + 1) % _textures.Length;
            _sprite.Texture = _textures[_counter];
        }

        if (_keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }
}
