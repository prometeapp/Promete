﻿using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.window;

[Demo("window/property.demo", "ウィンドウの情報を変更する")]
public class property(ConsoleLayer console, Keyboard keyboard) : Scene
{
	public override void OnUpdate()
	{
		console.Clear();
		console.Print($"Position: {Window.Location}");
		console.Print($"Size: {Window.Size}");
		console.Print($"ActualSize: {Window.ActualSize}");
		console.Print($"IsFocused: {Window.IsFocused}");
		console.Print($"IsVisible: {Window.IsVisible}");

		console.Print($"[1]: Set WindowMode to {nameof(WindowMode.Resizable)}");
		console.Print($"[2]: Set WindowMode to {nameof(WindowMode.Fixed)}");
		console.Print($"[3]: Set WindowMode to {nameof(WindowMode.NoFrame)}");
		console.Print("[4]: Toggle Fullscreen");
		console.Print("[ESC]: Exit");

		if (keyboard.Number1.IsKeyDown)
		{
			Window.Mode = WindowMode.Resizable;
		}
		if (keyboard.Number2.IsKeyDown)
		{
			Window.Mode = WindowMode.Fixed;
		}
		if (keyboard.Number3.IsKeyDown)
		{
			Window.Mode = WindowMode.NoFrame;
		}
		if (keyboard.Number4.IsKeyDown)
		{
			Window.IsFullScreen = !Window.IsFullScreen;
		}
		if (keyboard.Escape.IsKeyDown)
		{
			App.LoadScene<MainScene>();
		}
	}
}