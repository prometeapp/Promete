using System.Numerics;
using Silk.NET.Core;
using Silk.NET.Input;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// ブラウザの DOM イベントを Silk.NET の <see cref="IInputContext"/> として見せる実装です。
/// キーボードとマウスのみで、ゲームパッドは未対応です。
/// </summary>
public sealed class WebInputContext : IInputContext
{
    public event Action<IInputDevice, bool>? ConnectionChanged;

    public static WebInputContext Instance { get; } = new();

    public IntPtr Handle => IntPtr.Zero;

    public WebKeyboard Keyboard { get; } = new();

    public WebMouse Mouse { get; } = new();

    public IReadOnlyList<IGamepad> Gamepads { get; } = [];

    public IReadOnlyList<IJoystick> Joysticks { get; } = [];

    public IReadOnlyList<IKeyboard> Keyboards => [Keyboard];

    public IReadOnlyList<IMouse> Mice => [Mouse];

    public IReadOnlyList<IInputDevice> OtherDevices { get; } = [];

    /// <summary>
    /// 1 フレームの更新が終わったときに呼びます。同じフレーム内で押して離された入力が、
    /// 更新処理から見えずに消えてしまわないよう、離した状態への反映をここまで遅らせています。
    /// </summary>
    public void EndFrame()
    {
        Keyboard.EndFrame();
        Mouse.EndFrame();
    }

    public void Dispose()
    {
        _ = ConnectionChanged;
    }
}

/// <summary>DOM のキーボードイベントを受ける <see cref="IKeyboard"/> です。</summary>
public sealed class WebKeyboard : IKeyboard
{
    private readonly HashSet<Key> _pressed = [];
    private readonly HashSet<Key> _releasedThisFrame = [];

    public event Action<IKeyboard, Key, int>? KeyDown;

    public event Action<IKeyboard, Key, int>? KeyUp;

    public event Action<IKeyboard, char>? KeyChar;

    public string Name => "Web Keyboard";

    public int Index => 0;

    public bool IsConnected => true;

    public IReadOnlyList<Key> SupportedKeys { get; } = Enum.GetValues<Key>();

    public string ClipboardText { get; set; } = string.Empty;

    public bool IsKeyPressed(Key key) => _pressed.Contains(key) || _releasedThisFrame.Contains(key);

    public bool IsScancodePressed(int scancode) => false;

    public void BeginInput() { }

    public void EndInput() { }

    internal void Press(Key key)
    {
        if (!_pressed.Add(key))
            return;
        KeyDown?.Invoke(this, key, 0);
    }

    internal void Release(Key key)
    {
        if (!_pressed.Remove(key))
            return;
        _releasedThisFrame.Add(key);
        KeyUp?.Invoke(this, key, 0);
    }

    internal void EndFrame() => _releasedThisFrame.Clear();

    internal void Type(char c) => KeyChar?.Invoke(this, c);
}

/// <summary>DOM のマウスイベントを受ける <see cref="IMouse"/> です。</summary>
public sealed class WebMouse : IMouse
{
    private readonly HashSet<MouseButton> _pressed = [];
    private readonly HashSet<MouseButton> _releasedThisFrame = [];
    private ScrollWheel[] _wheels = [new(0, 0)];

    public event Action<IMouse, MouseButton>? MouseDown;

    public event Action<IMouse, MouseButton>? MouseUp;

    public event Action<IMouse, MouseButton, Vector2>? Click;

    public event Action<IMouse, MouseButton, Vector2>? DoubleClick;

    public event Action<IMouse, Vector2>? MouseMove;

    public event Action<IMouse, ScrollWheel>? Scroll;

    public string Name => "Web Mouse";

    public int Index => 0;

    public bool IsConnected => true;

    public IReadOnlyList<MouseButton> SupportedButtons { get; } = Enum.GetValues<MouseButton>();

    public IReadOnlyList<ScrollWheel> ScrollWheels => _wheels;

    public Vector2 Position { get; set; }

    public ICursor Cursor { get; } = new WebCursor();

    public int DoubleClickTime { get; set; } = 500;

    public int DoubleClickRange { get; set; } = 4;

    public bool IsButtonPressed(MouseButton btn) =>
        _pressed.Contains(btn) || _releasedThisFrame.Contains(btn);

    internal void Move(float x, float y)
    {
        Position = new Vector2(x, y);
        MouseMove?.Invoke(this, Position);
    }

    internal void Button(MouseButton button, bool down)
    {
        if (down)
        {
            _pressed.Add(button);
            MouseDown?.Invoke(this, button);
            return;
        }

        _pressed.Remove(button);
        _releasedThisFrame.Add(button);
        MouseUp?.Invoke(this, button);
        Click?.Invoke(this, button, Position);
        _ = DoubleClick;
    }

    internal void EndFrame()
    {
        _releasedThisFrame.Clear();
        _wheels = [new ScrollWheel(0, 0)];
    }

    internal void Wheel(float dx, float dy)
    {
        _wheels = [new ScrollWheel(dx, dy)];
        Scroll?.Invoke(this, _wheels[0]);
    }
}

/// <summary>何もしない <see cref="ICursor"/> です。</summary>
public sealed class WebCursor : ICursor
{
    public CursorType Type { get; set; }

    public StandardCursor StandardCursor { get; set; }

    public CursorMode CursorMode { get; set; }

    public bool IsConfined { get; set; }

    public int HotspotX { get; set; }

    public int HotspotY { get; set; }

    public RawImage Image { get; set; }

    public bool IsSupported(CursorMode mode) => mode == CursorMode.Normal;

    public bool IsSupported(StandardCursor standardCursor) => false;
}
