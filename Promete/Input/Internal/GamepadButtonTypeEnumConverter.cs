using System;
using Silk.NET.Input;

namespace Promete.Input.Internal;

internal static class GamepadButtonTypeEnumConverter
{
    public static ButtonName ToSilk(this GamepadButtonType type)
    {
        return type switch
        {
            GamepadButtonType.DpadUp => ButtonName.DPadUp,
            GamepadButtonType.DpadDown => ButtonName.DPadDown,
            GamepadButtonType.DpadLeft => ButtonName.DPadLeft,
            GamepadButtonType.DpadRight => ButtonName.DPadRight,
            GamepadButtonType.A => ButtonName.A,
            GamepadButtonType.B => ButtonName.B,
            GamepadButtonType.X => ButtonName.X,
            GamepadButtonType.Y => ButtonName.Y,
            GamepadButtonType.L1 => ButtonName.LeftBumper,
            GamepadButtonType.R1 => ButtonName.RightBumper,
            GamepadButtonType.L2 => throw new NotSupportedException("L2 is not a button in Silk.NET"),
            GamepadButtonType.R2 => throw new NotSupportedException("R2 is not a button in Silk.NET"),
            GamepadButtonType.Plus => ButtonName.Start,
            GamepadButtonType.Minus => ButtonName.Back,
            GamepadButtonType.LeftStick => ButtonName.LeftStick,
            GamepadButtonType.RightStick => ButtonName.RightStick,
            GamepadButtonType.Home => ButtonName.Home,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static GamepadButtonType ToPromete(this ButtonName type)
    {
        return type switch
        {
            ButtonName.DPadUp => GamepadButtonType.DpadUp,
            ButtonName.DPadDown => GamepadButtonType.DpadDown,
            ButtonName.DPadLeft => GamepadButtonType.DpadLeft,
            ButtonName.DPadRight => GamepadButtonType.DpadRight,
            ButtonName.A => GamepadButtonType.A,
            ButtonName.B => GamepadButtonType.B,
            ButtonName.X => GamepadButtonType.X,
            ButtonName.Y => GamepadButtonType.Y,
            ButtonName.LeftBumper => GamepadButtonType.L1,
            ButtonName.RightBumper => GamepadButtonType.R1,
            ButtonName.Start => GamepadButtonType.Plus,
            ButtonName.Back => GamepadButtonType.Minus,
            ButtonName.LeftStick => GamepadButtonType.LeftStick,
            ButtonName.RightStick => GamepadButtonType.RightStick,
            ButtonName.Home => GamepadButtonType.Home,
            _ => GamepadButtonType.Unknown
        };
    }
}
