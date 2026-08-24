using System.Collections.Generic;
using Promete.Backends.SilkNetCommon;
using Silk.NET.Input;

namespace Promete.Input;

/// <summary>
/// 接続されたゲームパッドの入力を取得する Promete プラグインです。このクラスは継承できません。
/// </summary>
public sealed class Gamepads(PrometeApp app, InputProvider inputProvider) : IInitializable
{
    private readonly List<Gamepad> _pads = [];
    private IInputContext? _ctx;

    /// <summary>
    /// 指定されたインデックスのゲームパッドを取得します。
    /// </summary>
    /// <param name="index">取得するゲームパッドのインデックス</param>
    /// <returns>ゲームパッドのインスタンス。存在しない場合は null</returns>
    public Gamepad? this[int index] => index < _pads.Count ? _pads[index] : null;

    public void OnStart()
    {
        _ctx = inputProvider.CreateInput();
        UpdateGamepads();
        _ctx.ConnectionChanged += OnConnectionChanged;
    }

    private void OnConnectionChanged(IInputDevice device, bool isConnected)
    {
        if (device is IGamepad)
            UpdateGamepads();
    }

    private void UpdateGamepads()
    {
        _pads.ForEach(p => p.Dispose());
        _pads.Clear();
        foreach (var silkGamepad in _ctx.Gamepads)
            _pads.Add(new Gamepad(silkGamepad, app));
    }
}
