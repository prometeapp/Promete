using System;
using System.Collections.Generic;
using Silk.NET.Input;

namespace Promete.Windowing.Headless;

public class DummyInputContext : IInputContext
{
	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public IntPtr Handle => IntPtr.Zero;
	public IReadOnlyList<IGamepad> Gamepads { get; } = [];
	public IReadOnlyList<IJoystick> Joysticks { get; } = [];
	public IReadOnlyList<IKeyboard> Keyboards { get; } = [];
	public IReadOnlyList<IMouse> Mice { get; } = [];
	public IReadOnlyList<IInputDevice> OtherDevices { get; } = [];
	public event Action<IInputDevice, bool>? ConnectionChanged;
}
