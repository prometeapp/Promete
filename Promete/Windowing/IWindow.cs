using System;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Silk.NET.Input;

namespace Promete.Windowing;

/// <summary>
/// ゲーム実行用のウィンドウを表します。
/// </summary>
public interface IWindow
{
    /// <summary>
    /// ゲームウィンドウの位置を取得または設定します。
    /// </summary>
    public VectorInt Location { get; set; }

    /// <summary>
    /// ゲームウィンドウのサイズを取得または設定します。
    /// </summary>
    public VectorInt Size { get; set; }

    /// <summary>
    /// ゲームウィンドウのデバイス単位のサイズを取得します。
    /// </summary>
    public VectorInt ActualSize { get; }

    /// <summary>
    /// ゲーム ウィンドウの拡大率を取得または設定します。
    /// 1, 2, 4, 8 のいずれかの値を指定します。
    /// 設定すると、<see cref="Size" /> 等のプロパティやレンダラーが用いる座標は変わりませんが、ウィンドウのみが拡大されます。
    /// これにより、ドット ベースのゲームを等倍拡大し、高解像度ディスプレイ等でプレイしやすくなります。
    /// </summary>
    public int Scale { get; set; }

    /// <summary>
    /// ゲームウィンドウのX座標位置を取得または設定します。
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// ゲームウィンドウのY座標位置を取得または設定します。
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// ゲームウィンドウの幅を取得または設定します。
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// ゲームウィンドウの高さを取得または設定します。
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// ゲームウィンドウのデバイス単位の幅を取得します。
    /// </summary>
    public int ActualWidth { get; }

    /// <summary>
    /// ゲームウィンドウのデバイス単位の高さを取得します。
    /// </summary>
    public int ActualHeight { get; }

    /// <summary>
    /// ゲームウィンドウが表示されているかどうかを取得または設定します。
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// ゲームウィンドウがフォーカスされているかどうかを取得します。
    /// </summary>
    public bool IsFocused { get; }

    /// <summary>
    /// ゲームウィンドウが全画面表示かどうかを取得または設定します。
    /// </summary>
    public bool IsFullScreen { get; set; }

    /// <summary>
    /// ゲームウィンドウが常に最前面に表示されるかどうかを取得または設定します。
    /// </summary>
    public bool TopMost { get; set; }

    /// <summary>
    /// ゲーム起動時からの経過時間を取得または設定します。
    /// </summary>
    public float TotalTime { get; }

    /// <summary>
    /// 前回の更新フレームからの経過時間を取得します。
    /// </summary>
    public float DeltaTime { get; }

    /// <summary>
    /// レンダリングFPSを取得します。
    /// </summary>
    public long FramePerSeconds { get; }

    /// <summary>
    /// 更新FPSを取得します。
    /// </summary>
    public long UpdatePerSeconds { get; }

    /// <summary>
    /// ゲームウィンドウが開始してからの総フレーム数を取得または設定します。
    /// </summary>
    public long TotalFrame { get; }

    [Obsolete("Use TargetFps instead.")] public int RefreshRate { get; set; }

    /// <summary>
    /// ゲームウィンドウがVsyncモードかどうかを取得または設定します。
    /// </summary>
    public bool IsVsyncMode { get; set; }

    /// <summary>
    /// FPS目標を取得または設定します。
    /// </summary>
    public int TargetFps { get; set; }

    /// <summary>
    /// UPS目標を取得または設定します。
    /// </summary>
    public int TargetUps { get; set; }

    /// <summary>
    /// 時間が流れる速度（通常の速度を<c>1.0f</c>とした倍率）を取得または設定します。
    /// </summary>
    public float TimeScale { get; set; }

    public float TotalTimeWithoutScale { get; }

    /// <summary>
    /// ゲームウィンドウのピクセル比率を取得します。
    /// </summary>
    public float PixelRatio { get; }

    /// <summary>
    /// ゲームウィンドウのタイトルを取得または設定します。
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// ゲームウィンドウのモードを取得または設定します。
    /// </summary>
    public WindowMode Mode { get; set; }

    /// <summary>
    /// INTERNAL API (使用しないでください)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public IInputContext? _RawInputContext { get; }

    /// <summary>
    /// INTERNAL API (使用しないでください)
    /// </summary>
    public TextureFactory TextureFactory { get; }

    /// <summary>
    /// このウィンドウを開き、ゲームを開始します。
    /// </summary>
    public void Run(WindowOptions opts);

    /// <summary>
    /// 指定されたステータスコードでゲームを終了します。
    /// </summary>
    public void Exit();

    /// <summary>
    /// スクリーンショットを撮り、それをテクスチャとして生成します。
    /// </summary>
    /// <returns>スクリーンショットのテクスチャ</returns>
    public Texture2D TakeScreenshot();

    /// <summary>
    /// 指定されたパスにスクリーンショットをPNG形式で保存します。
    /// </summary>
    /// <param name="path">パス</param>
    /// <param name="ct">このタスクのキャンセレーショントークン</param>
    public Task SaveScreenshotAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// ゲームが開始されたときに発生します。
    /// </summary>
    public event Action? Start;

    /// <summary>
    /// ゲームがフレームを更新するときに発生します。
    /// </summary>
    public event Action? Update;

    /// <summary>
    /// ゲームがフレームをレンダリングするときに発生します。
    /// </summary>
    public event Action? Render;

    /// <summary>
    /// ゲームが終了したときに発生します。
    /// </summary>
    public event Action? Destroy;

    /// <summary>
    /// ゲームがフレームを更新する前に発生します。
    /// </summary>
    public event Action? PreUpdate;

    /// <summary>
    /// ゲームがフレームを更新した後に発生します。
    /// </summary>
    public event Action? PostUpdate;

    /// <summary>
    /// ユーザーがウィンドウにファイルをドロップしたときに発生します。
    /// </summary>
    public event Action<FileDroppedEventArgs>? FileDropped;

    /// <summary>
    /// ゲームウィンドウがリサイズされたときに発生します。
    /// </summary>
    public event Action? Resize;
}
