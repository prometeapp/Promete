using System;

namespace Promete.Graphics;

/// <summary>
/// <see cref="TextureFactoryBase"/> からバックエンドへ渡される、テクスチャの転送要求です。
/// </summary>
/// <remarks>
/// 呼び出しの間だけ有効です。<see cref="Rgba"/> を呼び出しの外へ保持しないでください。
/// </remarks>
public readonly ref struct TextureUploadRequest
{
    /// <summary>
    /// <see cref="TextureUploadRequest"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="rgba">RGBA8888 形式のビットマップデータ。</param>
    /// <param name="size">テクスチャのサイズ。</param>
    /// <param name="options">サンプリングの設定。</param>
    public TextureUploadRequest(ReadOnlySpan<byte> rgba, VectorInt size, TextureOptions options)
    {
        Rgba = rgba;
        Size = size;
        Options = options;
    }

    /// <summary>
    /// RGBA8888 形式のビットマップデータを取得します。
    /// ピクセルは左上から右下へ、行優先で並んでいます。
    /// </summary>
    public ReadOnlySpan<byte> Rgba { get; }

    /// <summary>
    /// テクスチャのサイズを取得します。
    /// </summary>
    public VectorInt Size { get; }

    /// <summary>
    /// サンプリングの設定を取得します。
    /// </summary>
    public TextureOptions Options { get; }
}
