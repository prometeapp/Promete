using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Silk.NET.Input;

namespace Promete.Windowing
{
	/// <summary>
	/// ゲーム実行用のウィンドウを表します。
	/// </summary>
	public interface IWindow
	{
		/// <summary>
		/// Get or set location of this game window.
		/// </summary>
		Vector2 Location { get; set; }

		/// <summary>
		/// Get or set size of this game window.
		/// </summary>
		Vector2 Size { get; set; }

		/// <summary>
		/// Get or set device-unit size of this game window.
		/// </summary>
		Vector2 ActualSize { get; }

		/// <summary>
		/// ゲーム ウィンドウの拡大率を取得または設定します。
		/// 1, 2, 4, 8 のいずれかの値を指定します。
		/// 設定すると、<see cref="Size"/> 等のプロパティやレンダラーが用いる座標は変わりませんが、ウィンドウのみが拡大されます。
		/// これにより、ドット ベースのゲームを等倍拡大し、高解像度ディスプレイ等でプレイしやすくなります。
		/// </summary>
		int Scale { get; set; }

		/// <summary>
		/// Get or set X-coord location of this game window.
		/// </summary>
		int X { get; set; }

		/// <summary>
		/// Get or set Y-coord location of this game window.
		/// </summary>
		int Y { get; set; }

		/// <summary>
		/// Get or set width of this game window.
		/// </summary>
		int Width { get; set; }

		/// <summary>
		/// Get or set height of this game window.
		/// </summary>
		int Height { get; set; }

		/// <summary>
		/// Get or set device-unit width of this game window.
		/// </summary>
		int ActualWidth { get; }

		/// <summary>
		/// Get or set device-unit height of this game window.
		/// </summary>
		int ActualHeight { get; }

		/// <summary>
		/// Get or set whether this game window is visible.
		/// </summary>
		bool IsVisible { get; set; }

		/// <summary>
		/// Get or set whether this game window is focused.
		/// </summary>
		bool IsFocused { get; }

		/// <summary>
		/// Get or set whether this game window is fullscreen.
		/// </summary>
		bool IsFullScreen { get; set; }

		float TotalTime { get; }

		float DeltaTime { get; }

		long FramePerSeconds { get; }

		long UpdatePerSeconds { get; }

		/// <summary>
		/// Get or set total frame count after this game window starts.
		/// </summary>
		long TotalFrame { get; }

		/// <summary>
		/// Get or set refresh rate of this game window.
		/// </summary>
		int RefreshRate { get; }

		/// <summary>
		/// Get or set pixel ratio of this game window.
		/// </summary>
		float PixelRatio { get; }

		/// <summary>
		/// Get or set a title of this game window.
		/// </summary>
		string Title { get; set; }

		/// <summary>
		/// Get or set this game window mode.
		/// </summary>
		WindowMode Mode { get; set; }

		/// <summary>
		/// INTERNAL API (Not use this)
		/// </summary>
		IInputContext? _RawInputContext { get; }

		TextureFactory TextureFactory { get; }

		/// <summary>
		/// Open this window and start game.
		/// </summary>
		void Run();

		/// <summary>
		/// Exit this game by the specified status code.
		/// </summary>
		void Exit();

		/// <summary>
		/// Take a screenshot and generate a texture from it.
		/// </summary>
		/// <returns>A screenshot as a texture</returns>
		ITexture TakeScreenshot();

		/// <summary>
		/// Save a screenshot as PNG to the specified path.
		/// </summary>
		/// <param name="path">Path</param>
		/// <param name="ct">Cancellation Token of this task.</param>
		Task SaveScreenshotAsync(string path, CancellationToken ct = default);

		/// <summary>
		/// Occured when this game starts.
		/// </summary>
		event Action? Start;

		/// <summary>
		/// Occured when this game updates the frame.
		/// </summary>
		event Action? Update;

		/// <summary>
		/// Occured when this game renders the frame.
		/// </summary>
		event Action? Render;

		/// <summary>
		/// Occured when this game closed.
		/// </summary>
		event Action? Destroy;

		/// <summary>
		/// Occured before this game updates the frame.
		/// </summary>
		event Action? PreUpdate;

		/// <summary>
		/// Occured after this game updates the frame.
		/// </summary>
		event Action? PostUpdate;

		/// <summary>
		/// Occured when the user drops files into the window.
		/// </summary>
		event Action<FileDroppedEventArgs>? FileDropped;

		/// <summary>
		/// Occured when this game window resized.
		/// </summary>
		event Action? Resize;
	}
}
