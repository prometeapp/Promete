﻿namespace Promete.Input;

public class GamepadButton
{
    internal GamepadButton(GamepadButtonType type)
    {
        Type = type;
    }

    public GamepadButtonType Type { get; }

    /// <summary>
    /// このボタンが押されているかどうかを取得します。
    /// </summary>
    public bool IsPressed { get; internal set; }

    /// <summary>
    /// このボタンが押されてからの経過フレーム数を取得します。
    /// </summary>
    public int ElapsedFrameCount { get; internal set; }

    /// <summary>
    /// このボタンが押されてからの経過時間を取得します。
    /// </summary>
    public float ElapsedTime { get; internal set; }

    /// <summary>
    /// このボタンがこのフレームで押されたかどうかを取得します。
    /// </summary>
    public bool IsButtonDown { get; internal set; }

    /// <summary>
    /// このボタンがこのフレームで離されたかどうかを取得します。
    /// </summary>
    public bool IsButtonUp { get; internal set; }
}
