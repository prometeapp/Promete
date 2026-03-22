using System;
using System.Drawing;

namespace Promete.Graphics;

/// <summary>
/// <see cref="RenderTexture"/> の生成・管理を担うバックエンドインターフェースです。
/// </summary>
public interface IRenderTextureProvider
{
    /// <summary>
    /// 指定したサイズの <see cref="RenderTexture"/> を生成します。
    /// </summary>
    RenderTexture Create(VectorInt size);

    /// <summary>
    /// <see cref="RenderTexture"/> へのキャプチャを開始します。
    /// 返された <see cref="IDisposable"/> を Dispose するとバインドが解除されます。
    /// </summary>
    /// <param name="renderTexture">キャプチャ先のテクスチャ。</param>
    /// <param name="clearColor">クリアする色。null の場合はクリアしない。</param>
    IDisposable BeginCapture(RenderTexture renderTexture, Color? clearColor = null);

    /// <summary>
    /// <see cref="RenderTexture"/> のサイズを変更します。
    /// </summary>
    void Resize(RenderTexture renderTexture, VectorInt newSize);

    /// <summary>
    /// <see cref="RenderTexture"/> に関連する GL リソースを解放します。
    /// </summary>
    void Release(RenderTexture renderTexture);
}
