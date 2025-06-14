﻿using System.Collections.Generic;
using Promete.Windowing;
using Silk.NET.Input;

namespace Promete.Input;

/// <summary>
/// 接続されたゲームパッドの入力を取得する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed class Gamepads
{
    private readonly IInputContext _input;
    private readonly List<Gamepad> _pads = [];

    private readonly IWindow _window;

    /// <summary>
    /// 指定されたウィンドウでゲームパッド入力を管理するインスタンスを初期化します。
    /// </summary>
    /// <param name="window">ゲームパッド入力を取得するウィンドウ</param>
    public Gamepads(IWindow window)
    {
        _window = window;
        _input = window._RawInputContext!;
        UpdateGamepads();

        _input.ConnectionChanged += OnConnectionChanged;
    }

    /// <summary>
    /// 指定されたインデックスのゲームパッドを取得します。
    /// </summary>
    /// <param name="index">取得するゲームパッドのインデックス</param>
    /// <returns>ゲームパッドのインスタンス。存在しない場合は null</returns>
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
