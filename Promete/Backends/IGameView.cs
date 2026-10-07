using System;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Promete.Windowing;

namespace Promete.Backends;

/// <summary>
/// ゲーム画面（ウィンドウ等）の情報にアクセスします。
/// </summary>
public interface IGameView
{
    /// <summary>
    /// ユーザーがウィンドウにファイルをドロップしたときに発生します。
    /// </summary>
    public event Action<FileDroppedEventArgs>? FileDropped;

    /// <summary>
    /// ゲームウィンドウがリサイズされたときに発生します。
    /// </summary>
    public event Action? Resize;

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
    /// スクリーンショットを撮り、それをテクスチャとして生成します。
    /// </summary>
    /// <returns>スクリーンショットのテクスチャ</returns>
    /// <exception cref="NotSupportedException">実行環境がスクリーンショットに対応していない場合 (<see cref="GameViewFeature.Screenshot"/>)。</exception>
    public Texture2D TakeScreenshot();

    /// <summary>
    /// 指定されたパスにスクリーンショットをPNG形式で保存します。
    /// </summary>
    /// <param name="path">パス</param>
    /// <param name="ct">このタスクのキャンセレーショントークン</param>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    /// <exception cref="NotSupportedException">実行環境がスクリーンショットに対応していない場合 (<see cref="GameViewFeature.Screenshot"/>)。</exception>
    public Task SaveScreenshotAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// 指定した機能に、このゲーム画面が対応しているかどうかを取得します。
    /// </summary>
    /// <remarks>
    /// 実行環境によっては、一部の機能に対応していません (例: ブラウザではウィンドウの位置を変更できない)。
    /// 対応していない機能のプロパティを設定しても、例外は発生せず、画面には反映されません。
    /// 既定の実装は、すべての機能に対応していると答えます。
    /// </remarks>
    /// <param name="feature">問い合わせる機能。</param>
    /// <returns>対応していれば <see langword="true"/>。</returns>
    public bool IsSupported(GameViewFeature feature) => true;
}
