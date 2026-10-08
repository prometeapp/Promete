using Silk.NET.Input;

namespace Promete.Web.Input;

/// <summary>
/// DOM の <c>KeyboardEvent.code</c> と <c>MouseEvent.button</c> を、Silk.NET の値に変換します。
/// </summary>
internal static class WebKeyMap
{
    private static readonly Dictionary<string, Key> Special = new()
    {
        ["ArrowLeft"] = Key.Left,
        ["ArrowRight"] = Key.Right,
        ["ArrowUp"] = Key.Up,
        ["ArrowDown"] = Key.Down,
        ["Quote"] = Key.Apostrophe,
        ["Backquote"] = Key.GraveAccent,
        ["Backslash"] = Key.BackSlash,
        ["BracketLeft"] = Key.LeftBracket,
        ["BracketRight"] = Key.RightBracket,
        ["MetaLeft"] = Key.SuperLeft,
        ["MetaRight"] = Key.SuperRight,
        ["NumpadEnter"] = Key.KeypadEnter,
        ["NumpadAdd"] = Key.KeypadAdd,
        ["NumpadSubtract"] = Key.KeypadSubtract,
        ["NumpadMultiply"] = Key.KeypadMultiply,
        ["NumpadDivide"] = Key.KeypadDivide,
        ["NumpadDecimal"] = Key.KeypadDecimal,
    };

    /// <summary><c>KeyboardEvent.code</c> を <see cref="Key"/> に変換します。</summary>
    /// <param name="code">DOM のキーコード (例: KeyA, Digit1, ArrowLeft)。</param>
    /// <returns>対応する <see cref="Key"/>。無ければ <see cref="Key.Unknown"/>。</returns>
    public static Key ToKey(string code)
    {
        if (Special.TryGetValue(code, out var special))
            return special;

        var name = code switch
        {
            _ when code.StartsWith("Key", StringComparison.Ordinal) => code[3..],
            _ when code.StartsWith("Digit", StringComparison.Ordinal) => "Number" + code[5..],
            _ when code.StartsWith("Numpad", StringComparison.Ordinal) => "Keypad" + code[6..],
            _ => code,
        };
        return Enum.TryParse<Key>(name, out var key) ? key : Key.Unknown;
    }

    /// <summary><c>MouseEvent.button</c> を <see cref="MouseButton"/> に変換します。</summary>
    /// <param name="button">DOM のボタン番号 (0: 左, 1: 中, 2: 右)。</param>
    /// <returns>対応する <see cref="MouseButton"/>。</returns>
    public static MouseButton ToMouseButton(int button) =>
        button switch
        {
            0 => MouseButton.Left,
            1 => MouseButton.Middle,
            2 => MouseButton.Right,
            3 => MouseButton.Button4,
            4 => MouseButton.Button5,
            _ => MouseButton.Unknown,
        };
}
