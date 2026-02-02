using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/maskedContainer.demo", "マスクを使って子要素を切り抜く例")]
public class MaskedContainerExampleScene(ConsoleLayer console, Keyboard keyboard, Mouse mouse) : Scene
{
    private Texture2D? _backgroundTexture;
    private Texture2D? _circleMaskTexture;

    public override void OnStart()
    {
        _backgroundTexture = Window.TextureFactory.Load("assets/ichigo2.png");
        _circleMaskTexture = Window.TextureFactory.Load("assets/circle_mask.png");

        // ステンシルバッファ方式のデモ
        var stencilContainer = new MaskedContainer(_circleMaskTexture, useAlphaMask: false)
            .Location(100, 100)
            .Size(32, 32);
        stencilContainer.Add(new Sprite(_backgroundTexture)
            .Location(0, 0));
        Root.Add(stencilContainer);

        // アルファブレンディング方式のデモ
        var alphaContainer = new MaskedContainer(_circleMaskTexture, useAlphaMask: true)
            .Location(450, 100)
            .Size(32, 32);
        alphaContainer.Add(new Sprite(_backgroundTexture)
            .Location(0, 0));
        Root.Add(alphaContainer);

        console.Print("ステンシルバッファ方式（左）とアルファブレンディング方式（右）の比較");
        console.Print("ステンシル方式は高速だが2値マスクのみ");
        console.Print("アルファ方式はグラデーションマスクに対応");
        console.Print("Press ESC to return");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _backgroundTexture?.Dispose();
        _circleMaskTexture?.Dispose();
    }
}
