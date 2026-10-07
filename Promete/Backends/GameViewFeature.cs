namespace Promete.Backends;

/// <summary>
/// <see cref="IGameView"/> が対応しているかを問い合わせられる機能の種類です。
/// </summary>
/// <remarks>
/// 対応していない機能のプロパティを設定しても、例外は発生せず、画面には反映されません。
/// 戻り値を持つ操作 (<see cref="IGameView.TakeScreenshot"/> など) は、<see cref="System.NotSupportedException"/> をスローすることがあります。
/// </remarks>
/// <seealso cref="IGameView.IsSupported(GameViewFeature)"/>
public enum GameViewFeature
{
    /// <summary>
    /// ウィンドウの位置 (<see cref="IGameView.Location"/>、<see cref="IGameView.X"/>、<see cref="IGameView.Y"/>) の取得と設定。
    /// </summary>
    Location,

    /// <summary>
    /// 常に最前面に表示する設定 (<see cref="IGameView.TopMost"/>)。
    /// </summary>
    TopMost,

    /// <summary>
    /// 全画面表示 (<see cref="IGameView.IsFullScreen"/>)。
    /// </summary>
    FullScreen,

    /// <summary>
    /// ウィンドウのモード (<see cref="IGameView.Mode"/>)。
    /// </summary>
    WindowMode,

    /// <summary>
    /// タイトル (<see cref="IGameView.Title"/>) の表示。
    /// </summary>
    Title,

    /// <summary>
    /// 表示状態 (<see cref="IGameView.IsVisible"/>) の設定。
    /// </summary>
    Visibility,

    /// <summary>
    /// フォーカス状態 (<see cref="IGameView.IsFocused"/>) の取得。対応していない場合、<see cref="IGameView.IsFocused"/> は常に <see langword="true"/> を返します。
    /// </summary>
    Focus,

    /// <summary>
    /// スクリーンショット (<see cref="IGameView.TakeScreenshot"/>、<see cref="IGameView.SaveScreenshotAsync"/>)。
    /// </summary>
    Screenshot,

    /// <summary>
    /// ファイルのドロップ (<see cref="IGameView.FileDropped"/>)。
    /// </summary>
    FileDrop,
}
