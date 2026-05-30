using System.Collections.Generic;
using Promete.Graphics;

namespace Promete;

/// <summary>
/// スクリーンへのブリット処理を抽象化するインターフェースです。
/// </summary>
public interface IScreenBlitter
{
    /// <summary>
    /// 全描画のキャプチャ先 RenderTexture を取得します。
    /// </summary>
    public RenderTexture ScreenRenderTexture { get; }

    /// <summary>
    /// ポストプロセスマテリアルを順番に適用してスクリーンへブリットします。
    /// </summary>
    /// <param name="materials">
    /// 適用するマテリアルのリスト。空の場合はデフォルトシェーダーで直接ブリットします。
    /// </param>
    public void BlitToScreen(IReadOnlyList<Material> materials);
}
