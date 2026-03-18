namespace Promete.Nodes.Renderer;

/// <summary>
/// フレーム毎のレンダリングコンテキスト情報を保持します。
/// </summary>
public class RenderContext
{
    /// <summary>
    /// ウィンドウのサイズを取得します。
    /// </summary>
    public VectorInt WindowSize { get; init; }

    /// <summary>
    /// ウィンドウのスケールを取得します。
    /// </summary>
    public float WindowScale { get; init; }

    /// <summary>
    /// ウィンドウの実際の幅（物理ピクセル）を取得します。
    /// </summary>
    public int ActualWidth { get; init; }

    /// <summary>
    /// ウィンドウの実際の高さ（物理ピクセル）を取得します。
    /// </summary>
    public int ActualHeight { get; init; }
}
