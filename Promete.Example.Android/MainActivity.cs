using Android;
using Android.Content.Res;
using Promete.Android;
using Promete.Input;
using Silk.NET.Windowing.Sdl.Android;

namespace Promete.Example.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : SilkActivity
{
	public static AssetManager? CurrentAssets { get; private set; }

	protected override void OnRun()
	{
		CurrentAssets = Assets;
		var app = PrometeApp.Create()
			.Use<Keyboard>()
			.Use<Mouse>()
			.Use<ConsoleLayer>()
			.BuildWithAndroid();

		app.Run<MainScene>();
		app.Dispose();
	}
}
