using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FreeTypeSharp;

namespace Promete.Graphics.Fonts.FreeType;

/// <summary>
/// FreeType によってラスタライズされる、ベクターフォントおよび
/// 埋め込みビットマップフォントのグリフソースです。
/// </summary>
public sealed unsafe class FreeTypeGlyphSource : IGlyphSource
{
    /// <summary>
    /// モノクロレンダリングを指示する <c>FT_LOAD_TARGET_MONO</c> の値。
    /// FreeTypeSharp が定数を公開していないため、ここで定義しています。
    /// </summary>
    private const int LoadTargetMono = (int)FT_Render_Mode_.FT_RENDER_MODE_MONO << 16;

    private const int FaceFlagScalable = 1 << 0;
    private const int FaceFlagFixedSizes = 1 << 1;
    private const int FaceFlagKerning = 1 << 6;

    private static int _nextSourceId;

    private readonly Dictionary<(uint Index, GlyphRenderOptions Options), GlyphInfo> _glyphCache =
        new();

    private readonly FT_FaceRec_* _face;
    private readonly void* _fontData;
    private float _currentSize = float.NaN;
    private bool _isDisposed;

    private FreeTypeGlyphSource(FT_FaceRec_* face, void* fontData)
    {
        _face = face;
        _fontData = fontData;
        SourceId = Interlocked.Increment(ref _nextSourceId);

        FamilyName = Marshal.PtrToStringUTF8((IntPtr)face->family_name) ?? string.Empty;
        StyleName = Marshal.PtrToStringUTF8((IntPtr)face->style_name) ?? string.Empty;

        var flags = face->face_flags.ToInt64();
        IsScalable = (flags & FaceFlagScalable) != 0;
        HasFixedSizes = (flags & FaceFlagFixedSizes) != 0;
        HasKerning = (flags & FaceFlagKerning) != 0;
    }

    /// <inheritdoc />
    public int SourceId { get; }

    /// <summary>
    /// フォントファミリー名を取得します。
    /// </summary>
    public string FamilyName { get; }

    /// <summary>
    /// フォントのスタイル名を取得します。
    /// </summary>
    public string StyleName { get; }

    /// <summary>
    /// アウトラインによるスケーラブルなフォントかどうかを取得します。
    /// </summary>
    public bool IsScalable { get; }

    /// <summary>
    /// 埋め込みビットマップのストライクを持つかどうかを取得します。
    /// </summary>
    public bool HasFixedSizes { get; }

    /// <summary>
    /// カーニング情報を持つかどうかを取得します。
    /// </summary>
    public bool HasKerning { get; }

    /// <summary>
    /// ファイルからグリフソースを生成します。
    /// </summary>
    /// <param name="path">フォントファイルのパス。</param>
    /// <param name="faceIndex">TTC などのコレクション内におけるフェイスのインデックス。</param>
    public static FreeTypeGlyphSource FromFile(string path, int faceIndex = 0)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("フォントファイルが見つかりません。", path);

        // FT_New_Face のパス解釈は環境依存であるため、常にメモリ経由で読み込みます。
        return FromMemory(File.ReadAllBytes(path), faceIndex);
    }

    /// <summary>
    /// ストリームからグリフソースを生成します。
    /// </summary>
    public static FreeTypeGlyphSource FromStream(Stream stream, int faceIndex = 0)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return FromMemory(memory.ToArray(), faceIndex);
    }

    /// <summary>
    /// メモリ上のフォントデータからグリフソースを生成します。
    /// </summary>
    public static FreeTypeGlyphSource FromMemory(ReadOnlySpan<byte> data, int faceIndex = 0)
    {
        // FreeType はフェイスが破棄されるまでバッファを参照し続けるため、ネイティブメモリへ複製します。
        var buffer = NativeMemory.Alloc((nuint)data.Length);
        data.CopyTo(new Span<byte>(buffer, data.Length));

        FT_FaceRec_* face;
        var error = FT.FT_New_Memory_Face(
            FreeTypeContext.Library,
            (byte*)buffer,
            (IntPtr)data.Length,
            (IntPtr)faceIndex,
            &face
        );

        if (error != FT_Error.FT_Err_Ok)
        {
            NativeMemory.Free(buffer);
            throw new FontException($"フォントの読み込みに失敗しました。({error})");
        }

        return new FreeTypeGlyphSource(face, buffer);
    }

    /// <inheritdoc />
    public FontMetrics GetMetrics(in GlyphRenderOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ApplySize(options.Size);
        var metrics = _face->size->metrics;
        return new FontMetrics(
            FromF26Dot6(metrics.ascender),
            FromF26Dot6(metrics.descender),
            FromF26Dot6(metrics.height)
        );
    }

    /// <inheritdoc />
    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        glyph = default;
        if (codepoint < 0)
            return false;

        var index = FT.FT_Get_Char_Index(_face, (UIntPtr)codepoint);
        if (index == 0)
            return false;

        if (_glyphCache.TryGetValue((index, options), out glyph))
            return true;

        if (!LoadGlyph(index, options))
            return false;

        glyph = new GlyphInfo
        {
            Source = this,
            GlyphIndex = index,
            Codepoint = codepoint,
            Advance = FromF26Dot6(_face->glyph->advance.x),
        };
        _glyphCache[(index, options)] = glyph;
        return true;
    }

    /// <inheritdoc />
    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!LoadGlyph(glyph.GlyphIndex, options))
            throw new FontException($"グリフ {glyph.GlyphIndex} の読み込みに失敗しました。");

        var slot = _face->glyph;
        var renderMode = options.IsAntialiased
            ? FT_Render_Mode_.FT_RENDER_MODE_NORMAL
            : FT_Render_Mode_.FT_RENDER_MODE_MONO;

        // 埋め込みビットマップが読み込まれた場合、既にラスタライズ済みのため再レンダリングしません。
        if (slot->format != FT_Glyph_Format_.FT_GLYPH_FORMAT_BITMAP)
            FreeTypeContext.ThrowIfError(
                FT.FT_Render_Glyph(slot, renderMode),
                $"グリフ {glyph.GlyphIndex} のラスタライズに失敗しました。"
            );

        var bitmap = slot->bitmap;
        if (bitmap.width == 0 || bitmap.rows == 0)
            return GlyphBitmap.Empty;

        return new GlyphBitmap(
            ConvertToRgba(bitmap),
            new VectorInt((int)bitmap.width, (int)bitmap.rows),
            new VectorInt(slot->bitmap_left, -slot->bitmap_top)
        );
    }

    /// <inheritdoc />
    public float GetKerning(int left, int right, in GlyphRenderOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!HasKerning)
            return 0;

        var leftIndex = FT.FT_Get_Char_Index(_face, (UIntPtr)left);
        var rightIndex = FT.FT_Get_Char_Index(_face, (UIntPtr)right);
        if (leftIndex == 0 || rightIndex == 0)
            return 0;

        ApplySize(options.Size);

        FT_Vector_ kerning;
        var error = FT.FT_Get_Kerning(
            _face,
            leftIndex,
            rightIndex,
            FT_Kerning_Mode_.FT_KERNING_DEFAULT,
            &kerning
        );

        return error == FT_Error.FT_Err_Ok ? FromF26Dot6(kerning.x) : 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        _glyphCache.Clear();
        FT.FT_Done_Face(_face);
        NativeMemory.Free(_fontData);
    }

    /// <summary>
    /// 26.6 固定小数点数を実数に変換します。
    /// </summary>
    private static float FromF26Dot6(IntPtr value)
    {
        return value.ToInt64() / 64f;
    }

    private static FT_LOAD GetLoadFlags(in GlyphRenderOptions options)
    {
        return options.IsAntialiased
            ? FT_LOAD.FT_LOAD_DEFAULT
            : FT_LOAD.FT_LOAD_MONOCHROME | (FT_LOAD)LoadTargetMono;
    }

    /// <summary>
    /// FreeType のビットマップを RGBA8888 に正規化します。
    /// </summary>
    private static byte[] ConvertToRgba(in FT_Bitmap_ bitmap)
    {
        var width = (int)bitmap.width;
        var height = (int)bitmap.rows;
        var pixels = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            var row = bitmap.buffer + (y * bitmap.pitch);
            var destination = y * width * 4;

            switch (bitmap.pixel_mode)
            {
                case FT_Pixel_Mode_.FT_PIXEL_MODE_MONO:
                    for (var x = 0; x < width; x++)
                    {
                        var bit = (row[x >> 3] >> (7 - (x & 7))) & 1;
                        WriteWhite(pixels, destination + (x * 4), (byte)(bit * 255));
                    }

                    break;

                case FT_Pixel_Mode_.FT_PIXEL_MODE_GRAY:
                    for (var x = 0; x < width; x++)
                        WriteWhite(pixels, destination + (x * 4), row[x]);

                    break;

                case FT_Pixel_Mode_.FT_PIXEL_MODE_BGRA:
                    for (var x = 0; x < width; x++)
                        WriteUnpremultipliedBgra(pixels, destination + (x * 4), row + (x * 4));

                    break;

                default:
                    throw new FontException(
                        $"未対応のピクセル形式です。({bitmap.pixel_mode})"
                    );
            }
        }

        return pixels;
    }

    private static void WriteWhite(byte[] pixels, int offset, byte alpha)
    {
        pixels[offset] = 255;
        pixels[offset + 1] = 255;
        pixels[offset + 2] = 255;
        pixels[offset + 3] = alpha;
    }

    /// <summary>
    /// 乗算済みアルファの BGRA ピクセルを、乗算前の RGBA に変換して書き込みます。
    /// </summary>
    private static void WriteUnpremultipliedBgra(byte[] pixels, int offset, byte* source)
    {
        var alpha = source[3];
        if (alpha == 0)
        {
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = pixels[offset + 3] = 0;
            return;
        }

        pixels[offset] = (byte)Math.Min(255, source[2] * 255 / alpha);
        pixels[offset + 1] = (byte)Math.Min(255, source[1] * 255 / alpha);
        pixels[offset + 2] = (byte)Math.Min(255, source[0] * 255 / alpha);
        pixels[offset + 3] = alpha;
    }

    /// <summary>
    /// グリフをスロットへ読み込み、必要に応じてスタイルを合成します。
    /// </summary>
    private bool LoadGlyph(uint glyphIndex, in GlyphRenderOptions options)
    {
        ApplySize(options.Size);

        if (FT.FT_Load_Glyph(_face, glyphIndex, GetLoadFlags(options)) != FT_Error.FT_Err_Ok)
            return false;

        // 専用の字形を持たないスタイルは、字形を変形させて合成します。
        if (options.IsBold)
            FT.FT_GlyphSlot_Embolden(_face->glyph);
        if (options.IsItalic)
            FT.FT_GlyphSlot_Oblique(_face->glyph);

        return true;
    }

    /// <summary>
    /// 指定したピクセルサイズをフェイスに適用します。直前と同じサイズであれば何もしません。
    /// </summary>
    private void ApplySize(float size)
    {
        if (_currentSize.Equals(size))
            return;

        // 72dpi を指定することで、ポイント数がそのままピクセル数として解決されます。
        var error = FT.FT_Set_Char_Size(_face, IntPtr.Zero, (IntPtr)(long)(size * 64), 72, 72);
        if (error != FT_Error.FT_Err_Ok)
            SelectNearestFixedSize(size);

        _currentSize = size;
    }

    /// <summary>
    /// スケーラブルでないフォントに対し、要求サイズに最も近いストライクを選択します。
    /// </summary>
    private void SelectNearestFixedSize(float size)
    {
        if (_face->num_fixed_sizes <= 0)
            throw new FontException($"フォントサイズ {size} を適用できませんでした。");

        var nearestIndex = 0;
        var nearestDistance = float.MaxValue;
        for (var i = 0; i < _face->num_fixed_sizes; i++)
        {
            var distance = Math.Abs(_face->available_sizes[i].height - size);
            if (distance >= nearestDistance)
                continue;
            nearestDistance = distance;
            nearestIndex = i;
        }

        FreeTypeContext.ThrowIfError(
            FT.FT_Select_Size(_face, nearestIndex),
            $"フォントサイズ {size} に対応するストライクを選択できませんでした。"
        );
    }
}
