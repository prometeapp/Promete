using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Promete.Input.Internal;
using Promete.Windowing;

namespace Promete.Input;

public partial class Keyboard
{
	private readonly IWindow window;

	public Keyboard(IWindow window)
	{
		this.window = window;
		var kb = GetCurrentKeyboard();
		if (kb == null) throw new InvalidOperationException("BUG: RawInputContext is null.");

		kb.KeyChar += (_, e) =>
		{
			keychars.Enqueue(e);
			KeyPress?.Invoke(new KeyPressEventArgs(e));
		};
		kb.KeyDown += (_, e, _) =>
		{
			KeyOf(e.ToPromete()).IsKeyDown = true;
			KeyDown?.Invoke(new KeyEventArgs(e.ToPromete()));
		};
		kb.KeyUp += (_, e, _) =>
		{
			KeyUp?.Invoke(new KeyEventArgs(e.ToPromete()));
			KeyOf(e.ToPromete()).IsKeyUp = true;
		};

		window.PreUpdate += OnPreUpdate;
		window.PostUpdate += OnPostUpdate;
		window.Destroy += OnDestroy;
	}

	/// <summary>
	/// Get all key codes;
	/// </summary>
	public IEnumerable<KeyCode> AllKeyCodes => allCodes;

	/// <summary>
	/// Get all pressed keys.
	/// </summary>
	public IEnumerable<KeyCode> AllPressedKeys => allCodes.Where(c => KeyOf(c).IsPressed);

	/// <summary>
	/// Get all keys which pressed then.
	/// </summary>
	public IEnumerable<KeyCode> AllDownKeys => allCodes.Where(c => KeyOf(c).IsKeyDown);

	/// <summary>
	/// Get all keys which released then.
	/// </summary>
	public IEnumerable<KeyCode> AllUpKeys => allCodes.Where(c => KeyOf(c).IsKeyUp);

	private readonly Queue<char> keychars = new();
	private readonly KeyCode[] allCodes = Enum.GetValues<KeyCode>().Distinct().ToArray();

	/// <summary>
	/// Get input string from the keyboard buffer.
	/// </summary>
	public string GetString()
	{
		if (!HasChar()) return "";

		var buf = new StringBuilder();
		while (HasChar())
			buf.Append(GetChar());
		return buf.ToString();
	}

	/// <summary>
	/// Get an input char from the keyboard buffer.
	/// </summary>
	public char GetChar() => HasChar() ? keychars.Dequeue() : '\0';

	/// <summary>
	/// Check whether some chars in the keyboard buffer.
	/// </summary>
	/// <returns></returns>
	public bool HasChar() => keychars.Count > 0;

	public void OpenVirtualKeyboard()
	{
		GetCurrentKeyboard()?.BeginInput();
	}

	public void CloseVirtualKeyboard()
	{
		GetCurrentKeyboard()?.EndInput();
	}

	private void OnPreUpdate()
	{
		var kb = GetCurrentKeyboard();
		if (kb == null) return;

		Parallel.ForEach(allCodes, keyCode =>
		{
			var silkKey = keyCode.ToSilk();
			if (silkKey < 0) return;
			var isPressed = kb.IsKeyPressed(silkKey);
			var key = KeyOf(keyCode);
			key.IsPressed = isPressed;
			key.ElapsedFrameCount = isPressed ? key.ElapsedFrameCount + 1 : 0;
			key.ElapsedTime = isPressed ? key.ElapsedTime + window.DeltaTime : 0;
		});
	}

	private void OnPostUpdate()
	{
		Parallel.ForEach(allCodes, keyCode =>
		{
			var key = KeyOf(keyCode);
			key.IsKeyDown = false;
			key.IsKeyUp = false;
		});
	}

	private void OnDestroy()
	{
		window.PreUpdate -= OnPreUpdate;
		window.PostUpdate -= OnPostUpdate;
		window.Destroy -= OnDestroy;
	}

	private Silk.NET.Input.IKeyboard? GetCurrentKeyboard()
	{
		var input = window._RawInputContext;
		var kb = input?.Keyboards[0];
		return kb;
	}

	public event Action<KeyEventArgs>? KeyDown;
	public event Action<KeyPressEventArgs>? KeyPress;
	public event Action<KeyEventArgs>? KeyUp;
}
