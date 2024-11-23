namespace Promete.Input;

/// <summary>
/// マウスのボタン入力を表します。
/// </summary>
public class MouseButton
{
	/// <summary>
	/// このボタンが押されているかどうかを取得します。
	/// </summary>
	public bool IsPressed { get; internal set; }

	/// <summary>
	/// このボタンが押されてからの経過フレーム数を取得します。
	/// </summary>
	/// <value></value>
	public int ElapsedFrameCount { get; internal set; }

	/// <summary>
	/// このボタンが押されてからの経過時間を取得します。
	/// </summary>
	/// <value></value>
	public float ElapsedTime { get; internal set; }

	/// <summary>
	/// このボタンがこのフレームで押されたかどうかを取得します。
	/// </summary>
	public bool IsButtonDown { get; internal set; }

	/// <summary>
	/// このボタンがこのフレームで離されたかどうかを取得します。
	/// </summary>
	public bool IsButtonUp { get; internal set; }

	internal MouseButton() { }

	public static implicit operator bool(MouseButton button) => button.IsPressed;
}
