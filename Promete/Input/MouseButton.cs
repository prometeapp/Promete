namespace Promete.Input;

public class MouseButton
{
	internal MouseButton() { }

	/// <summary>
	/// Gets a value that indicates whether the button is pressed.
	/// </summary>
	public bool IsPressed { get; internal set; }

	/// <summary>
	/// Gets the frame count elapsed since the button was pressed.
	/// </summary>
	/// <value></value>
	public int ElapsedFrameCount { get; internal set; }

	/// <summary>
	/// Gets the time elapsed since the button was pressed.
	/// </summary>
	/// <value></value>
	public float ElapsedTime { get; internal set; }

	/// <summary>
	/// Gets whether the button was pressed at this frame.
	/// </summary>
	public bool IsButtonDown { get; internal set; }

	/// <summary>
	/// Gets whether the button was released at this frame.
	/// </summary>
	public bool IsButtonUp { get; internal set; }

	public static implicit operator bool(MouseButton button) => button.IsPressed;
}
