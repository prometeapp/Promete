using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Promete.Graphics.Fonts.FreeType;

namespace Promete.Graphics.Fonts;

/// <summary>
/// テキストの描画に用いるフォントを表します。
/// </summary>
/// <remarks>
/// このクラスはグリフの供給元 (<see cref="IGlyphSource" />) と、
/// サイズやスタイルといった描画設定を束ねた不変の値オブジェクトです。
/// グリフを実際にどう用意するかは <see cref="Source" /> の実装に委ねられるため、
/// ベクターフォント・画像ベースのビットマップフォント・外字のいずれであっても、
/// 同じように扱うことができます。
/// </remarks>
public sealed class Font : IEquatable<Font>
{
    private Font(IGlyphSource source, float size, FontStyle style, bool isAntialiased)
    {
        Source = source;
        Size = size;
        Style = style;
        IsAntialiased = isAntialiased;
        RenderOptions = new GlyphRenderOptions(size, isAntialiased, style);
    }

    /// <summary>
    /// グリフの供給元を取得します。
    /// </summary>
    public IGlyphSource Source { get; }

    /// <summary>
    /// フォントサイズを取得します。
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// フォントスタイルを取得します。
    /// </summary>
    public FontStyle Style { get; }

    /// <summary>
    /// アンチエイリアスが有効かどうかを取得します。
    /// </summary>
    public bool IsAntialiased { get; }

    /// <summary>
    /// このフォントに対応するグリフのレンダリングオプションを取得します。
    /// </summary>
    public GlyphRenderOptions RenderOptions { get; }

    /// <summary>
    /// このフォントのメトリクスを取得します。
    /// </summary>
    public FontMetrics Metrics => Source.GetMetrics(RenderOptions);

    /// <summary>
    /// ファイルパスを指定し、フォントを生成します。
    /// 同一のパスから生成されたフォントは、グリフの供給元を共有します。
    /// </summary>
    /// <param name="path">フォントファイルのパス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <exception cref="FileNotFoundException">フォントファイルが見つからない場合。</exception>
    public static Font FromFile(
        string path,
        float size = 16,
        FontStyle style = FontStyle.Normal,
        bool isAntialiased = true,
        int faceIndex = 0
    )
    {
        return GetProvider().FromFile(path, size, style, isAntialiased, faceIndex);
    }

    /// <summary>
    /// 実行環境にインストールされているフォントを、ファミリー名を指定して読み込みます。
    /// </summary>
    /// <param name="familyName">フォントファミリー名。指定可能な名前は環境によって異なります。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。専用の字形がない場合は合成されます。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <exception cref="FontException">指定したフォントが見つからない場合。</exception>
    public static Font FromSystem(
        string familyName,
        float size = 16,
        FontStyle style = FontStyle.Normal,
        bool isAntialiased = true
    )
    {
        return GetProvider().FromSystem(familyName, size, style, isAntialiased);
    }

    /// <summary>
    /// ストリームを指定し、フォントを生成します。
    /// </summary>
    public static Font FromStream(
        Stream stream,
        float size = 16,
        FontStyle style = FontStyle.Normal,
        bool isAntialiased = true
    )
    {
        return GetProvider().FromStream(stream, size, style, isAntialiased);
    }

    /// <summary>
    /// 任意のグリフソースからフォントを生成します。
    /// </summary>
    public static Font FromGlyphSource(
        IGlyphSource source,
        float size = 16,
        FontStyle style = FontStyle.Normal,
        bool isAntialiased = true
    )
    {
        return new Font(source, size, style, isAntialiased);
    }

    /// <summary>
    /// 実行環境の既定のフォントを生成します。
    /// </summary>
    public static Font GetDefault(
        float size = 16,
        FontStyle style = FontStyle.Normal,
        bool isAntialiased = true
    )
    {
        return GetProvider().GetDefault(size, style, isAntialiased);
    }

    /// <summary>
    /// フォントサイズを変更した新しいフォントを生成します。
    /// </summary>
    public Font With(float size)
    {
        return new Font(Source, size, Style, IsAntialiased);
    }

    /// <summary>
    /// フォントスタイルを変更した新しいフォントを生成します。
    /// </summary>
    public Font With(FontStyle style)
    {
        return new Font(Source, Size, style, IsAntialiased);
    }

    /// <summary>
    /// フォントサイズおよびスタイルを変更した新しいフォントを生成します。
    /// </summary>
    public Font With(float size, FontStyle style)
    {
        return new Font(Source, size, style, IsAntialiased);
    }

    /// <summary>
    /// フォントサイズ、スタイル、アンチエイリアスの有効/無効を変更した新しいフォントを生成します。
    /// </summary>
    public Font With(float size, FontStyle style, bool isAntialiased)
    {
        return new Font(Source, size, style, isAntialiased);
    }

    /// <summary>
    /// このフォントが収録していない文字を補完するグリフソースを追加した、新しいフォントを生成します。
    /// </summary>
    /// <param name="fallbacks">補完に用いるグリフソース。先に指定したものほど優先されます。</param>
    public Font WithFallback(params IGlyphSource[] fallbacks)
    {
        var sources = Source is CompositeGlyphSource composite
            ? composite.Sources.Concat(fallbacks)
            : [Source, .. fallbacks];

        return new Font(
            new CompositeGlyphSource(sources, GetMetricsSource()),
            Size,
            Style,
            IsAntialiased
        );
    }

    /// <summary>
    /// このフォントより優先して参照されるグリフソースを追加した、新しいフォントを生成します。
    /// 外字を差し込む場合に使用します。
    /// </summary>
    /// <param name="overrides">優先して参照するグリフソース。</param>
    public Font WithOverride(params IGlyphSource[] overrides)
    {
        IEnumerable<IGlyphSource> sources = Source is CompositeGlyphSource composite
            ? [.. overrides, .. composite.Sources]
            : [.. overrides, Source];

        // 外字を先頭へ置いても、行の高さは本文のフォントを基準に保つ
        return new Font(
            new CompositeGlyphSource(sources, GetMetricsSource()),
            Size,
            Style,
            IsAntialiased
        );
    }

    /// <summary>
    /// 行の高さの基準となるグリフソースを取得します。
    /// </summary>
    private IGlyphSource GetMetricsSource()
    {
        return Source is CompositeGlyphSource composite ? composite.MetricsSource : Source;
    }

    /// <inheritdoc />
    public bool Equals(Font? other)
    {
        return other is not null
            && ReferenceEquals(other.Source, Source)
            && other.Size.Equals(Size)
            && other.Style == Style
            && other.IsAntialiased == IsAntialiased;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Font font && Equals(font);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Size, Style, IsAntialiased);
    }

    /// <summary>
    /// 実行中のバックエンドが提供する <see cref="IFontProvider"/> を取得します。
    /// <see cref="PrometeApp"/> が無い場合は、FreeType を用いる既定の実装を返します。
    /// </summary>
    private static IFontProvider GetProvider()
    {
        return PrometeApp.CurrentOrNull?.FontProvider ?? FreeTypeFontProvider.Shared;
    }
}
