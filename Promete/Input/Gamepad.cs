using System;
using System.Collections.Generic;
using System.Linq;
using Promete.Input.Internal;
using Silk.NET.Input;

namespace Promete.Input;

public sealed class Gamepad : IDisposable
{
    private const int TriggerCount = 2;
    private readonly Dictionary<GamepadButtonType, GamepadButton> _buttonMap = new();

    private readonly GamepadButton[] _buttons;
    private readonly IGamepad _pad;
    private readonly PrometeApp _app;

    public Gamepad(IGamepad pad, PrometeApp app)
    {
        _pad = pad;
        _app = app;
        _buttons = new GamepadButton[pad.Buttons.Count + TriggerCount];
        for (var i = 0; i < pad.Buttons.Count; i++)
        {
            _buttons[i] = new GamepadButton(pad.Buttons[i].Name.ToPromete());
            _buttonMap[_buttons[i].Type] = _buttons[i];
        }

        // Note: XInputの仕様に従い、Prometeでは最大2つのトリガーをL2, R2として解釈する
        _buttonMap[GamepadButtonType.L2] = _buttons[pad.Buttons.Count + 0] = new GamepadButton(
            GamepadButtonType.L2
        );
        _buttonMap[GamepadButtonType.R2] = _buttons[pad.Buttons.Count + 1] = new GamepadButton(
            GamepadButtonType.R2
        );

        pad.ButtonDown += OnButtonDown;
        pad.ButtonUp += OnButtonUp;
        pad.TriggerMoved += OnTriggerMove;

        app.PreUpdate += OnPreUpdate;
        app.PostUpdate += OnPostUpdate;
    }

    ~Gamepad()
    {
        ReleaseUnmanagedResources();
    }

    /// <summary>
    /// ゲームパッドが接続されているかどうかを取得します。
    /// </summary>
    public bool IsConnected => _pad.IsConnected;

    /// <summary>
    /// ゲームパッドの名前を取得します。
    /// </summary>
    public string Name => _pad.Name;

    /// <summary>
    /// ゲームパッドが振動機能をサポートしているかどうかを取得します。
    /// </summary>
    public bool IsVibrationSupported => _pad.VibrationMotors.Any();

    /// <summary>
    /// 左スティックの位置を取得します。
    /// </summary>
    public Vector LeftStick =>
        _pad.Thumbsticks.Count >= 1 ? (_pad.Thumbsticks[0].X, _pad.Thumbsticks[0].Y) : (0, 0);

    /// <summary>
    /// 右スティックの位置を取得します。
    /// </summary>
    public Vector RightStick =>
        _pad.Thumbsticks.Count >= 2 ? (_pad.Thumbsticks[1].X, _pad.Thumbsticks[1].Y) : (0, 0);

    /// <summary>
    /// 全てのボタンを取得します。
    /// </summary>
    public IEnumerable<GamepadButton> AllButtons => _buttons.AsEnumerable();

    /// <summary>
    /// 現在押されている全てのボタンを列挙します。
    /// </summary>
    public IEnumerable<GamepadButton> AllPressedButtons => _buttons.Where(c => c.IsPressed);

    /// <summary>
    /// このフレームで押された全てのボタンを列挙します。
    /// </summary>
    public IEnumerable<GamepadButton> AllDownButtons => _buttons.Where(c => c.IsButtonDown);

    /// <summary>
    /// このフレームで離された全てのボタンを列挙します。
    /// </summary>
    public IEnumerable<GamepadButton> AllUpButtons => _buttons.Where(c => c.IsButtonUp);

    /// <summary>
    /// インデックスを指定してボタンを取得します。
    /// </summary>
    /// <param name="index">ボタンのインデックス</param>
    public GamepadButton this[int index] => _buttons[index];

    /// <summary>
    /// ボタンの種類を指定してボタンを取得します。
    /// </summary>
    /// <param name="type">ボタンの種類</param>
    public GamepadButton this[GamepadButtonType type] => _buttonMap[type];

    /// <summary>
    /// リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// ゲームパッドを振動させます。
    /// </summary>
    /// <param name="value">振動の強さ (0.0～1.0)</param>
    public void Vibrate(float value)
    {
        if (!IsVibrationSupported)
            return;

        foreach (var motor in _pad.VibrationMotors)
            motor.Speed = value;
    }

    private void OnPreUpdate()
    {
        for (var i = 0; i < _buttons.Length; i++)
        {
            var isPressed =
                i < _buttons.Length - 2
                    ? _pad.Buttons[i].Pressed
                    : _pad.Triggers[i - _buttons.Length + 2].Position >= 1;
            _buttons[i].IsPressed = isPressed;
            _buttons[i].ElapsedFrameCount = isPressed ? _buttons[i].ElapsedFrameCount + 1 : 0;
            _buttons[i].ElapsedTime = isPressed ? _buttons[i].ElapsedTime + _app.Time.DeltaTime : 0;
        }
    }

    private void OnPostUpdate()
    {
        foreach (var t in _buttons)
        {
            t.IsButtonDown = false;
            t.IsButtonUp = false;
        }
    }

    private void OnButtonDown(IGamepad pad, Button button)
    {
        _buttons[button.Index].IsButtonDown = true;
    }

    private void OnButtonUp(IGamepad pad, Button button)
    {
        _buttons[button.Index].IsButtonUp = true;
    }

    private void OnTriggerMove(IGamepad pad, Trigger trigger)
    {
        var button = _buttons[_buttons.Length - 2 + trigger.Index];
        if (trigger.Position >= 1)
            button.IsButtonDown = true;
        else
            button.IsButtonUp = true;
    }

    private void ReleaseUnmanagedResources()
    {
        foreach (var motor in _pad.VibrationMotors)
            motor.Speed = 0;
        _pad.ButtonDown -= OnButtonDown;
        _pad.ButtonUp -= OnButtonUp;
        _pad.TriggerMoved -= OnTriggerMove;
        _app.PreUpdate -= OnPreUpdate;
        _app.PostUpdate -= OnPostUpdate;
    }
}
