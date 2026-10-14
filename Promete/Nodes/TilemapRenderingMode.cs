namespace Promete.Nodes;

/// <summary>
/// タイルマップのレンダリングモードを指定します。
/// </summary>
public enum TilemapRenderingMode
{
    /// <summary>
    /// 自動的に最適なレンダリング方法を選択します。
    /// 登録済みタイル数が画面に映る範囲のマス数以下なら <see cref="RenderAll"/>、多ければ <see cref="Scan"/> を選びます。
    /// </summary>
    Auto,

    /// <summary>
    /// すべてのタイルをレンダリングします。
    /// </summary>
    RenderAll,

    /// <summary>
    /// スキャン方式でタイルをレンダリングします。
    /// </summary>
    Scan,
}
