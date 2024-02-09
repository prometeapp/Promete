using System;
using System.Collections.Generic;
using System.Linq;
using Promete.Input.Internal;
using Promete.Windowing;
using Silk.NET.Input;

namespace Promete.Input;

public sealed class Gamepad : IDisposable
{
	public bool IsConnected => pad.IsConnected;

	public string Name => pad.Name;
	public bool IsVibrationSupported => pad.VibrationMotors.Any();

	public Vector LeftStick => pad.Thumbsticks.Count >= 1 ? (pad.Thumbsticks[0].X, pad.Thumbsticks[0].Y) : (0, 0);

	public Vector RightStick => pad.Thumbsticks.Count >= 2 ? (pad.Thumbsticks[1].X, pad.Thumbsticks[1].Y) : (0, 0);

	public GamepadButton this[int index] => buttons[index];

	public GamepadButton this[GamepadButtonType type] => buttonMap[type];

	public IEnumerable<GamepadButton> AllButtons => buttons.AsEnumerable();

	/// <summary>
	/// 現在押されている全てのボタンを列挙します。
	/// </summary>
	public IEnumerable<GamepadButton> AllPressedButtons => buttons.Where(c => c.IsPressed);

	/// <summary>
	/// このフレームで押された全てのボタンを列挙します。
	/// </summary>
	public IEnumerable<GamepadButton> AllDownButtons => buttons.Where(c => c.IsButtonDown);

	/// <summary>
	/// このフレームで離された全てのボタンを列挙します。
	/// </summary>
	public IEnumerable<GamepadButton> AllUpButtons => buttons.Where(c => c.IsButtonUp);

	private readonly GamepadButton[] buttons;
	private readonly Dictionary<GamepadButtonType, GamepadButton> buttonMap = new();
	private readonly IWindow window;
	private readonly IGamepad pad;

	private const int TriggerCount = 2;

	public Gamepad(IGamepad pad, IWindow window)
	{
		this.pad = pad;
		this.window = window;
		buttons = new GamepadButton[pad.Buttons.Count + TriggerCount];
		for (var i = 0; i < pad.Buttons.Count; i++)
		{
			buttons[i] = new GamepadButton(pad.Buttons[i].Name.ToPromete());
			buttonMap[buttons[i].Type] = buttons[i];
		}

		// Note: XInputの仕様に従い、Prometeでは最大2つのトリガーをL2, R2として解釈する
		buttonMap[GamepadButtonType.L2] = buttons[pad.Buttons.Count + 0] = new GamepadButton(GamepadButtonType.L2);
		buttonMap[GamepadButtonType.R2] = buttons[pad.Buttons.Count + 1] = new GamepadButton(GamepadButtonType.R2);

		pad.ButtonDown += OnButtonDown;
		pad.ButtonUp += OnButtonUp;
		pad.TriggerMoved += OnTriggerMove;

		window.PreUpdate += OnPreUpdate;
		window.PostUpdate += OnPostUpdate;
	}

	public void Vibrate(float value)
	{
		if (!IsVibrationSupported) return;

		foreach (var motor in pad.VibrationMotors)
		{
			motor.Speed = value;
		}
	}

	public void Dispose()
	{
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	private void OnPreUpdate()
	{
		for (var i = 0; i < buttons.Length; i++)
		{
			var isPressed = i < buttons.Length - 2
				? pad.Buttons[i].Pressed
				: pad.Triggers[i - buttons.Length + 2].Position >= 1;
			buttons[i].IsPressed = isPressed;
			buttons[i].ElapsedFrameCount = isPressed ? buttons[i].ElapsedFrameCount + 1 : 0;
			buttons[i].ElapsedTime = isPressed ? buttons[i].ElapsedTime + window.DeltaTime : 0;
		}
	}

	private void OnPostUpdate()
	{
		foreach (var t in buttons)
		{
			t.IsButtonDown = false;
			t.IsButtonUp = false;
		}
	}

	private void OnButtonDown(IGamepad pad, Button button)
	{
		buttons[button.Index].IsButtonDown = true;
	}

	private void OnButtonUp(IGamepad pad, Button button)
	{
		buttons[button.Index].IsButtonUp = true;
	}

	private void OnTriggerMove(IGamepad pad, Trigger trigger)
	{
		var button = buttons[buttons.Length - 2 + trigger.Index];
		if (trigger.Position >= 1)
		{
			button.IsButtonDown = true;
		}
		else
		{
			button.IsButtonUp = true;
		}
	}

	private void ReleaseUnmanagedResources()
	{
		foreach (var motor in pad.VibrationMotors)
		{
			motor.Speed = 0;
		}
		pad.ButtonDown -= OnButtonDown;
		pad.ButtonUp -= OnButtonUp;
		pad.TriggerMoved -= OnTriggerMove;
		window.PreUpdate -= OnPreUpdate;
		window.PostUpdate -= OnPostUpdate;
	}

	~Gamepad()
	{
		ReleaseUnmanagedResources();
	}
}
