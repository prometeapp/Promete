using System.Collections.Concurrent;
using System.Text;
using Promete.Graphics.Fonts;

namespace Promete.Test.AssetsApi;

/// <summary>
/// グリフソースの生成を数え、ストリームに対応しない環境 (Web) も再現できるテスト用のフォントプロバイダ。
/// </summary>
public sealed class FakeFontProvider : IFontProvider
{
    private readonly ConcurrentDictionary<string, FakeGlyphSource> _fileCache = new();
    private int _fromStreamCount;
    private int _fromFileCount;

    /// <summary>
    /// false にすると、<see cref="FromStream"/> が <see cref="NotSupportedException"/> を投げる (Web の CanvasFontProvider と同じ)。
    /// </summary>
    public bool SupportsStream { get; set; } = true;

    /// <summary>
    /// <see cref="FromStream"/> が呼ばれた回数。
    /// </summary>
    public int FromStreamCount => Volatile.Read(ref _fromStreamCount);

    /// <summary>
    /// <see cref="FromFile"/> が呼ばれた回数。
    /// </summary>
    public int FromFileCount => Volatile.Read(ref _fromFileCount);

    /// <summary>
    /// <see cref="FromFile"/> に渡されたパス。
    /// </summary>
    public ConcurrentQueue<string> FromFilePaths { get; } = new();

    /// <summary>
    /// 本物の provider と同じく、同じパスには同じグリフソースを返す (provider がキャッシュを持つ)。
    /// </summary>
    public Font FromFile(
        string path,
        float size,
        FontStyle style,
        bool isAntialiased,
        int faceIndex
    )
    {
        Interlocked.Increment(ref _fromFileCount);
        FromFilePaths.Enqueue(path);
        var source = _fileCache.GetOrAdd(path, p => new FakeGlyphSource(Encoding.UTF8.GetBytes(p)));
        return Font.FromGlyphSource(source, size, style, isAntialiased);
    }

    public Font FromSystem(string familyName, float size, FontStyle style, bool isAntialiased) =>
        throw new NotSupportedException();

    /// <summary>
    /// ストリームの中身をすべて読み、それを持つグリフソースを毎回新しく作る (FreeType と同じく、ストリームは閉じない)。
    /// </summary>
    public Font FromStream(Stream stream, float size, FontStyle style, bool isAntialiased)
    {
        if (!SupportsStream)
            throw new NotSupportedException("この環境はストリームからフォントを読めません。");

        Interlocked.Increment(ref _fromStreamCount);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Font.FromGlyphSource(
            new FakeGlyphSource(buffer.ToArray()),
            size,
            style,
            isAntialiased
        );
    }

    public Font GetDefault(float size, FontStyle style, bool isAntialiased) =>
        throw new NotSupportedException();
}

/// <summary>
/// 中身のバイト列と、Dispose された回数を持つテスト用のグリフソース。
/// </summary>
public sealed class FakeGlyphSource(byte[] data) : IGlyphSource
{
    private int _disposeCount;

    /// <summary>
    /// 読み込んだデータ。
    /// </summary>
    public byte[] Data { get; } = data;

    /// <summary>
    /// Dispose された回数。
    /// </summary>
    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool IsDisposed => DisposeCount > 0;

    public int SourceId { get; } = GlyphSourceId.Next();

    public FontMetrics GetMetrics(in GlyphRenderOptions options) => default;

    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
    {
        glyph = default;
        return false;
    }

    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options) =>
        throw new NotSupportedException();

    public float GetKerning(int left, int right, in GlyphRenderOptions options) => 0;

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}
