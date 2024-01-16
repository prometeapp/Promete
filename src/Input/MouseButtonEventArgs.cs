namespace Promete.Input;

public class MouseButtonEventArgs(int buttonId, VectorInt position) : MouseEventArgs(position)
{
	/// <summary>
	/// Get the button ID related to the event.
	/// </summary>
	public int ButtonId { get; } = buttonId;
}
