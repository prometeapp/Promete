namespace Promete.Input;

/// <summary>
/// Keyboard event argument.
/// </summary>
public struct KeyEventArgs
{
	/// <summary>
	/// Get a pressed key.
	/// </summary>
	public KeyCode Key { get; }

	internal KeyEventArgs(KeyCode key)
	{
		Key = key;
	}
}
