using System;
using System.Collections.Generic;
using System.Linq;

namespace Promete.Graphics.Fonts;

/// <summary>
/// 複数のグリフソースを先頭から順に探索する、フォールバックチェーンを表します。
/// </summary>
/// <remarks>
/// 外字の差し込み、欧文フォントに対する和文フォントの補完、絵文字フォントの合成は、
/// いずれもチェーンのどこにソースを差し込むかの違いでしかありません。
/// </remarks>
public sealed class CompositeGlyphSource : IGlyphSource, INamedGlyphSource
{
    private readonly IGlyphSource[] _sources;
    private readonly bool _leavesOpen;
    private bool _isDisposed;

    /// <summary>
    /// <see cref="CompositeGlyphSource" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="sources">探索するグリフソース。先頭ほど優先されます。</param>
    /// <param name="leavesOpen">
    /// <c>true</c> の場合、このインスタンスを破棄しても各グリフソースを破棄しません。
    /// </param>
    public CompositeGlyphSource(IEnumerable<IGlyphSource> sources, bool leavesOpen = true)
    {
        _sources = sources.ToArray();
        if (_sources.Length == 0)
            throw new ArgumentException("グリフソースを 1 つ以上指定してください。", nameof(sources));

        _leavesOpen = leavesOpen;
        SourceId = _sources[0].SourceId;
    }

    /// <inheritdoc />
    /// <remarks>
    /// グリフは実際に供給したソースの ID でキャッシュされるため、
    /// この値がアトラスのキーに使われることはありません。
    /// </remarks>
    public int SourceId { get; }

    /// <summary>
    /// 探索対象のグリフソースを取得します。
    /// </summary>
    public IReadOnlyList<IGlyphSource> Sources => _sources;

    /// <inheritdoc />
    /// <remarks>行の高さは、チェーンの先頭のソースを基準とします。</remarks>
    public FontMetrics GetMetrics(in GlyphRenderOptions options)
    {
        return _sources[0].GetMetrics(options);
    }

    /// <inheritdoc />
    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
    {
        foreach (var source in _sources)
        {
            if (source.TryGetGlyph(codepoint, options, out glyph))
                return true;
        }

        glyph = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetCodepointByName(string name, out int codepoint)
    {
        foreach (var source in _sources)
        {
            if (source is INamedGlyphSource named && named.TryGetCodepointByName(name, out codepoint))
                return true;
        }

        codepoint = 0;
        return false;
    }

    /// <inheritdoc />
    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options)
    {
        // グリフは供給元のソースを保持しているため、そちらへ委譲します。
        return glyph.Source.Rasterize(glyph, options);
    }

    /// <inheritdoc />
    /// <remarks>カーニングは、両方のグリフを供給できるソースのみが提供します。</remarks>
    public float GetKerning(int left, int right, in GlyphRenderOptions options)
    {
        foreach (var source in _sources)
        {
            if (source.TryGetGlyph(left, options, out _) && source.TryGetGlyph(right, options, out _))
                return source.GetKerning(left, right, options);
        }

        return 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        if (_leavesOpen)
            return;

        foreach (var source in _sources)
            source.Dispose();
    }
}
