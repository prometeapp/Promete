using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace Promete.Graphics.Fonts;

/// <summary>
/// 画像から切り出したグリフを供給する、ビットマップフォントのグリフソースです。
/// </summary>
/// <remarks>
/// 等幅のフォント画像を格子状に切り出す用途のほか、
/// 1 文字ずつ画像を登録して外字として使うこともできます。
/// フォントサイズは元の大きさに対する整数倍として解決されるため、
/// 拡大してもドットが崩れません。
/// </remarks>
public sealed class BitmapGlyphSource : IGlyphSource, INamedGlyphSource
{
    /// <summary>
    /// 名前で登録されたグリフに、コードポイントを自動的に割り当てる際の開始位置。
    /// Unicode の私用領域 (Private Use Area) の先頭です。
    /// </summary>
    private const int PrivateUseAreaStart = 0xE000;

    private readonly Dictionary<int, BitmapGlyph> _glyphs = new();
    private readonly Dictionary<string, int> _namedGlyphs = new();
    private int _nextPrivateUseCodepoint = PrivateUseAreaStart;
    private readonly int _nativeSize;
    private readonly int _baseline;
    private uint _nextGlyphIndex = 1;
    private bool _isDisposed;

    /// <summary>
    /// <see cref="BitmapGlyphSource" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="nativeSize">この画像が本来想定している行の高さ。</param>
    /// <param name="baseline">グリフの上端からベースラインまでの距離。</param>
    public BitmapGlyphSource(int nativeSize, int? baseline = null)
    {
        if (nativeSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(nativeSize));

        _nativeSize = nativeSize;
        _baseline = baseline ?? nativeSize;
        SourceId = GlyphSourceId.Next();
    }

    /// <inheritdoc />
    public int SourceId { get; }

    /// <summary>
    /// この画像が本来想定している行の高さを取得します。
    /// </summary>
    public int NativeSize => _nativeSize;

    /// <summary>
    /// 登録されているグリフの数を取得します。
    /// </summary>
    public int GlyphCount => _glyphs.Count;

    /// <summary>
    /// 格子状に文字が並んだ画像から、等幅のビットマップフォントを生成します。
    /// </summary>
    /// <param name="path">フォント画像のパス。</param>
    /// <param name="cellSize">1 文字あたりの大きさ。</param>
    /// <param name="characters">画像に含まれる文字を、左上から右下の順に並べた文字列。</param>
    /// <param name="baseline">グリフの上端からベースラインまでの距離。</param>
    public static BitmapGlyphSource FromGrid(
        string path,
        VectorInt cellSize,
        string characters,
        int? baseline = null
    )
    {
        using var stream = File.OpenRead(path);
        return FromGrid(stream, cellSize, characters, baseline);
    }

    /// <summary>
    /// 格子状に文字が並んだ画像から、等幅のビットマップフォントを生成します。
    /// </summary>
    public static BitmapGlyphSource FromGrid(
        Stream stream,
        VectorInt cellSize,
        string characters,
        int? baseline = null
    )
    {
        if (cellSize.X <= 0 || cellSize.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellSize));

        using var image = Image.Load<Rgba32>(stream);
        var source = new BitmapGlyphSource(cellSize.Y, baseline);
        var columns = image.Width / cellSize.X;
        if (columns <= 0)
            throw new ArgumentException("画像の幅がセルの幅より小さいです。", nameof(cellSize));

        var index = 0;
        foreach (var rune in EnumerateCodepoints(characters))
        {
            var x = (index % columns) * cellSize.X;
            var y = (index / columns) * cellSize.Y;
            if (y + cellSize.Y > image.Height)
                throw new ArgumentException("画像に対して文字数が多すぎます。", nameof(characters));

            source.Register(rune, Crop(image, x, y, cellSize), cellSize);
            index++;
        }

        return source;
    }

    /// <summary>
    /// 画像ファイルから 1 文字分のグリフを登録します。外字の登録に使用します。
    /// </summary>
    /// <param name="codepoint">割り当てる Unicode コードポイント。</param>
    /// <param name="path">画像のパス。</param>
    /// <param name="bearing">ベースライン原点から画像左上までのオフセット。既定では画像の下端がベースラインに揃います。</param>
    /// <param name="advance">送り幅。省略した場合は画像の幅が使われます。</param>
    public void Register(
        int codepoint,
        string path,
        VectorInt? bearing = null,
        int? advance = null
    )
    {
        using var image = Image.Load<Rgba32>(path);
        var size = new VectorInt(image.Width, image.Height);
        Register(codepoint, Crop(image, 0, 0, size), size, bearing, advance);
    }

    /// <summary>
    /// 名前を付けてグリフを登録します。コードポイントは私用領域から自動的に割り当てられます。
    /// PTML では <c>&lt;tex=名前&gt;</c> として本文へ差し込めます。
    /// </summary>
    /// <param name="name">グリフに付ける名前。</param>
    /// <param name="path">画像のパス。</param>
    /// <param name="bearing">ベースライン原点から画像左上までのオフセット。</param>
    /// <param name="advance">送り幅。省略した場合は画像の幅が使われます。</param>
    /// <returns>割り当てられたコードポイント。</returns>
    public int Register(
        string name,
        string path,
        VectorInt? bearing = null,
        int? advance = null
    )
    {
        using var image = Image.Load<Rgba32>(path);
        var size = new VectorInt(image.Width, image.Height);
        return Register(name, Crop(image, 0, 0, size), size, bearing, advance);
    }

    /// <summary>
    /// 名前を付けてグリフを登録します。コードポイントは私用領域から自動的に割り当てられます。
    /// </summary>
    /// <param name="name">グリフに付ける名前。</param>
    /// <param name="pixels">RGBA8888 形式のピクセルデータ。</param>
    /// <param name="size">画像のサイズ。</param>
    /// <param name="bearing">ベースライン原点から画像左上までのオフセット。</param>
    /// <param name="advance">送り幅。省略した場合は画像の幅が使われます。</param>
    /// <returns>割り当てられたコードポイント。</returns>
    public int Register(
        string name,
        byte[] pixels,
        VectorInt size,
        VectorInt? bearing = null,
        int? advance = null
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (!_namedGlyphs.TryGetValue(name, out var codepoint))
        {
            codepoint = _nextPrivateUseCodepoint++;
            _namedGlyphs[name] = codepoint;
        }

        Register(codepoint, pixels, size, bearing, advance);
        return codepoint;
    }

    /// <inheritdoc />
    public bool TryGetCodepointByName(string name, out int codepoint)
    {
        return _namedGlyphs.TryGetValue(name, out codepoint);
    }

    /// <summary>
    /// ピクセルデータから 1 文字分のグリフを登録します。
    /// </summary>
    /// <param name="codepoint">割り当てる Unicode コードポイント。</param>
    /// <param name="pixels">RGBA8888 形式のピクセルデータ。</param>
    /// <param name="size">画像のサイズ。</param>
    /// <param name="bearing">ベースライン原点から画像左上までのオフセット。</param>
    /// <param name="advance">送り幅。省略した場合は画像の幅が使われます。</param>
    public void Register(
        int codepoint,
        byte[] pixels,
        VectorInt size,
        VectorInt? bearing = null,
        int? advance = null
    )
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (pixels.Length < size.X * size.Y * 4)
            throw new ArgumentException("ピクセルデータが不足しています。", nameof(pixels));

        _glyphs[codepoint] = new BitmapGlyph(
            _nextGlyphIndex++,
            pixels,
            size,
            bearing ?? new VectorInt(0, -_baseline),
            advance ?? size.X
        );
    }

    /// <inheritdoc />
    public FontMetrics GetMetrics(in GlyphRenderOptions options)
    {
        var scale = GetScale(options.Size);
        return new FontMetrics(
            _baseline * scale,
            (_baseline - _nativeSize) * scale,
            _nativeSize * scale
        );
    }

    /// <inheritdoc />
    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_glyphs.TryGetValue(codepoint, out var bitmapGlyph))
        {
            glyph = default;
            return false;
        }

        glyph = new GlyphInfo
        {
            Source = this,
            GlyphIndex = bitmapGlyph.Index,
            Codepoint = codepoint,
            Advance = bitmapGlyph.Advance * GetScale(options.Size),
        };
        return true;
    }

    /// <inheritdoc />
    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_glyphs.TryGetValue(glyph.Codepoint, out var bitmapGlyph))
            return GlyphBitmap.Empty;

        var scale = GetScale(options.Size);
        return scale == 1
            ? new GlyphBitmap(bitmapGlyph.Pixels, bitmapGlyph.Size, bitmapGlyph.Bearing)
            : new GlyphBitmap(
                Upscale(bitmapGlyph.Pixels, bitmapGlyph.Size, scale),
                bitmapGlyph.Size * scale,
                bitmapGlyph.Bearing * scale
            );
    }

    /// <inheritdoc />
    /// <remarks>ビットマップフォントはカーニングを持ちません。</remarks>
    public float GetKerning(int left, int right, in GlyphRenderOptions options)
    {
        return 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        _glyphs.Clear();
        _namedGlyphs.Clear();
    }

    /// <summary>
    /// 要求されたフォントサイズを、元の大きさに対する整数倍へ解決します。
    /// </summary>
    private int GetScale(float size)
    {
        // 既定の丸めは偶数寄せのため、2.5 倍などが意図せず縮小されてしまう
        return Math.Max(1, (int)MathF.Round(size / _nativeSize, MidpointRounding.AwayFromZero));
    }

    private static IEnumerable<int> EnumerateCodepoints(string text)
    {
        for (var i = 0; i < text.Length; )
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                yield return char.ConvertToUtf32(text[i], text[i + 1]);
                i += 2;
                continue;
            }

            yield return text[i];
            i++;
        }
    }

    private static byte[] Crop(Image<Rgba32> image, int x, int y, VectorInt size)
    {
        var pixels = new byte[size.X * size.Y * 4];
        for (var row = 0; row < size.Y; row++)
        {
            var span = image.DangerousGetPixelRowMemory(y + row).Span.Slice(x, size.X);
            MemoryMarshal.AsBytes(span).CopyTo(pixels.AsSpan(row * size.X * 4));
        }

        return pixels;
    }

    /// <summary>
    /// ピクセルデータを最近傍補間で整数倍に拡大します。
    /// </summary>
    private static byte[] Upscale(byte[] pixels, VectorInt size, int scale)
    {
        var width = size.X * scale;
        var result = new byte[width * size.Y * scale * 4];

        for (var y = 0; y < size.Y; y++)
        for (var x = 0; x < size.X; x++)
        {
            var source = ((y * size.X) + x) * 4;
            for (var dy = 0; dy < scale; dy++)
            for (var dx = 0; dx < scale; dx++)
            {
                var destination = ((((y * scale) + dy) * width) + (x * scale) + dx) * 4;
                pixels.AsSpan(source, 4).CopyTo(result.AsSpan(destination, 4));
            }
        }

        return result;
    }

    /// <summary>
    /// 登録された 1 文字分のビットマップを表します。
    /// </summary>
    private readonly record struct BitmapGlyph(
        uint Index,
        byte[] Pixels,
        VectorInt Size,
        VectorInt Bearing,
        int Advance
    );
}
