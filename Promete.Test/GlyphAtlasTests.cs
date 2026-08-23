using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.Graphics.Fonts.FreeType;
using Promete.Test.Fakes;

namespace Promete.Test;

public class GlyphAtlasTests
{
    private const string FontPath = "assets/Koruri.ttf";

    [Fact]
    public void グリフを登録するとテクスチャが割り当てられる()
    {
        using var fixture = new Fixture();

        var entry = fixture.Add('A');

        entry.IsEmpty.Should().BeFalse();
        entry.Texture.Handle.Should().NotBe(0);
        entry.Size.X.Should().BePositive();
        fixture.Atlas.GlyphCount.Should().Be(1);
        fixture.Atlas.PageCount.Should().Be(1);
    }

    [Fact]
    public void 同じグリフを再要求してもラスタライズし直さない()
    {
        using var fixture = new Fixture();

        var first = fixture.Add('A');
        var second = fixture.Add('A');

        second.Should().Be(first);
        fixture.Atlas.GlyphCount.Should().Be(1);
    }

    [Fact]
    public void サイズが異なるグリフは別のエントリになる()
    {
        using var fixture = new Fixture();

        var small = fixture.Add('A', new GlyphRenderOptions(16));
        var large = fixture.Add('A', new GlyphRenderOptions(32));

        fixture.Atlas.GlyphCount.Should().Be(2);
        large.Size.X.Should().BeGreaterThan(small.Size.X);
    }

    [Fact]
    public void アンチエイリアスの有無で別のエントリになる()
    {
        using var fixture = new Fixture();

        fixture.Add('A', new GlyphRenderOptions(16));
        fixture.Add('A', new GlyphRenderOptions(16, IsAntialiased: false));

        fixture.Atlas.GlyphCount.Should().Be(2);
    }

    [Fact]
    public void 複数のグリフは同一ページに配置され同じハンドルを共有する()
    {
        using var fixture = new Fixture();

        var handles = "Promete あいうえお"
            .Where(c => c != ' ')
            .Select(c => fixture.Add(c).Texture.Handle)
            .Distinct();

        handles.Should().ContainSingle("バッチ描画のため、全グリフが同一テクスチャに収まる必要がある");
        fixture.Atlas.PageCount.Should().Be(1);
    }

    [Fact]
    public void 空白文字は空のエントリとして登録される()
    {
        using var fixture = new Fixture();

        var entry = fixture.Add(' ');

        entry.IsEmpty.Should().BeTrue();
        fixture.Atlas.GlyphCount.Should().Be(1);
        fixture.Atlas.PageCount.Should().Be(0);
    }

    [Fact]
    public void 割り当てられたUVはグリフの領域を指す()
    {
        using var fixture = new Fixture(pageSize: 64);

        var entry = fixture.Add('A');
        var texture = entry.Texture;

        texture.UvStart.X.Should().BeApproximately(0, 0.0001f);
        texture.UvStart.Y.Should().BeApproximately(0, 0.0001f);
        texture.UvEnd.X.Should().BeApproximately(entry.Size.X / 64f, 0.0001f);
        texture.UvEnd.Y.Should().BeApproximately(entry.Size.Y / 64f, 0.0001f);
    }

    [Fact]
    public void グリフの内容がページテクスチャへ書き込まれる()
    {
        using var fixture = new Fixture(pageSize: 64);

        var entry = fixture.Add('A');
        var alphaValues = new List<byte>();
        for (var y = 0; y < entry.Size.Y; y++)
        for (var x = 0; x < entry.Size.X; x++)
            alphaValues.Add(fixture.Factory.GetAlphaAt(entry.Texture.Handle, (64, 64), (x, y)));

        alphaValues.Should().Contain(a => a > 0, "グリフの内容がページへ転送されている必要がある");
    }

    [Fact]
    public void ページが埋まると新しいページが確保される()
    {
        // 32px のグリフが 1 つ入る程度の極小ページを用意する
        using var fixture = new Fixture(pageSize: 40);
        var options = new GlyphRenderOptions(32);

        fixture.Add('A', options);
        fixture.Add('B', options);
        fixture.Add('C', options);

        fixture.Atlas.PageCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public void ページに収まらない巨大なグリフは専用テクスチャになる()
    {
        using var fixture = new Fixture(pageSize: 8);

        var entry = fixture.Add('A', new GlyphRenderOptions(64));

        entry.IsEmpty.Should().BeFalse();
        entry.Texture.UvStart.Should().Be(Vector.Zero);
        entry.Texture.UvEnd.Should().Be(Vector.One);
    }

    [Fact]
    public void Clearするとページが解放される()
    {
        using var fixture = new Fixture();
        fixture.Add('A');

        fixture.Atlas.Clear();

        fixture.Atlas.GlyphCount.Should().Be(0);
        fixture.Atlas.PageCount.Should().Be(0);
        fixture.Factory.DisposedHandles.Should().HaveCount(1);
    }

    [Fact]
    public void 破棄後に操作すると例外をスローする()
    {
        using var fixture = new Fixture();
        fixture.Atlas.Dispose();

        var act = () => fixture.Add('A');
        act.Should().Throw<ObjectDisposedException>();
    }

    private sealed class Fixture : IDisposable
    {
        private readonly FreeTypeGlyphSource _source = FreeTypeGlyphSource.FromFile(FontPath);

        public Fixture(int pageSize = 256)
        {
            Factory = new FakeTextureFactory();
            Atlas = new GlyphAtlas(Factory, pageSize);
        }

        public FakeTextureFactory Factory { get; }

        public GlyphAtlas Atlas { get; }

        public GlyphEntry Add(int codepoint, GlyphRenderOptions? options = null)
        {
            var resolved = options ?? new GlyphRenderOptions(16);
            _source.TryGetGlyph(codepoint, resolved, out var glyph).Should().BeTrue();
            return Atlas.GetOrAdd(glyph, resolved);
        }

        public void Dispose()
        {
            Atlas.Dispose();
            _source.Dispose();
        }
    }
}
