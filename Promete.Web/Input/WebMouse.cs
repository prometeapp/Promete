using System.Numerics;
using Silk.NET.Input;

namespace Promete.Web.Input;

/// <summary>
/// DOM のマウスイベントを受ける <see cref="IMouse"/> です。
/// </summary>
internal sealed class WebMouse : IMouse
{
    private readonly HashSet<MouseButton> _pressed = [];
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

    public bool IsButtonPressed(MouseButton btn) => _pressed.Contains(btn);

    internal void Move(float x, float y)
    {
        Position = new Vector2(x, y);
        MouseMove?.Invoke(this, Position);
    }

    internal void Button(MouseButton button, bool down)
    {
        if (down)
        {
            if (_pressed.Add(button))
                MouseDown?.Invoke(this, button);
            return;
        }

        if (!_pressed.Remove(button))
            return;
        MouseUp?.Invoke(this, button);
        Click?.Invoke(this, button, Position);
        _ = DoubleClick;
    }

    internal void Wheel(float dx, float dy)
    {
        var current = _wheels[0];
        _wheels = [new ScrollWheel(current.X + dx, current.Y + dy)];
        Scroll?.Invoke(this, new ScrollWheel(dx, dy));
    }

    internal void ResetWheel() => _wheels = [new ScrollWheel(0, 0)];
}
