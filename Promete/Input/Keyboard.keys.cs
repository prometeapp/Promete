﻿using System;

namespace Promete.Input;

public partial class Keyboard
{
    public Key Unknown { get; } = new();
    public Key ShiftLeft { get; } = new();
    public Key ShiftRight { get; } = new();
    public Key ControlLeft { get; } = new();
    public Key ControlRight { get; } = new();
    public Key AltLeft { get; } = new();
    public Key AltRight { get; } = new();
    public Key WinLeft { get; } = new();
    public Key WinRight { get; } = new();
    public Key Menu { get; } = new();
    public Key F1 { get; } = new();
    public Key F2 { get; } = new();
    public Key F3 { get; } = new();
    public Key F4 { get; } = new();
    public Key F5 { get; } = new();
    public Key F6 { get; } = new();
    public Key F7 { get; } = new();
    public Key F8 { get; } = new();
    public Key F9 { get; } = new();
    public Key F10 { get; } = new();
    public Key F11 { get; } = new();
    public Key F12 { get; } = new();
    public Key F13 { get; } = new();
    public Key F14 { get; } = new();
    public Key F15 { get; } = new();
    public Key F16 { get; } = new();
    public Key F17 { get; } = new();
    public Key F18 { get; } = new();
    public Key F19 { get; } = new();
    public Key F20 { get; } = new();
    public Key F21 { get; } = new();
    public Key F22 { get; } = new();
    public Key F23 { get; } = new();
    public Key F24 { get; } = new();
    public Key F25 { get; } = new();
    public Key F26 { get; } = new();
    public Key F27 { get; } = new();
    public Key F28 { get; } = new();
    public Key F29 { get; } = new();
    public Key F30 { get; } = new();
    public Key F31 { get; } = new();
    public Key F32 { get; } = new();
    public Key F33 { get; } = new();
    public Key F34 { get; } = new();
    public Key F35 { get; } = new();
    public Key Up { get; } = new();
    public Key Down { get; } = new();
    public Key Left { get; } = new();
    public Key Right { get; } = new();
    public Key Enter { get; } = new();
    public Key Escape { get; } = new();
    public Key Space { get; } = new();
    public Key Tab { get; } = new();
    public Key BackSpace { get; } = new();
    public Key Insert { get; } = new();
    public Key Delete { get; } = new();
    public Key PageUp { get; } = new();
    public Key PageDown { get; } = new();
    public Key Home { get; } = new();
    public Key End { get; } = new();
    public Key CapsLock { get; } = new();
    public Key ScrollLock { get; } = new();
    public Key PrintScreen { get; } = new();
    public Key Pause { get; } = new();
    public Key NumLock { get; } = new();
    public Key Clear { get; } = new();
    public Key Sleep { get; } = new();
    public Key Keypad0 { get; } = new();
    public Key Keypad1 { get; } = new();
    public Key Keypad2 { get; } = new();
    public Key Keypad3 { get; } = new();
    public Key Keypad4 { get; } = new();
    public Key Keypad5 { get; } = new();
    public Key Keypad6 { get; } = new();
    public Key Keypad7 { get; } = new();
    public Key Keypad8 { get; } = new();
    public Key Keypad9 { get; } = new();
    public Key KeypadDivide { get; } = new();
    public Key KeypadMultiply { get; } = new();
    public Key KeypadMinus { get; } = new();
    public Key KeypadPlus { get; } = new();
    public Key KeypadPeriod { get; } = new();
    public Key KeypadEnter { get; } = new();
    public Key A { get; } = new();
    public Key B { get; } = new();
    public Key C { get; } = new();
    public Key D { get; } = new();
    public Key E { get; } = new();
    public Key F { get; } = new();
    public Key G { get; } = new();
    public Key H { get; } = new();
    public Key I { get; } = new();
    public Key J { get; } = new();
    public Key K { get; } = new();
    public Key L { get; } = new();
    public Key M { get; } = new();
    public Key N { get; } = new();
    public Key O { get; } = new();
    public Key P { get; } = new();
    public Key Q { get; } = new();
    public Key R { get; } = new();
    public Key S { get; } = new();
    public Key T { get; } = new();
    public Key U { get; } = new();
    public Key V { get; } = new();
    public Key W { get; } = new();
    public Key X { get; } = new();
    public Key Y { get; } = new();
    public Key Z { get; } = new();
    public Key Number0 { get; } = new();
    public Key Number1 { get; } = new();
    public Key Number2 { get; } = new();
    public Key Number3 { get; } = new();
    public Key Number4 { get; } = new();
    public Key Number5 { get; } = new();
    public Key Number6 { get; } = new();
    public Key Number7 { get; } = new();
    public Key Number8 { get; } = new();
    public Key Number9 { get; } = new();
    public Key Tilde { get; } = new();
    public Key Minus { get; } = new();
    public Key Plus { get; } = new();
    public Key BracketLeft { get; } = new();
    public Key BracketRight { get; } = new();
    public Key Semicolon { get; } = new();
    public Key Quote { get; } = new();
    public Key Comma { get; } = new();
    public Key Period { get; } = new();
    public Key Slash { get; } = new();
    public Key BackSlash { get; } = new();
    public Key NonUSBackSlash { get; } = new();
    public Key LastKey { get; } = new();

    /// <summary>
    /// Get a specific key by <see cref="KeyCode" />.
    /// </summary>
    public Key KeyOf(KeyCode code)
    {
        return code switch
        {
            KeyCode.Unknown => Unknown,
            KeyCode.ShiftLeft => ShiftLeft,
            KeyCode.ShiftRight => ShiftRight,
            KeyCode.ControlLeft => ControlLeft,
            KeyCode.ControlRight => ControlRight,
            KeyCode.AltLeft => AltLeft,
            KeyCode.AltRight => AltRight,
            KeyCode.WinLeft => WinLeft,
            KeyCode.WinRight => WinRight,
            KeyCode.Menu => Menu,
            KeyCode.F1 => F1,
            KeyCode.F2 => F2,
            KeyCode.F3 => F3,
            KeyCode.F4 => F4,
            KeyCode.F5 => F5,
            KeyCode.F6 => F6,
            KeyCode.F7 => F7,
            KeyCode.F8 => F8,
            KeyCode.F9 => F9,
            KeyCode.F10 => F10,
            KeyCode.F11 => F11,
            KeyCode.F12 => F12,
            KeyCode.F13 => F13,
            KeyCode.F14 => F14,
            KeyCode.F15 => F15,
            KeyCode.F16 => F16,
            KeyCode.F17 => F17,
            KeyCode.F18 => F18,
            KeyCode.F19 => F19,
            KeyCode.F20 => F20,
            KeyCode.F21 => F21,
            KeyCode.F22 => F22,
            KeyCode.F23 => F23,
            KeyCode.F24 => F24,
            KeyCode.F25 => F25,
            KeyCode.F26 => F26,
            KeyCode.F27 => F27,
            KeyCode.F28 => F28,
            KeyCode.F29 => F29,
            KeyCode.F30 => F30,
            KeyCode.F31 => F31,
            KeyCode.F32 => F32,
            KeyCode.F33 => F33,
            KeyCode.F34 => F34,
            KeyCode.F35 => F35,
            KeyCode.Up => Up,
            KeyCode.Down => Down,
            KeyCode.Left => Left,
            KeyCode.Right => Right,
            KeyCode.Enter => Enter,
            KeyCode.Escape => Escape,
            KeyCode.Space => Space,
            KeyCode.Tab => Tab,
            KeyCode.BackSpace => BackSpace,
            KeyCode.Insert => Insert,
            KeyCode.Delete => Delete,
            KeyCode.PageUp => PageUp,
            KeyCode.PageDown => PageDown,
            KeyCode.Home => Home,
            KeyCode.End => End,
            KeyCode.CapsLock => CapsLock,
            KeyCode.ScrollLock => ScrollLock,
            KeyCode.PrintScreen => PrintScreen,
            KeyCode.Pause => Pause,
            KeyCode.NumLock => NumLock,
            KeyCode.Clear => Clear,
            KeyCode.Sleep => Sleep,
            KeyCode.Keypad0 => Keypad0,
            KeyCode.Keypad1 => Keypad1,
            KeyCode.Keypad2 => Keypad2,
            KeyCode.Keypad3 => Keypad3,
            KeyCode.Keypad4 => Keypad4,
            KeyCode.Keypad5 => Keypad5,
            KeyCode.Keypad6 => Keypad6,
            KeyCode.Keypad7 => Keypad7,
            KeyCode.Keypad8 => Keypad8,
            KeyCode.Keypad9 => Keypad9,
            KeyCode.KeypadDivide => KeypadDivide,
            KeyCode.KeypadMultiply => KeypadMultiply,
            KeyCode.KeypadPlus => KeypadPlus,
            KeyCode.KeypadMinus => KeypadMinus,
            KeyCode.KeypadPeriod => KeypadPeriod,
            KeyCode.KeypadEnter => KeypadEnter,
            KeyCode.A => A,
            KeyCode.B => B,
            KeyCode.C => C,
            KeyCode.D => D,
            KeyCode.E => E,
            KeyCode.F => F,
            KeyCode.G => G,
            KeyCode.H => H,
            KeyCode.I => I,
            KeyCode.J => J,
            KeyCode.K => K,
            KeyCode.L => L,
            KeyCode.M => M,
            KeyCode.N => N,
            KeyCode.O => O,
            KeyCode.P => P,
            KeyCode.Q => Q,
            KeyCode.R => R,
            KeyCode.S => S,
            KeyCode.T => T,
            KeyCode.U => U,
            KeyCode.V => V,
            KeyCode.W => W,
            KeyCode.X => X,
            KeyCode.Y => Y,
            KeyCode.Z => Z,
            KeyCode.Number0 => Number0,
            KeyCode.Number1 => Number1,
            KeyCode.Number2 => Number2,
            KeyCode.Number3 => Number3,
            KeyCode.Number4 => Number4,
            KeyCode.Number5 => Number5,
            KeyCode.Number6 => Number6,
            KeyCode.Number7 => Number7,
            KeyCode.Number8 => Number8,
            KeyCode.Number9 => Number9,
            KeyCode.Tilde => Tilde,
            KeyCode.Minus => Minus,
            KeyCode.Plus => Plus,
            KeyCode.BracketLeft => BracketLeft,
            KeyCode.BracketRight => BracketRight,
            KeyCode.Semicolon => Semicolon,
            KeyCode.Quote => Quote,
            KeyCode.Comma => Comma,
            KeyCode.Period => Period,
            KeyCode.Slash => Slash,
            KeyCode.BackSlash => BackSlash,
            KeyCode.NonUSBackSlash => NonUSBackSlash,
            KeyCode.LastKey => LastKey,
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };
    }
}
