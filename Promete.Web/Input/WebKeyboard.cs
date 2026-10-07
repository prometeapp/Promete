using Silk.NET.Input;

namespace Promete.Web.Input;

/// <summary>
/// DOM のキーボードイベントを受ける <see cref="IKeyboard"/> です。
/// </summary>
internal sealed class WebKeyboard : IKeyboard
{
    private readonly HashSet<Key> _pressed = [];

    public event Action<IKeyboard, Key, int>? KeyDown;

    public event Action<IKeyboard, Key, int>? KeyUp;

    public event Action<IKeyboard, char>? KeyChar;

    public string Name => "Web Keyboard";

    public int Index => 0;

    public bool IsConnected => true;

    public IReadOnlyList<Key> SupportedKeys { get; } = Enum.GetValues<Key>();

    public string ClipboardText { get; set; } = string.Empty;

    public bool IsKeyPressed(Key key) => _pressed.Contains(key);

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
        KeyUp?.Invoke(this, key, 0);
    }

    internal void Type(char c) => KeyChar?.Invoke(this, c);
}
