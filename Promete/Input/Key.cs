namespace Promete.Input;

/// <summary>
/// キーボード上のキー入力を表します。
/// </summary>
public class Key
{
	/// <summary>
	/// キーが現在押されているかどうかを取得します。
	/// </summary>
	public bool IsPressed { get; internal set; }

	/// <summary>
	/// キーがこのフレームで押されたかどうかを取得します。
	/// </summary>
	public bool IsKeyDown { get; internal set; }

	/// <summary>
	/// キーがこのフレームで離されたかどうかを取得します。
	/// </summary>
	public bool IsKeyUp { get; internal set; }

	/// <summary>
	/// キーが押されてからの経過フレーム数を取得します。
	/// </summary>
	/// <value></value>
	public int ElapsedFrameCount { get; internal set; }

	/// <summary>
	/// キーが押されてからの経過時間を取得します。
	/// </summary>
	/// <value></value>
	public float ElapsedTime { get; internal set; }

	internal Key()
	{
	}

	public static implicit operator bool(Key key) => key.IsPressed;
}
