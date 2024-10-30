using System;
using System.Numerics;
using Promete.Windowing;
using Silk.NET.Input;

using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Promete.Input;

/// <summary>
/// マウスカーソルの位置や、ボタン入力、ホイールスクロールの情報を取得する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed class Mouse
{
	/// <summary>
	/// マウスカーソルの位置を取得します。
	/// </summary>
	public VectorInt Position { get; private set; }

	/// <summary>
	/// マウスホイールのスクロール量を取得します。
	/// </summary>
	public Vector Scroll { get; private set; }

	/// <summary>
	/// 指定したボタンの情報を取得します。
	/// </summary>
	/// <param name="index">ボタン番号。</param>
	public MouseButton this[int index] => buttons[index];

	/// <summary>
	/// 指定したボタンの情報を取得します。
	/// </summary>
	/// <param name="type">ボタンタイプ。</param>
	public MouseButton this[MouseButtonType type] => buttons[(int)type];

	private bool isMouseOnWindow = false;
	private IMouse? mouse;

	private readonly IWindow window;
	private readonly MouseButton[] buttons;

	public Mouse(IWindow window)
	{
		this.window = window;

		var input = window._RawInputContext ?? throw new InvalidOperationException($"{nameof(window._RawInputContext)} is null.");

		if (input.Mice.Count == 0)
		{
			return;
		}

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
		UpdateMouseDevice();
		if (mouse == null) return;
		var wheel = mouse.ScrollWheels[0];
		Scroll = (wheel.X, wheel.Y);
		Position = VectorInt.From(mouse.Position / window.Scale);

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

	private void UpdateMouseDevice()
	{
		// 現在使用中のマウスが切断された場合、イベントを解除する
		if (mouse is { IsConnected: false })
		{
			mouse.Click -= OnMouseClick;
			mouse.MouseDown -= OnMouseDown;
			mouse.MouseUp -= OnMouseUp;
			mouse.MouseMove -= OnMouseMove;
			mouse = null;
		}

		// マウスが存在しない場合、取得を試みる
		if (mouse == null)
		{
			TryFindMouse();
		}
	}

	private void TryFindMouse()
	{
		var input = window._RawInputContext ?? throw new InvalidOperationException($"{nameof(window._RawInputContext)} is null.");
		if (input.Mice.Count == 0) return;

		mouse = input.Mice[0];
		mouse.Click += OnMouseClick;
		mouse.MouseDown += OnMouseDown;
		mouse.MouseUp += OnMouseUp;
		mouse.MouseMove += OnMouseMove;
	}

	private void OnMouseClick(IMouse mouse, SilkMouseButton btn, Vector2 pos)
	{
		var id = (int)btn;
		if (id < 0 || buttons.Length <= id) return;

		Click?.Invoke(new MouseButtonEventArgs(id, (VectorInt)Vector.From(pos / window.Scale)));
	}

	private void OnMouseDown(IMouse mouse, SilkMouseButton btn)
	{
		var id = (int)btn;
		if (id < 0 || buttons.Length <= id) return;

		buttons[id].IsButtonDown = true;
		ButtonDown?.Invoke(new MouseButtonEventArgs(id, VectorInt.From(mouse.Position / window.Scale)));
	}

	private void OnMouseUp(IMouse mouse, SilkMouseButton btn)
	{
		var id = (int)btn;
		if (id < 0 || buttons.Length <= id) return;

		buttons[id].IsButtonUp = true;
		ButtonUp?.Invoke(new MouseButtonEventArgs(id, VectorInt.From(mouse.Position / window.Scale)));
	}

	private void OnMouseMove(IMouse mouse, Vector2 pos)
	{
		pos /= window.Scale;
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
