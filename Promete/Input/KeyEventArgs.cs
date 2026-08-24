namespace Promete.Input;

/// <summary>
/// Keyboard event argument.
/// </summary>
public struct KeyEventArgs
{
    internal KeyEventArgs(KeyCode key)
    {
        Key = key;
    }

    /// <summary>
    /// Get a pressed key.
    /// </summary>
    public KeyCode Key { get; }
}
