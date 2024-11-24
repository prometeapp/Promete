using System.Collections.Generic;
using Promete.Windowing;
using Silk.NET.Input;

namespace Promete.Input;

/// <summary>
///     接続されたゲームパッドの入力を取得する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed class Gamepads
{
    private readonly IInputContext _input;
    private readonly List<Gamepad> _pads = [];

    private readonly IWindow _window;

    public Gamepads(IWindow window)
    {
        _window = window;
        _input = window._RawInputContext!;
        UpdateGamepads();

        _input.ConnectionChanged += OnConnectionChanged;
    }

    public Gamepad? this[int index] => index < _pads.Count ? _pads[index] : null;

    private void OnConnectionChanged(IInputDevice device, bool isConnected)
    {
        if (device is IGamepad) UpdateGamepads();
    }

    private void UpdateGamepads()
    {
        _pads.ForEach(p => p.Dispose());
        _pads.Clear();
        foreach (var silkGamepad in _input.Gamepads) _pads.Add(new Gamepad(silkGamepad, _window));
    }
}
