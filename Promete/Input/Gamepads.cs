using System.Collections.Generic;
using Promete.Windowing;
using Silk.NET.Input;

namespace Promete.Input;

/// <summary>
/// 接続されたゲームパッドの入力を取得する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed class Gamepads
{
	private IInputContext input;
	private List<Gamepad> pads = [];

	private readonly IWindow window;

	public Gamepad? this[int index] => index < pads.Count ? pads[index] : null;

	public Gamepads(IWindow window)
	{
		this.window = window;
		input = window._RawInputContext!;
		UpdateGamepads();

		input.ConnectionChanged += OnConnectionChanged;
	}

	private void OnConnectionChanged(IInputDevice device, bool isConnected)
	{
		if (device is IGamepad) UpdateGamepads();
	}

	private void UpdateGamepads()
	{
		pads.ForEach(p => p.Dispose());
		pads.Clear();
		foreach (var silkGamepad in input.Gamepads)
		{
			pads.Add(new Gamepad(silkGamepad, window));
		}
	}
}
