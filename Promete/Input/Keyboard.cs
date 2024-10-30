using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Promete.Input.Internal;
using Promete.Windowing;
using Silk.NET.Input;

namespace Promete.Input;

/// <summary>
/// キーボード入力を提供する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed partial class Keyboard
{
	private readonly IWindow window;

	/// <summary>
	/// 存在する全てのキーコードを列挙します。
	/// </summary>
	public IEnumerable<KeyCode> AllKeyCodes => allCodes;

	/// <summary>
	/// 現在押されている全てのキーを列挙します。
	/// </summary>
	public IEnumerable<KeyCode> AllPressedKeys => allCodes.Where(c => KeyOf(c).IsPressed);

	/// <summary>
	/// このフレームで押された全てのキーを列挙します。
	/// </summary>
	public IEnumerable<KeyCode> AllDownKeys => allCodes.Where(c => KeyOf(c).IsKeyDown);

	/// <summary>
	/// このフレームで離された全てのキーを列挙します。
	/// </summary>
	public IEnumerable<KeyCode> AllUpKeys => allCodes.Where(c => KeyOf(c).IsKeyUp);

	private IKeyboard? _currentKeyboard;

	private readonly Queue<char> keychars = new();
	private readonly KeyCode[] allCodes = Enum.GetValues<KeyCode>().Distinct().ToArray();

	public Keyboard(IWindow window)
	{
		this.window = window;
		TryFindKeyboard();

		window.PreUpdate += OnPreUpdate;
		window.PostUpdate += OnPostUpdate;
		window.Destroy += OnDestroy;
	}

	/// <summary>
	/// キーボードバッファに蓄積されている、入力された文字列を取得します。
	/// 呼び出した時点でバッファはクリアされます。
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
	/// キーボードバッファに蓄積されている、入力された文字を取得します。
	/// 呼び出した時点でその文字はバッファから削除されます。
	/// </summary>
	public char GetChar() => HasChar() ? keychars.Dequeue() : '\0';

	/// <summary>
	/// キーボードバッファにデータが存在するかどうかを取得します。
	/// </summary>
	/// <returns></returns>
	public bool HasChar() => keychars.Count > 0;

	/// <summary>
	/// モバイル デバイス等で仮想キーボードを開きます。
	/// </summary>
	public void OpenVirtualKeyboard()
	{
		_currentKeyboard?.BeginInput();
	}

	/// <summary>
	/// モバイル デバイス等で仮想キーボードを閉じます。
	/// </summary>
	public void CloseVirtualKeyboard()
	{
		_currentKeyboard?.EndInput();
	}

	private void OnPreUpdate()
	{
		if (_currentKeyboard is { IsConnected: false })
		{
			_currentKeyboard.KeyDown -= OnKeyDown;
			_currentKeyboard.KeyUp -= OnKeyUp;
			_currentKeyboard.KeyChar -= OnKeyChar;
			_currentKeyboard = null;
		}

		if (_currentKeyboard == null) TryFindKeyboard();
		if (_currentKeyboard == null) return;

		Parallel.ForEach(allCodes, keyCode =>
		{
			var silkKey = keyCode.ToSilk();
			if (silkKey < 0) return;
			var isPressed = _currentKeyboard.IsKeyPressed(silkKey);
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

	private void TryFindKeyboard()
	{
		var input = window._RawInputContext ?? throw new InvalidOperationException($"{nameof(window._RawInputContext)} is null.");
		if (input.Keyboards.Count == 0) return;

		_currentKeyboard = input.Keyboards[0];
		_currentKeyboard.KeyDown += OnKeyDown;
		_currentKeyboard.KeyUp += OnKeyUp;
		_currentKeyboard.KeyChar += OnKeyChar;
	}

	private void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key e, int i)
	{
		KeyUp?.Invoke(new KeyEventArgs(e.ToPromete()));
		KeyOf(e.ToPromete()).IsKeyUp = true;
	}

	private void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key e, int i)
	{
		KeyOf(e.ToPromete()).IsKeyDown = true;
		KeyDown?.Invoke(new KeyEventArgs(e.ToPromete()));
	}

	private void OnKeyChar(IKeyboard _, char e)
	{
		keychars.Enqueue(e);
		KeyPress?.Invoke(new KeyPressEventArgs(e));
	}

	public event Action<KeyEventArgs>? KeyDown;
	public event Action<KeyPressEventArgs>? KeyPress;
	public event Action<KeyEventArgs>? KeyUp;
}
