namespace Promete.Input;

/// <summary>
/// Keyboard pressed event argument.
/// </summary>
public struct KeyPressEventArgs
{
    internal KeyPressEventArgs(char ch)
    {
        KeyChar = ch;
    }

    public char KeyChar { get; }
}
