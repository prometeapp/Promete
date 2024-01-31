using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Promete.Input.Internal;
using Promete.Windowing;

namespace Promete.Input;

/// <summary>
/// キーボード入力を提供する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed partial class Keyboard
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

	private readonly Queue<char> keychars = new();
	private readonly KeyCode[] allCodes = Enum.GetValues<KeyCode>().Distinct().ToArray();

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
		GetCurrentKeyboard()?.BeginInput();
	}

	/// <summary>
	/// モバイル デバイス等で仮想キーボードを閉じます。
	/// </summary>
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
