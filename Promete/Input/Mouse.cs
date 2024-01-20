using System;
using System.Numerics;
using Promete.Windowing;
using Silk.NET.Input;

using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Promete.Input;

/// <summary>
/// This class gets the mouse cursor position, mouse button status, etc. This class can not be inherited.
/// </summary>
public class Mouse
{
	/// <summary>
	/// Get mouse cursor coordinates.
	/// </summary>
	/// <value>The position.</value>
	public VectorInt Position { get; private set; }

	/// <summary>
	/// Get mouse wheel scroll amount.
	/// </summary>
	/// <value></value>
	public Vector Scroll { get; private set; }

	public MouseButton this[int index] => buttons[index];

	public MouseButton this[MouseButtonType type] => buttons[(int)type];

	private bool isMouseOnWindow = false;
	private IMouse? mouse;

	private readonly IWindow window;
	private readonly MouseButton[] buttons;

	public Mouse(IWindow window)
	{
		this.window = window;

		var input = window._RawInputContext ?? throw new InvalidOperationException($"{nameof(window._RawInputContext)} is null.");

		mouse = input.Mice[0];
		mouse.DoubleClickTime = 0;

		mouse.Click += OnMouseClick;
		mouse.MouseDown += OnMouseDown;
		mouse.MouseUp += OnMouseUp;
		mouse.MouseMove += OnMouseMove;

		window.PreUpdate += OnPreUpdate;
		window.PostUpdate += OnPostUpdate;
		window.Destroy += OnDestroy;

		buttons = new MouseButton[12];
		for (var i = 0; i < buttons.Length; i++)
		{
			buttons[i] = new MouseButton();
		}
	}

	private void OnPreUpdate()
	{
		if (mouse == null) return;
		var wheel = mouse.ScrollWheels[0];
		Scroll = (wheel.X, wheel.Y);
		Position = ((int)mouse.Position.X, (int)mouse.Position.Y);

		for (var i = 0; i < buttons.Length; i++)
		{
			var isPressed = mouse.IsButtonPressed((SilkMouseButton)i);
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

	private void OnDestroy()
	{
		window.PreUpdate -= OnPreUpdate;
		window.PostUpdate -= OnPostUpdate;
		window.Destroy -= OnDestroy;

		if (mouse == null) return;
		mouse.Click -= OnMouseClick;
		mouse.MouseDown -= OnMouseDown;
		mouse.MouseUp -= OnMouseUp;
		mouse.MouseMove -= OnMouseMove;
	}

	private void OnMouseClick(IMouse mouse, SilkMouseButton btn, Vector2 pos)
	{
		var id = (int)btn;
		if (id < 0 || buttons.Length <= id) return;

		Click?.Invoke(new MouseButtonEventArgs(id, (VectorInt)Vector.From(pos)));
	}

	private void OnMouseDown(IMouse mouse, SilkMouseButton btn)
	{
		var id = (int)btn;
		if (id < 0 || buttons.Length <= id) return;

		buttons[id].IsButtonDown = true;
		ButtonDown?.Invoke(new MouseButtonEventArgs(id, VectorInt.From(mouse.Position)));
	}

	private void OnMouseUp(IMouse mouse, SilkMouseButton btn)
	{
		var id = (int)btn;
		if (id < 0 || buttons.Length <= id) return;

		buttons[id].IsButtonUp = true;
		ButtonUp?.Invoke(new MouseButtonEventArgs(id, VectorInt.From(mouse.Position)));
	}

	private void OnMouseMove(IMouse mouse, Vector2 pos)
	{
		Move?.Invoke(new MouseEventArgs((VectorInt)Vector.From(pos)));

		// マウスが画面に出入りしたときのイベント発火条件をチェックする
		if (0 <= pos.X && 0 <= pos.Y && pos.X <= window.Width && pos.Y <= window.Height)
		{
			// 画面内
			if (!isMouseOnWindow) Enter?.Invoke();
			isMouseOnWindow = true;
		}
		else
		{
			// 画面外
			if (isMouseOnWindow) Leave?.Invoke();
			isMouseOnWindow = false;
		}
	}

	public event Action<MouseButtonEventArgs>? Click;
	public event Action<MouseButtonEventArgs>? ButtonUp;
	public event Action<MouseButtonEventArgs>? ButtonDown;
	public event Action<MouseEventArgs>? Move;
	public event Action? Enter;
	public event Action? Leave;
}
