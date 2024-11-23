using System.Drawing;
using Promete.Nodes;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;

namespace Promete.Example.examples.graphics;

[Demo("graphics/9slicesprite.demo", "9-slice Spriteの例")]
public class NineSliceSpriteTest(ConsoleLayer console, Keyboard keyboard) : Scene
{
	public override void OnStart()
	{
		console.Print("Press ESC to return");

		var font = Font.GetDefault(18);

		var normalSprite = Window.TextureFactory.Load("assets/rect.png");
		var nineSliceSprite = Window.TextureFactory.Load9Sliced("assets/rect.png", 16, 16, 16, 16);

		sprite = new Sprite(normalSprite);
		nineslice = new NineSliceSprite(nineSliceSprite);
		t1 = new Text("Sprite", font, Color.Lime);
		t2 = new Text("9-slice Sprite", font, Color.Lime);

		UpdateLocation();

		Root.AddRange(sprite, nineslice, t1, t2);
	}

	public override void OnUpdate()
	{
		UpdateLocation();

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}

	private void UpdateLocation()
	{
		sprite.Location = (Window.Width / 4 - 128, 64);
		nineslice.Location = (Window.Width / 4 + 32, 64);

		t1.Location = (sprite.Location.X, sprite.Location.Y - 24);
		t2.Location = (nineslice.Location.X, nineslice.Location.Y - 24);

		sprite.Width = nineslice.Width = (int)(64 + 64 * Math.Abs(Math.Sin(Window.TotalTime * 2)));
		sprite.Height = nineslice.Height = (int)(64 + 256 * Math.Abs(Math.Sin(Window.TotalTime * 2)));
	}

#pragma warning disable
	private static Sprite sprite;
	private static NineSliceSprite nineslice;
	private static Text t1;
	private static Text t2;
#pragma warning restore
}
