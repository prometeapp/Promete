namespace Promete.Input;

/// <summary>
/// Keyboard pressed event argument.
/// </summary>
public struct KeyPressEventArgs
{
    public char KeyChar { get; }

    internal KeyPressEventArgs(char ch)
    {
        KeyChar = ch;
    }
}
