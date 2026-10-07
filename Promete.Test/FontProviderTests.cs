using FluentAssertions;
using Promete.Backends;
using Promete.Backends.Headless;
using Promete.Graphics.Fonts;
using Promete.Graphics.Fonts.FreeType;
using Promete.Headless;

namespace Promete.Test;

/// <summary>
/// <see cref="Font"/> の静的メソッドが、バックエンドの <see cref="IFontProvider"/> に委譲されることを確かめるテスト。
/// </summary>
public class FontProviderTests
{
    [Fact]
    public void StaticMethods_ShouldDelegateToBackendProvider()
    {
        using var app = PrometeApp.Create().Build<FontProviderBackend>(null);

        Font.FromFile("a.ttf", 12, FontStyle.Bold, false, 1)
            .Should()
            .Be(RecordingFontProvider.Font);
        Font.FromSystem("Family", 12).Should().Be(RecordingFontProvider.Font);
        Font.FromStream(Stream.Null, 12).Should().Be(RecordingFontProvider.Font);
        Font.GetDefault(12).Should().Be(RecordingFontProvider.Font);

        RecordingFontProvider
            .Calls.Should()
            .Equal(
                "FromFile:a.ttf:12:Bold:False:1",
                "FromSystem:Family",
                "FromStream",
                "GetDefault"
            );
        app.GetPlugin<IFontProvider>().Should().BeOfType<RecordingFontProvider>();
    }

    [Fact]
    public void DefaultBackend_ShouldProvideFreeTypeProvider()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        app.GetPlugin<IFontProvider>().Should().BeSameAs(FreeTypeFontProvider.Shared);
    }

    internal sealed class FontProviderBackend : HeadlessBackend
    {
        public override IFontProvider SetupFontProvider()
        {
            RecordingFontProvider.Calls.Clear();
            return new RecordingFontProvider();
        }
    }

    internal sealed class RecordingFontProvider : IFontProvider
    {
        public static List<string> Calls { get; } = [];

        public static Font Font { get; } = Font.FromGlyphSource(new NullGlyphSource());

        public Font FromFile(
            string path,
            float size,
            FontStyle style,
            bool isAntialiased,
            int faceIndex
        )
        {
            Calls.Add($"FromFile:{path}:{size}:{style}:{isAntialiased}:{faceIndex}");
            return Font;
        }

        public Font FromSystem(string familyName, float size, FontStyle style, bool isAntialiased)
        {
            Calls.Add($"FromSystem:{familyName}");
            return Font;
        }

        public Font FromStream(Stream stream, float size, FontStyle style, bool isAntialiased)
        {
            Calls.Add("FromStream");
            return Font;
        }

        public Font GetDefault(float size, FontStyle style, bool isAntialiased)
        {
            Calls.Add("GetDefault");
            return Font;
        }
    }

    private sealed class NullGlyphSource : IGlyphSource
    {
        public int SourceId => 0;

        public FontMetrics GetMetrics(in GlyphRenderOptions options) => default;

        public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
        {
            glyph = default;
            return false;
        }

        public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options) =>
            throw new NotSupportedException();

        public float GetKerning(int left, int right, in GlyphRenderOptions options) => 0;

        public void Dispose() { }
    }
}
