using System.Drawing;
using FluentAssertions;
using Promete.Graphics;
using Promete.Graphics.Imaging;
using Promete.Test.Fakes;

namespace Promete.Test;

/// <summary>
/// <see cref="Promete.Graphics.TextureFactoryBase"/> の共通実装を検証します。
/// </summary>
public class TextureFactoryBaseTests
{
    [Fact]
    public void Loadはデコードした画像をアップロードする()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 2);

        var texture = factory.Load(stream);

        texture.Size.Should().Be(new VectorInt(4, 2));
        factory.CreatedCount.Should().Be(1);
        factory.GetAlphaAt(texture.Handle, texture.Size, (0, 0)).Should().Be(255);
    }

    [Fact]
    public void 破棄するとバックエンドのハンドルが破棄される()
    {
        var factory = new FakeTextureFactory();
        var texture = factory.CreateSolid(Color.Red, (2, 2));

        texture.Dispose();

        factory.DisposedHandles.Should().Equal(texture.Handle);
    }

    [Fact]
    public void CreateSolidは指定色で塗りつぶす()
    {
        var factory = new FakeTextureFactory();

        var texture = factory.CreateSolid(Color.FromArgb(128, 1, 2, 3), (2, 2));

        factory.GetSize(texture.Handle).Should().Be(new VectorInt(2, 2));
        factory.GetAlphaAt(texture.Handle, (2, 2), (1, 1)).Should().Be(128);
    }

    [Fact]
    public void 三次元配列からも生成できる()
    {
        var factory = new FakeTextureFactory();
        var bitmap = new byte[2, 1, 4];
        bitmap[1, 0, 3] = 200;

        var texture = factory.Create(bitmap);

        texture.Size.Should().Be(new VectorInt(2, 1));
        factory.GetAlphaAt(texture.Handle, texture.Size, (1, 0)).Should().Be(200);
    }

    [Fact]
    public void ビットマップがサイズに対して不足していると例外になる()
    {
        var factory = new FakeTextureFactory();

        var act = () => factory.Create(new byte[15], (2, 2));

        act.Should().Throw<ArgumentException>();
        factory.CreatedCount.Should().Be(0);
    }

    [Fact]
    public void Updateは範囲外を指定すると例外になる()
    {
        var factory = new FakeTextureFactory();
        var texture = factory.CreateSolid(Color.Black, (4, 4));

        var act = () => factory.Update(texture, (3, 3), (2, 2), new byte[16]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Updateは指定領域のみ書き換える()
    {
        var factory = new FakeTextureFactory();
        var texture = factory.CreateSolid(Color.Transparent, (4, 4));
        var patch = new byte[2 * 2 * 4];
        for (var i = 3; i < patch.Length; i += 4)
            patch[i] = 255;

        factory.Update(texture, (1, 1), (2, 2), patch);

        factory.GetAlphaAt(texture.Handle, texture.Size, (1, 1)).Should().Be(255);
        factory.GetAlphaAt(texture.Handle, texture.Size, (2, 2)).Should().Be(255);
        factory.GetAlphaAt(texture.Handle, texture.Size, (0, 0)).Should().Be(0);
        factory.GetAlphaAt(texture.Handle, texture.Size, (3, 3)).Should().Be(0);
    }

    [Fact]
    public void スプライトシートは全セルが1枚のテクスチャを共有する()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);

        var cells = factory.LoadSpriteSheet(stream, 2, 2, (2, 2));

        cells.Should().HaveCount(4);
        factory.CreatedCount.Should().Be(1);
        cells.Select(c => c.Handle).Distinct().Should().HaveCount(1);
        cells[3].UvStart.Should().Be(new Vector(0.5f, 0.5f));
        cells[3].UvEnd.Should().Be(new Vector(1f, 1f));
    }

    [Fact]
    public void スプライトシートは全セルを破棄した時点で一度だけ解放される()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);
        var cells = factory.LoadSpriteSheet(stream, 2, 2, (2, 2));

        cells[0].Dispose();
        cells[1].Dispose();
        cells[2].Dispose();
        factory.DisposedHandles.Should().BeEmpty();

        cells[3].Dispose();
        factory.DisposedHandles.Should().Equal(cells[0].Handle);
    }

    [Fact]
    public void スプライトシートが画像からはみ出す場合はアップロード前に例外になる()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);

        var act = () => factory.LoadSpriteSheet(stream, 3, 1, (2, 2));

        act.Should().Throw<ArgumentException>();
        factory.CreatedCount.Should().Be(0);
    }

    [Fact]
    public void オプションを省略すると既定の設定で生成される()
    {
        var factory = new FakeTextureFactory();

        var texture = factory.CreateSolid(Color.Red, (2, 2));

        factory.GetOptions(texture.Handle).Should().Be(TextureOptions.Default);
        TextureOptions.Default.Filter.Should().Be(TextureFilterMode.Nearest);
        TextureOptions.Default.Address.Should().Be(TextureAddressMode.Clamp);
    }

    [Fact]
    public void 指定したオプションがバックエンドに渡される()
    {
        var factory = new FakeTextureFactory();
        var options = new TextureOptions(TextureFilterMode.Linear, TextureAddressMode.Repeat);

        var solid = factory.CreateSolid(Color.Red, (2, 2), options);
        using var stream = CreatePng(2, 2);
        var loaded = factory.Load(stream, options);

        factory.GetOptions(solid.Handle).Should().Be(options);
        factory.GetOptions(loaded.Handle).Should().Be(options);
    }

    [Fact]
    public void スプライトシートと9スライスにもオプションが渡される()
    {
        var factory = new FakeTextureFactory();
        var options = new TextureOptions(TextureFilterMode.Linear, TextureAddressMode.Mirror);
        using var sheet = CreatePng(4, 4);
        using var nine = CreatePng(6, 6);

        var cells = factory.LoadSpriteSheet(sheet, 2, 2, (2, 2), options);
        var sliced = factory.Load9Sliced(nine, 2, 2, 2, 2, options);

        factory.GetOptions(cells[0].Handle).Should().Be(options);
        factory.CreatedCount.Should().Be(1 + 9);
        for (var handle = 2; handle <= factory.CreatedCount; handle++)
            factory.GetOptions(handle).Should().Be(options);
        sliced.Should().NotBeNull();
    }

    [Fact]
    public void スプライトシートのセルはサブテクスチャとして判定される()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);

        var cells = factory.LoadSpriteSheet(stream, 2, 2, (2, 2));
        var whole = factory.CreateSolid(Color.Red, (2, 2));

        cells.Should().OnlyContain(c => c.IsSubTexture);
        whole.IsSubTexture.Should().BeFalse();
    }

    [Fact]
    public void サブテクスチャへのUpdateは例外になる()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);
        var cells = factory.LoadSpriteSheet(stream, 2, 2, (2, 2));

        var act = () => factory.Update(cells[1], (0, 0), (1, 1), new byte[4]);

        act.Should().Throw<InvalidOperationException>();
    }

    private static MemoryStream CreatePng(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = 255;

        var stream = new MemoryStream();
        PngEncoder.Encode(new RgbaImage(width, height, pixels), stream);
        stream.Position = 0;
        return stream;
    }
}
