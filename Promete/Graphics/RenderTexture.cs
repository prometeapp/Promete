using System;
using System.Drawing;

namespace Promete.Graphics;

/// <summary>
/// 描画内容をテクスチャとして保持する低レイヤー描画ターゲットです。
/// バックエンドに依存しない抽象として設計されています。
/// </summary>
public sealed class RenderTexture : IDisposable
{
    /// <summary>
    /// レンダリング結果のテクスチャを取得します。
    /// </summary>
    public Texture2D Texture { get; internal set; }

    /// <summary>
    /// このテクスチャのサイズを取得します。
    /// </summary>
    public VectorInt Size { get; internal set; }

    private readonly IRenderTextureProvider _provider;
    private bool _disposed;

    internal RenderTexture(VectorInt size, Texture2D texture, IRenderTextureProvider provider)
    {
        Size = size;
        Texture = texture;
        _provider = provider;
    }

    /// <summary>
    /// このテクスチャへのキャプチャを開始します。
    /// </summary>
    /// <param name="clearColor">クリアする色。null の場合はクリアしない。</param>
    /// <returns>Dispose() でキャプチャを終了するスコープ。</returns>
    public IDisposable BeginCapture(Color? clearColor = null) =>
        _provider.BeginCapture(this, clearColor);

    /// <summary>
    /// サイズを変更します。
    /// </summary>
    public void Resize(VectorInt newSize)
    {
        Size = newSize;
        _provider.Resize(this, newSize);
    }

    /// <summary>
    /// このオブジェクトが保持する GL リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _provider.Release(this);
    }
}
