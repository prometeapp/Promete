namespace Promete.Nodes;

/// <summary>
/// タイルマップのレンダリングモードを指定します。
/// </summary>
public enum TilemapRenderingMode
{
    /// <summary>
    /// 自動的に最適なレンダリング方法を選択します。
    /// </summary>
    Auto,

    /// <summary>
    /// すべてのタイルをレンダリングします。
    /// </summary>
    RenderAll,

    /// <summary>
    /// スキャン方式でタイルをレンダリングします。
    /// </summary>
    Scan
}
