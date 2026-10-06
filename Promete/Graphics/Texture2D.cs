using System;

namespace Promete.Graphics;

/// <summary>
/// 2Dテクスチャのハンドルをラップします。
/// </summary>
public readonly struct Texture2D : IDisposable
{
    private readonly Action<Texture2D> _onDispose;

    /// <summary>
    /// バックエンドのテクスチャハンドルから <see cref="Texture2D"/> を生成します。
    /// 通常は <see cref="TextureFactoryBase"/> を使用してください。バックエンドの実装向けです。
    /// </summary>
    /// <param name="handle">バックエンドのテクスチャハンドル。</param>
    /// <param name="size">テクスチャのサイズ。</param>
    /// <param name="onDispose">破棄時に呼ばれる処理。</param>
    public Texture2D(int handle, VectorInt size, Action<Texture2D> onDispose)
        : this(handle, size, onDispose, (0, 0), (1, 1)) { }

    /// <summary>
    /// バックエンドのテクスチャハンドルと UV 範囲から <see cref="Texture2D"/> を生成します。
    /// 通常は <see cref="TextureFactoryBase"/> を使用してください。バックエンドの実装向けです。
    /// </summary>
    /// <param name="handle">バックエンドのテクスチャハンドル。</param>
    /// <param name="size">テクスチャのサイズ。</param>
    /// <param name="onDispose">破棄時に呼ばれる処理。</param>
    /// <param name="uvStart">左上の UV 座標。</param>
    /// <param name="uvEnd">右下の UV 座標。</param>
    public Texture2D(
        int handle,
        VectorInt size,
        Action<Texture2D> onDispose,
        Vector uvStart,
        Vector uvEnd
    )
    {
        Handle = handle;
        Size = size;
        _onDispose = onDispose;
        UvStart = uvStart;
        UvEnd = uvEnd;
    }

    /// <summary>
    /// このテクスチャのOpenGLハンドルを取得します。
    /// </summary>
    public int Handle { get; }

    /// <summary>
    /// このテクスチャのサイズを取得します。
    /// </summary>
    public VectorInt Size { get; }

    /// <summary>
    /// このテクスチャの左上のUV座標を取得します。
    /// </summary>
    public Vector UvStart { get; }

    /// <summary>
    /// このテクスチャの右下のUV座標を取得します。
    /// </summary>
    public Vector UvEnd { get; }

    /// <summary>
    /// この <see cref="Texture2D" /> を破棄します。
    /// </summary>
    public void Dispose()
    {
        _onDispose?.Invoke(this);
    }
}
