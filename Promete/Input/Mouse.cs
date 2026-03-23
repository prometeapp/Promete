using System;
using System.Numerics;
using Promete.Backends;
using Promete.Backends.SilkNetCommon;
using Promete.Windowing;
using Silk.NET.Input;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Promete.Input;

/// <summary>
/// マウスカーソルの位置や、ボタン入力、ホイールスクロールの情報を取得する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed class Mouse(PrometeApp app, InputProvider inputProvider) : IInitializable, IUpdatable
{
    private MouseButton[] _buttons = [];

    private bool _isMouseOnWindow;
    private IMouse? _mouse;
    private IInputContext _ctx;

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
    public MouseButton this[int index] => _buttons[index];

    /// <summary>
    /// 指定したボタンの情報を取得します。
    /// </summary>
    /// <param name="type">ボタンタイプ。</param>
    public MouseButton this[MouseButtonType type] => _buttons[(int)type];

    public void OnStart()
    {
        app.PostUpdate += OnPostUpdate;
        app.Destroy += OnDestroy;
        _ctx = inputProvider.CreateInput();

        _buttons = new MouseButton[12];
        for (var i = 0; i < _buttons.Length; i++) _buttons[i] = new MouseButton();
    }

    public void OnUpdate()
    {
        UpdateMouseDevice();
        if (_mouse == null) return;
        var wheel = _mouse.ScrollWheels[0];
        Scroll = (wheel.X, wheel.Y);
        Position = VectorInt.From(_mouse.Position / app.View.Scale);

        for (var i = 0; i < _buttons.Length; i++)
        {
            var isPressed = _mouse.IsButtonPressed((SilkMouseButton)i);
            _buttons[i].IsPressed = isPressed;
            _buttons[i].ElapsedFrameCount = isPressed ? _buttons[i].ElapsedFrameCount + 1 : 0;
            _buttons[i].ElapsedTime = isPressed ? _buttons[i].ElapsedTime + app.Time.DeltaTime : 0;
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

    private void OnDestroy()
    {
        app.PostUpdate -= OnPostUpdate;
        app.Destroy -= OnDestroy;

        if (_mouse == null) return;
        _mouse.Click -= OnMouseClick;
        _mouse.MouseDown -= OnMouseDown;
        _mouse.MouseUp -= OnMouseUp;
        _mouse.MouseMove -= OnMouseMove;
    }

    private void UpdateMouseDevice()
    {
        // 現在使用中のマウスが切断された場合、イベントを解除する
        if (_mouse is { IsConnected: false })
        {
            _mouse.Click -= OnMouseClick;
            _mouse.MouseDown -= OnMouseDown;
            _mouse.MouseUp -= OnMouseUp;
            _mouse.MouseMove -= OnMouseMove;
            _mouse = null;
        }

        // マウスが存在しない場合、取得を試みる
        if (_mouse == null) TryFindMouse();
    }

    private void TryFindMouse()
    {
        if (_ctx.Mice.Count == 0) return;

        _mouse = _ctx.Mice[0];
        _mouse.Click += OnMouseClick;
        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;
    }

    private void OnMouseClick(IMouse mouse, SilkMouseButton btn, Vector2 pos)
    {
        var id = (int)btn;
        if (id < 0 || _buttons.Length <= id) return;

        Click?.Invoke(new MouseButtonEventArgs(id, (VectorInt)Vector.From(pos / (app.View.Scale * app.View.PixelRatio))));
    }

    private void OnMouseDown(IMouse mouse, SilkMouseButton btn)
    {
        var id = (int)btn;
        if (id < 0 || _buttons.Length <= id) return;

        _buttons[id].IsButtonDown = true;
        ButtonDown?.Invoke(new MouseButtonEventArgs(id, VectorInt.From(mouse.Position / (app.View.Scale * app.View.PixelRatio))));
    }

    private void OnMouseUp(IMouse mouse, SilkMouseButton btn)
    {
        var id = (int)btn;
        if (id < 0 || _buttons.Length <= id) return;

        _buttons[id].IsButtonUp = true;
        ButtonUp?.Invoke(new MouseButtonEventArgs(id, VectorInt.From(mouse.Position / (app.View.Scale * app.View.PixelRatio))));
    }

    private void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        pos /= app.View.Scale * app.View.PixelRatio;
        Move?.Invoke(new MouseEventArgs((VectorInt)Vector.From(pos)));

        // マウスが画面に出入りしたときのイベント発火条件をチェックする
        if (pos is { X: >= 0, Y: >= 0 } && pos.X <= app.View.Width && pos.Y <= app.View.Height)
        {
            // 画面内
            if (!_isMouseOnWindow) Enter?.Invoke();
            _isMouseOnWindow = true;
        }
        else
        {
            // 画面外
            if (_isMouseOnWindow) Leave?.Invoke();
            _isMouseOnWindow = false;
        }
    }

    public event Action<MouseButtonEventArgs>? Click;
    public event Action<MouseButtonEventArgs>? ButtonUp;
    public event Action<MouseButtonEventArgs>? ButtonDown;
    public event Action<MouseEventArgs>? Move;
    public event Action? Enter;
    public event Action? Leave;
}
