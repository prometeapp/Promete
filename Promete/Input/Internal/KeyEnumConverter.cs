namespace Promete.Input.Internal;

internal static class KeyEnumConverter
{
    public static Silk.NET.Input.Key ToSilk(this KeyCode code)
    {
        return code switch
        {
            KeyCode.Unknown => Silk.NET.Input.Key.Unknown,
            KeyCode.ShiftLeft => Silk.NET.Input.Key.ShiftLeft,
            KeyCode.ShiftRight => Silk.NET.Input.Key.ShiftRight,
            KeyCode.ControlLeft => Silk.NET.Input.Key.ControlLeft,
            KeyCode.ControlRight => Silk.NET.Input.Key.ControlRight,
            KeyCode.AltLeft => Silk.NET.Input.Key.AltLeft,
            KeyCode.AltRight => Silk.NET.Input.Key.AltRight,
            KeyCode.WinLeft => Silk.NET.Input.Key.SuperLeft,
            KeyCode.WinRight => Silk.NET.Input.Key.SuperRight,
            KeyCode.Menu => Silk.NET.Input.Key.Menu,
            KeyCode.F1 => Silk.NET.Input.Key.F1,
            KeyCode.F2 => Silk.NET.Input.Key.F2,
            KeyCode.F3 => Silk.NET.Input.Key.F3,
            KeyCode.F4 => Silk.NET.Input.Key.F4,
            KeyCode.F5 => Silk.NET.Input.Key.F5,
            KeyCode.F6 => Silk.NET.Input.Key.F6,
            KeyCode.F7 => Silk.NET.Input.Key.F7,
            KeyCode.F8 => Silk.NET.Input.Key.F8,
            KeyCode.F9 => Silk.NET.Input.Key.F9,
            KeyCode.F10 => Silk.NET.Input.Key.F10,
            KeyCode.F11 => Silk.NET.Input.Key.F11,
            KeyCode.F12 => Silk.NET.Input.Key.F12,
            KeyCode.F13 => Silk.NET.Input.Key.F13,
            KeyCode.F14 => Silk.NET.Input.Key.F14,
            KeyCode.F15 => Silk.NET.Input.Key.F15,
            KeyCode.F16 => Silk.NET.Input.Key.F16,
            KeyCode.F17 => Silk.NET.Input.Key.F17,
            KeyCode.F18 => Silk.NET.Input.Key.F18,
            KeyCode.F19 => Silk.NET.Input.Key.F19,
            KeyCode.F20 => Silk.NET.Input.Key.F20,
            KeyCode.F21 => Silk.NET.Input.Key.F21,
            KeyCode.F22 => Silk.NET.Input.Key.F22,
            KeyCode.F23 => Silk.NET.Input.Key.F23,
            KeyCode.F24 => Silk.NET.Input.Key.F24,
            KeyCode.F25 => Silk.NET.Input.Key.F25,
            KeyCode.Up => Silk.NET.Input.Key.Up,
            KeyCode.Down => Silk.NET.Input.Key.Down,
            KeyCode.Left => Silk.NET.Input.Key.Left,
            KeyCode.Right => Silk.NET.Input.Key.Right,
            KeyCode.Enter => Silk.NET.Input.Key.Enter,
            KeyCode.Escape => Silk.NET.Input.Key.Escape,
            KeyCode.Space => Silk.NET.Input.Key.Space,
            KeyCode.Tab => Silk.NET.Input.Key.Tab,
            KeyCode.BackSpace => Silk.NET.Input.Key.Backspace,
            KeyCode.Insert => Silk.NET.Input.Key.Insert,
            KeyCode.Delete => Silk.NET.Input.Key.Delete,
            KeyCode.PageUp => Silk.NET.Input.Key.PageUp,
            KeyCode.PageDown => Silk.NET.Input.Key.PageDown,
            KeyCode.Home => Silk.NET.Input.Key.Home,
            KeyCode.End => Silk.NET.Input.Key.End,
            KeyCode.CapsLock => Silk.NET.Input.Key.CapsLock,
            KeyCode.ScrollLock => Silk.NET.Input.Key.ScrollLock,
            KeyCode.PrintScreen => Silk.NET.Input.Key.PrintScreen,
            KeyCode.Pause => Silk.NET.Input.Key.Pause,
            KeyCode.NumLock => Silk.NET.Input.Key.NumLock,
            KeyCode.Keypad0 => Silk.NET.Input.Key.Keypad0,
            KeyCode.Keypad1 => Silk.NET.Input.Key.Keypad1,
            KeyCode.Keypad2 => Silk.NET.Input.Key.Keypad2,
            KeyCode.Keypad3 => Silk.NET.Input.Key.Keypad3,
            KeyCode.Keypad4 => Silk.NET.Input.Key.Keypad4,
            KeyCode.Keypad5 => Silk.NET.Input.Key.Keypad5,
            KeyCode.Keypad6 => Silk.NET.Input.Key.Keypad6,
            KeyCode.Keypad7 => Silk.NET.Input.Key.Keypad7,
            KeyCode.Keypad8 => Silk.NET.Input.Key.Keypad8,
            KeyCode.Keypad9 => Silk.NET.Input.Key.Keypad9,
            KeyCode.KeypadDivide => Silk.NET.Input.Key.KeypadDivide,
            KeyCode.KeypadMultiply => Silk.NET.Input.Key.KeypadMultiply,
            KeyCode.KeypadMinus => Silk.NET.Input.Key.KeypadSubtract,
            KeyCode.KeypadPlus => Silk.NET.Input.Key.KeypadAdd,
            KeyCode.KeypadPeriod => Silk.NET.Input.Key.KeypadDecimal,
            KeyCode.KeypadEnter => Silk.NET.Input.Key.KeypadEnter,
            KeyCode.A => Silk.NET.Input.Key.A,
            KeyCode.B => Silk.NET.Input.Key.B,
            KeyCode.C => Silk.NET.Input.Key.C,
            KeyCode.D => Silk.NET.Input.Key.D,
            KeyCode.E => Silk.NET.Input.Key.E,
            KeyCode.F => Silk.NET.Input.Key.F,
            KeyCode.G => Silk.NET.Input.Key.G,
            KeyCode.H => Silk.NET.Input.Key.H,
            KeyCode.I => Silk.NET.Input.Key.I,
            KeyCode.J => Silk.NET.Input.Key.J,
            KeyCode.K => Silk.NET.Input.Key.K,
            KeyCode.L => Silk.NET.Input.Key.L,
            KeyCode.M => Silk.NET.Input.Key.M,
            KeyCode.N => Silk.NET.Input.Key.N,
            KeyCode.O => Silk.NET.Input.Key.O,
            KeyCode.P => Silk.NET.Input.Key.P,
            KeyCode.Q => Silk.NET.Input.Key.Q,
            KeyCode.R => Silk.NET.Input.Key.R,
            KeyCode.S => Silk.NET.Input.Key.S,
            KeyCode.T => Silk.NET.Input.Key.T,
            KeyCode.U => Silk.NET.Input.Key.U,
            KeyCode.V => Silk.NET.Input.Key.V,
            KeyCode.W => Silk.NET.Input.Key.W,
            KeyCode.X => Silk.NET.Input.Key.X,
            KeyCode.Y => Silk.NET.Input.Key.Y,
            KeyCode.Z => Silk.NET.Input.Key.Z,
            KeyCode.Number0 => Silk.NET.Input.Key.Number0,
            KeyCode.Number1 => Silk.NET.Input.Key.Number1,
            KeyCode.Number2 => Silk.NET.Input.Key.Number2,
            KeyCode.Number3 => Silk.NET.Input.Key.Number3,
            KeyCode.Number4 => Silk.NET.Input.Key.Number4,
            KeyCode.Number5 => Silk.NET.Input.Key.Number5,
            KeyCode.Number6 => Silk.NET.Input.Key.Number6,
            KeyCode.Number7 => Silk.NET.Input.Key.Number7,
            KeyCode.Number8 => Silk.NET.Input.Key.Number8,
            KeyCode.Number9 => Silk.NET.Input.Key.Number9,
            KeyCode.Minus => Silk.NET.Input.Key.Minus,
            KeyCode.BracketLeft => Silk.NET.Input.Key.LeftBracket,
            KeyCode.BracketRight => Silk.NET.Input.Key.RightBracket,
            KeyCode.Semicolon => Silk.NET.Input.Key.Semicolon,
            KeyCode.Quote => Silk.NET.Input.Key.Apostrophe,
            KeyCode.Comma => Silk.NET.Input.Key.Comma,
            KeyCode.Tilde => Silk.NET.Input.Key.GraveAccent,
            KeyCode.Period => Silk.NET.Input.Key.Period,
            KeyCode.Slash => Silk.NET.Input.Key.Slash,
            KeyCode.BackSlash => Silk.NET.Input.Key.BackSlash,
            _ => Silk.NET.Input.Key.Unknown
        };
    }

    public static KeyCode ToPromete(this Silk.NET.Input.Key code)
    {
        return code switch
        {
            Silk.NET.Input.Key.Unknown => KeyCode.Unknown,
            Silk.NET.Input.Key.Space => KeyCode.Space,
            Silk.NET.Input.Key.Apostrophe => KeyCode.Quote,
            Silk.NET.Input.Key.Comma => KeyCode.Comma,
            Silk.NET.Input.Key.Minus => KeyCode.Minus,
            Silk.NET.Input.Key.Period => KeyCode.Period,
            Silk.NET.Input.Key.Slash => KeyCode.Slash,
            Silk.NET.Input.Key.Number0 => KeyCode.Number0,
            Silk.NET.Input.Key.Number1 => KeyCode.Number1,
            Silk.NET.Input.Key.Number2 => KeyCode.Number2,
            Silk.NET.Input.Key.Number3 => KeyCode.Number3,
            Silk.NET.Input.Key.Number4 => KeyCode.Number4,
            Silk.NET.Input.Key.Number5 => KeyCode.Number5,
            Silk.NET.Input.Key.Number6 => KeyCode.Number6,
            Silk.NET.Input.Key.Number7 => KeyCode.Number7,
            Silk.NET.Input.Key.Number8 => KeyCode.Number8,
            Silk.NET.Input.Key.Number9 => KeyCode.Number9,
            Silk.NET.Input.Key.Semicolon => KeyCode.Semicolon,
            Silk.NET.Input.Key.A => KeyCode.A,
            Silk.NET.Input.Key.B => KeyCode.B,
            Silk.NET.Input.Key.C => KeyCode.C,
            Silk.NET.Input.Key.D => KeyCode.D,
            Silk.NET.Input.Key.E => KeyCode.E,
            Silk.NET.Input.Key.F => KeyCode.F,
            Silk.NET.Input.Key.G => KeyCode.G,
            Silk.NET.Input.Key.H => KeyCode.H,
            Silk.NET.Input.Key.I => KeyCode.I,
            Silk.NET.Input.Key.J => KeyCode.J,
            Silk.NET.Input.Key.K => KeyCode.K,
            Silk.NET.Input.Key.L => KeyCode.L,
            Silk.NET.Input.Key.M => KeyCode.M,
            Silk.NET.Input.Key.N => KeyCode.N,
            Silk.NET.Input.Key.O => KeyCode.O,
            Silk.NET.Input.Key.P => KeyCode.P,
            Silk.NET.Input.Key.Q => KeyCode.Q,
            Silk.NET.Input.Key.R => KeyCode.R,
            Silk.NET.Input.Key.S => KeyCode.S,
            Silk.NET.Input.Key.T => KeyCode.T,
            Silk.NET.Input.Key.U => KeyCode.U,
            Silk.NET.Input.Key.V => KeyCode.V,
            Silk.NET.Input.Key.W => KeyCode.W,
            Silk.NET.Input.Key.X => KeyCode.X,
            Silk.NET.Input.Key.Y => KeyCode.Y,
            Silk.NET.Input.Key.Z => KeyCode.Z,
            Silk.NET.Input.Key.LeftBracket => KeyCode.BracketLeft,
            Silk.NET.Input.Key.BackSlash => KeyCode.BackSlash,
            Silk.NET.Input.Key.RightBracket => KeyCode.BracketRight,
            Silk.NET.Input.Key.GraveAccent => KeyCode.Tilde,
            Silk.NET.Input.Key.Escape => KeyCode.Escape,
            Silk.NET.Input.Key.Enter => KeyCode.Enter,
            Silk.NET.Input.Key.Tab => KeyCode.Tab,
            Silk.NET.Input.Key.Backspace => KeyCode.BackSpace,
            Silk.NET.Input.Key.Insert => KeyCode.Insert,
            Silk.NET.Input.Key.Delete => KeyCode.Delete,
            Silk.NET.Input.Key.Right => KeyCode.Right,
            Silk.NET.Input.Key.Left => KeyCode.Left,
            Silk.NET.Input.Key.Down => KeyCode.Down,
            Silk.NET.Input.Key.Up => KeyCode.Up,
            Silk.NET.Input.Key.PageUp => KeyCode.PageUp,
            Silk.NET.Input.Key.PageDown => KeyCode.PageDown,
            Silk.NET.Input.Key.Home => KeyCode.Home,
            Silk.NET.Input.Key.End => KeyCode.End,
            Silk.NET.Input.Key.CapsLock => KeyCode.CapsLock,
            Silk.NET.Input.Key.ScrollLock => KeyCode.ScrollLock,
            Silk.NET.Input.Key.NumLock => KeyCode.NumLock,
            Silk.NET.Input.Key.PrintScreen => KeyCode.PrintScreen,
            Silk.NET.Input.Key.Pause => KeyCode.Pause,
            Silk.NET.Input.Key.F1 => KeyCode.F1,
            Silk.NET.Input.Key.F2 => KeyCode.F2,
            Silk.NET.Input.Key.F3 => KeyCode.F3,
            Silk.NET.Input.Key.F4 => KeyCode.F4,
            Silk.NET.Input.Key.F5 => KeyCode.F5,
            Silk.NET.Input.Key.F6 => KeyCode.F6,
            Silk.NET.Input.Key.F7 => KeyCode.F7,
            Silk.NET.Input.Key.F8 => KeyCode.F8,
            Silk.NET.Input.Key.F9 => KeyCode.F9,
            Silk.NET.Input.Key.F10 => KeyCode.F10,
            Silk.NET.Input.Key.F11 => KeyCode.F11,
            Silk.NET.Input.Key.F12 => KeyCode.F12,
            Silk.NET.Input.Key.F13 => KeyCode.F13,
            Silk.NET.Input.Key.F14 => KeyCode.F14,
            Silk.NET.Input.Key.F15 => KeyCode.F15,
            Silk.NET.Input.Key.F16 => KeyCode.F16,
            Silk.NET.Input.Key.F17 => KeyCode.F17,
            Silk.NET.Input.Key.F18 => KeyCode.F18,
            Silk.NET.Input.Key.F19 => KeyCode.F19,
            Silk.NET.Input.Key.F20 => KeyCode.F20,
            Silk.NET.Input.Key.F21 => KeyCode.F21,
            Silk.NET.Input.Key.F22 => KeyCode.F22,
            Silk.NET.Input.Key.F23 => KeyCode.F23,
            Silk.NET.Input.Key.F24 => KeyCode.F24,
            Silk.NET.Input.Key.F25 => KeyCode.F25,
            Silk.NET.Input.Key.Keypad0 => KeyCode.Keypad0,
            Silk.NET.Input.Key.Keypad1 => KeyCode.Keypad1,
            Silk.NET.Input.Key.Keypad2 => KeyCode.Keypad2,
            Silk.NET.Input.Key.Keypad3 => KeyCode.Keypad3,
            Silk.NET.Input.Key.Keypad4 => KeyCode.Keypad4,
            Silk.NET.Input.Key.Keypad5 => KeyCode.Keypad5,
            Silk.NET.Input.Key.Keypad6 => KeyCode.Keypad6,
            Silk.NET.Input.Key.Keypad7 => KeyCode.Keypad7,
            Silk.NET.Input.Key.Keypad8 => KeyCode.Keypad8,
            Silk.NET.Input.Key.Keypad9 => KeyCode.Keypad9,
            Silk.NET.Input.Key.KeypadDecimal => KeyCode.KeypadPeriod,
            Silk.NET.Input.Key.KeypadDivide => KeyCode.KeypadDivide,
            Silk.NET.Input.Key.KeypadMultiply => KeyCode.KeypadMultiply,
            Silk.NET.Input.Key.KeypadSubtract => KeyCode.KeypadMinus,
            Silk.NET.Input.Key.KeypadAdd => KeyCode.KeypadPlus,
            Silk.NET.Input.Key.KeypadEnter => KeyCode.KeypadEnter,
            Silk.NET.Input.Key.ShiftLeft => KeyCode.ShiftLeft,
            Silk.NET.Input.Key.ControlLeft => KeyCode.ControlLeft,
            Silk.NET.Input.Key.AltLeft => KeyCode.AltLeft,
            Silk.NET.Input.Key.SuperLeft => KeyCode.WinLeft,
            Silk.NET.Input.Key.ShiftRight => KeyCode.ShiftRight,
            Silk.NET.Input.Key.ControlRight => KeyCode.ControlRight,
            Silk.NET.Input.Key.AltRight => KeyCode.AltRight,
            Silk.NET.Input.Key.SuperRight => KeyCode.WinRight,
            Silk.NET.Input.Key.Menu => KeyCode.Menu,
            _ => KeyCode.Unknown
        };
    }
}
