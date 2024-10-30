using System;
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
		/// ゲームウィンドウの位置を取得または設定します。
		/// </summary>
		VectorInt Location { get; set; }

		/// <summary>
		/// ゲームウィンドウのサイズを取得または設定します。
		/// </summary>
		VectorInt Size { get; set; }

		/// <summary>
		/// ゲームウィンドウのデバイス単位のサイズを取得します。
		/// </summary>
		VectorInt ActualSize { get; }

		/// <summary>
		/// ゲーム ウィンドウの拡大率を取得または設定します。
		/// 1, 2, 4, 8 のいずれかの値を指定します。
		/// 設定すると、<see cref="Size"/> 等のプロパティやレンダラーが用いる座標は変わりませんが、ウィンドウのみが拡大されます。
		/// これにより、ドット ベースのゲームを等倍拡大し、高解像度ディスプレイ等でプレイしやすくなります。
		/// </summary>
		int Scale { get; set; }

		/// <summary>
		/// ゲームウィンドウのX座標位置を取得または設定します。
		/// </summary>
		int X { get; set; }

		/// <summary>
		/// ゲームウィンドウのY座標位置を取得または設定します。
		/// </summary>
		int Y { get; set; }

		/// <summary>
		/// ゲームウィンドウの幅を取得または設定します。
		/// </summary>
		int Width { get; set; }

		/// <summary>
		/// ゲームウィンドウの高さを取得または設定します。
		/// </summary>
		int Height { get; set; }

		/// <summary>
		/// ゲームウィンドウのデバイス単位の幅を取得します。
		/// </summary>
		int ActualWidth { get; }

		/// <summary>
		/// ゲームウィンドウのデバイス単位の高さを取得します。
		/// </summary>
		int ActualHeight { get; }

		/// <summary>
		/// ゲームウィンドウが表示されているかどうかを取得または設定します。
		/// </summary>
		bool IsVisible { get; set; }

		/// <summary>
		/// ゲームウィンドウがフォーカスされているかどうかを取得します。
		/// </summary>
		bool IsFocused { get; }

		/// <summary>
		/// ゲームウィンドウが全画面表示かどうかを取得または設定します。
		/// </summary>
		bool IsFullScreen { get; set; }

		/// <summary>
		/// ゲーム起動時からの経過時間を取得または設定します。
		/// </summary>
		float TotalTime { get; }

		/// <summary>
		/// 前回の更新フレームからの経過時間を取得します。
		/// </summary>
		float DeltaTime { get; }

		/// <summary>
		/// レンダリングFPSを取得します。
		/// </summary>
		long FramePerSeconds { get; }

		/// <summary>
		/// 更新FPSを取得します。
		/// </summary>
		long UpdatePerSeconds { get; }

		/// <summary>
		/// ゲームウィンドウが開始してからの総フレーム数を取得または設定します。
		/// </summary>
		long TotalFrame { get; }

		[Obsolete("Use TargetFps instead.")]
		int RefreshRate { get; set; }

		/// <summary>
		/// ゲームウィンドウがVsyncモードかどうかを取得または設定します。
		/// </summary>
		bool IsVsyncMode { get; set; }

		/// <summary>
		/// FPS目標を取得または設定します。
		/// </summary>
		int TargetFps { get; set; }

		/// <summary>
		/// UPS目標を取得または設定します。
		/// </summary>
		int TargetUps { get; set; }

		/// <summary>
		/// ゲームウィンドウのピクセル比率を取得します。
		/// </summary>
		float PixelRatio { get; }

		/// <summary>
		/// ゲームウィンドウのタイトルを取得または設定します。
		/// </summary>
		string Title { get; set; }

		/// <summary>
		/// ゲームウィンドウのモードを取得または設定します。
		/// </summary>
		WindowMode Mode { get; set; }

		/// <summary>
		/// INTERNAL API (使用しないでください)
		/// </summary>
		IInputContext? _RawInputContext { get; }

		/// <summary>
		/// INTERNAL API (使用しないでください)
		/// </summary>
		TextureFactory TextureFactory { get; }

		/// <summary>
		/// このウィンドウを開き、ゲームを開始します。
		/// </summary>
		void Run(WindowOptions opts);

		/// <summary>
		/// 指定されたステータスコードでゲームを終了します。
		/// </summary>
		void Exit();

		/// <summary>
		/// スクリーンショットを撮り、それをテクスチャとして生成します。
		/// </summary>
		/// <returns>スクリーンショットのテクスチャ</returns>
		Texture2D TakeScreenshot();

		/// <summary>
		/// 指定されたパスにスクリーンショットをPNG形式で保存します。
		/// </summary>
		/// <param name="path">パス</param>
		/// <param name="ct">このタスクのキャンセレーショントークン</param>
		Task SaveScreenshotAsync(string path, CancellationToken ct = default);

		/// <summary>
		/// ゲームが開始されたときに発生します。
		/// </summary>
		event Action? Start;

		/// <summary>
		/// ゲームがフレームを更新するときに発生します。
		/// </summary>
		event Action? Update;

		/// <summary>
		/// ゲームがフレームをレンダリングするときに発生します。
		/// </summary>
		event Action? Render;

		/// <summary>
		/// ゲームが終了したときに発生します。
		/// </summary>
		event Action? Destroy;

		/// <summary>
		/// ゲームがフレームを更新する前に発生します。
		/// </summary>
		event Action? PreUpdate;

		/// <summary>
		/// ゲームがフレームを更新した後に発生します。
		/// </summary>
		event Action? PostUpdate;

		/// <summary>
		/// ユーザーがウィンドウにファイルをドロップしたときに発生します。
		/// </summary>
		event Action<FileDroppedEventArgs>? FileDropped;

		/// <summary>
		/// ゲームウィンドウがリサイズされたときに発生します。
		/// </summary>
		event Action? Resize;
	}
}
