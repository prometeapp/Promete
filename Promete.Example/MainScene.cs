using System.Drawing;
using Promete;
using Promete.Windowing;

namespace Promete.Example;

public class MainScene(PrometeApp app) : Scene
{
	private int time = 0;
	public override void OnStart()
	{
		app.BackgroundColor = Color.Aqua;
	}

	public override void OnUpdate()
	{
		time++;
		if (time > 300)
		{
			app.Exit();
		}
	}
}
