using Promete.Graphics;

namespace Promete.Nodes;

/// <summary>
/// マスク画像を使用して子要素を切り抜いて描画するコンテナです。
/// </summary>
/// <remarks>
/// このクラスは2つのマスキング方式をサポートします:
/// <list type="bullet">
/// <item><description>ステンシルバッファ方式（デフォルト、<see cref="UseAlphaMask"/> = false）: 高速でシンプル、マスクは完全表示/非表示の2値</description></item>
/// <item><description>アルファブレンディング方式（<see cref="UseAlphaMask"/> = true）: グラデーションマスクや部分透明に対応、やや複雑</description></item>
/// </list>
/// <para>
/// <strong>注意:</strong> MaskedContainerの入れ子はサポートされていません。
/// MaskedContainerの子としてMaskedContainerを配置した場合の動作は未定義です。
/// </para>
/// </remarks>
public class MaskedContainer : Container
{
    /// <summary>
    /// マスクに使用するテクスチャを取得または設定します。
    /// </summary>
    /// <remarks>
    /// nullの場合、このコンテナは通常の <see cref="Container"/> として動作します。
    /// </remarks>
    public Texture2D? MaskTexture { get; set; }

    /// <summary>
    /// アルファブレンディングによるマスクを使用するかどうかを取得または設定します。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>false（デフォルト）: ステンシルバッファによる2値マスク（高速）</description></item>
    /// <item><description>true: フレームバッファとアルファブレンディングによるグラデーションマスク対応</description></item>
    /// </list>
    /// <para>
    /// <strong>注意:</strong> MaskedContainerの入れ子はサポートされていません。
    /// </para>
    /// </remarks>
    public bool UseAlphaMask { get; set; } = false;

    /// <summary>
    /// MaskedContainer の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="maskTexture">マスクに使用するテクスチャ。nullの場合は通常のContainerとして動作します。</param>
    /// <param name="useAlphaMask">アルファブレンディングを使用するかどうか。デフォルトはfalse（ステンシルバッファ方式）。</param>
    /// <param name="isTrimmable">範囲外に出た子ノードを描画しないかどうか。</param>
    public MaskedContainer(Texture2D? maskTexture = null, bool useAlphaMask = false, bool isTrimmable = false)
        : base(isTrimmable)
    {
        MaskTexture = maskTexture;
        UseAlphaMask = useAlphaMask;
    }
}
