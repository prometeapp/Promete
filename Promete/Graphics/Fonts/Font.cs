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
    private static readonly Dictionary<string, IGlyphSource> SourceCache = new();

    private static readonly Lazy<IGlyphSource> DefaultSource = new(LoadDefaultSource);

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
        bool isAntialiased = true
    )
    {
        var fullPath = Path.GetFullPath(path);
        if (!SourceCache.TryGetValue(fullPath, out var source))
        {
            source = FreeTypeGlyphSource.FromFile(fullPath);
            SourceCache[fullPath] = source;
        }

        return new Font(source, size, style, isAntialiased);
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
        return new Font(FreeTypeGlyphSource.FromStream(stream), size, style, isAntialiased);
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
        return new Font(DefaultSource.Value, size, style, isAntialiased);
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

        return new Font(new CompositeGlyphSource(sources), Size, Style, IsAntialiased);
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

        return new Font(new CompositeGlyphSource(sources), Size, Style, IsAntialiased);
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
    /// 実行環境ごとの既定のフォントファイルを探索して読み込みます。
    /// </summary>
    private static IGlyphSource LoadDefaultSource()
    {
        foreach (var path in EnumerateDefaultFontPaths())
        {
            if (File.Exists(path))
                return FreeTypeGlyphSource.FromFile(path);
        }

        throw new FontException("既定のフォントが見つかりませんでした。");
    }

    private static IEnumerable<string> EnumerateDefaultFontPaths()
    {
        if (OperatingSystem.IsWindows())
        {
            var fonts = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Fonts"
            );
            return new[]
            {
                "BIZ-UDGothicR.ttc",
                "YuGothM.ttc",
                "meiryo.ttc",
                "msgothic.ttc",
                "arial.ttf",
            }.Select(name => Path.Combine(fonts, name));
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return
            [
                "/System/Library/Fonts/ヒラギノ角ゴシック W3.ttc",
                "/System/Library/Fonts/Hiragino Sans GB.ttc",
                "/System/Library/Fonts/Helvetica.ttc",
                "/Library/Fonts/Arial.ttf",
            ];
        }

        return
        [
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/noto-cjk/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/TTF/DejaVuSans.ttf",
        ];
    }
}
