using System.Text;
using FluentAssertions;
using Promete.AssetPacker;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetPackageWriter.WriteTo(Stream)"/> が書き出すバイト列が、形式の仕様 (PACKAGE_FORMAT.md) どおりであることのテスト。
/// テスト側で独立に実装したパーサー (<see cref="RawPackage"/>) で検査する。
/// </summary>
public sealed class AssetPackageWriterFormatTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Empty_ShouldWriteHeaderAndEmptyToc()
    {
        var bytes = Write(new AssetPackageWriter());

        var package = RawPackage.Parse(bytes);
        package.Entries.Should().BeEmpty();
        package.TocOffset.Should().Be(64);
        package.TocSize.Should().Be(4);
        bytes.Length.Should().Be(68);
    }

    [Fact]
    public void Header_ShouldHaveMagicVersionAndReservedFields()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", RawPackageLayout.CompressibleText());

        var package = RawPackage.Parse(Write(writer));

        package.Magic.Should().Equal(0x50, 0x52, 0x4D, 0x54, 0x50, 0x41, 0x4B, 0x1A);
        Encoding.ASCII.GetString(package.Magic, 0, 7).Should().Be("PRMTPAK");
        package.Major.Should().Be(1);
        package.Minor.Should().Be(0);
        package.HeaderSize.Should().Be(64);
        package.Flags.Should().Be(0, "暗号化しないパッケージのフラグは 0");
        package.Reserved20.Should().Be(0);
        package.TocNonce.Should().Equal(new byte[12], "暗号化しないパッケージの目次の nonce は 0");
        package.Reserved52.Should().Equal(new byte[12]);
    }

    [Fact]
    public void Toc_ShouldBeAtTheEndOfThePackage()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", [1, 2, 3]);
        writer.Add("b.bin", RawPackageLayout.RandomBytes(100));

        var bytes = Write(writer);
        var package = RawPackage.Parse(bytes);

        ((long)(package.TocOffset + package.TocSize)).Should().Be(bytes.Length);
        package.TocTrailingLength.Should().Be(0);
    }

    [Fact]
    public void Entries_ShouldBeSortedByOrdinalPathRegardlessOfAddOrder()
    {
        var writer = new AssetPackageWriter();
        writer.Add("b.txt", [2]);
        writer.Add("a/z.txt", [3]);
        writer.Add("B.txt", [4]);
        writer.Add("a.txt", [1]);
        writer.Add("あ.txt", [5]);

        var package = RawPackage.Parse(Write(writer));

        package
            .Entries.Select(e => e.Path)
            .Should()
            .Equal("B.txt", "a.txt", "a/z.txt", "b.txt", "あ.txt");
    }

    [Fact]
    public void DataSection_ShouldBePackedContiguouslyBetweenHeaderAndToc()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.bin", RawPackageLayout.RandomBytes(10, 1));
        writer.Add("b.txt", RawPackageLayout.CompressibleText());
        writer.Add("c.bin", []);
        writer.Add("d.png", RawPackageLayout.RandomBytes(33, 2));

        var package = RawPackage.Parse(Write(writer));

        var expectedOffset = 64UL;
        foreach (var entry in package.Entries)
        {
            entry
                .Offset.Should()
                .Be(expectedOffset, $"'{entry.Path}' は前のエントリの直後に置かれるはず");
            expectedOffset += entry.StoredSize;
        }

        expectedOffset.Should().Be(package.TocOffset);
    }

    [Fact]
    public void Entries_ShouldHaveZeroNonceAndNoExtra()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", RawPackageLayout.CompressibleText());
        writer.Add("b.png", [1, 2, 3]);

        var package = RawPackage.Parse(Write(writer));

        foreach (var entry in package.Entries)
        {
            entry.Nonce.Should().Equal(new byte[12]);
            entry.Extra.Should().BeEmpty();
            (entry.Flags & RawPackageLayout.EntryFlagEncrypted).Should().Be(0);
            (entry.Flags & ~RawPackageLayout.EntryFlagDeflate)
                .Should()
                .Be(0, "1.0 で使うフラグは Deflate だけ");
        }
    }

    [Fact]
    public void Entry_Stored_ShouldKeepRawBytes()
    {
        var content = RawPackageLayout.RandomBytes(1000);
        var writer = new AssetPackageWriter();
        writer.Add("raw.bin", content, AssetCompression.Never);

        var entry = RawPackage.Parse(Write(writer)).Entry("raw.bin");

        entry.IsDeflated.Should().BeFalse();
        entry.StoredSize.Should().Be(1000);
        entry.OriginalSize.Should().Be(1000);
        entry.StoredData.Should().Equal(content);
    }

    [Fact]
    public void Entry_Deflated_ShouldBeRawDeflateOfContent()
    {
        var content = RawPackageLayout.CompressibleText();
        var writer = new AssetPackageWriter();
        writer.Add("text.txt", content, AssetCompression.Always);

        var entry = RawPackage.Parse(Write(writer)).Entry("text.txt");

        entry.IsDeflated.Should().BeTrue();
        entry.OriginalSize.Should().Be((ulong)content.Length);
        entry.StoredSize.Should().Be((ulong)entry.StoredData.Length);
        entry.StoredSize.Should().BeLessThan((ulong)content.Length);
        RawPackageLayout.Inflate(entry.StoredData).Should().Equal(content);
    }

    [Theory]
    [InlineData("image.png")]
    [InlineData("bgm.ogg")]
    [InlineData("se.wav")]
    [InlineData("photo.JPG")]
    public void Auto_KnownFormats_ShouldBeStoredEvenIfCompressible(string path)
    {
        var content = RawPackageLayout.CompressibleText();
        var writer = new AssetPackageWriter();
        writer.Add(path, content);

        var entry = RawPackage.Parse(Write(writer)).Entry(path);

        entry.IsDeflated.Should().BeFalse();
        entry.StoredData.Should().Equal(content);
    }

    [Theory]
    [InlineData("data.json")]
    [InlineData("font.ttf")]
    [InlineData("noextension")]
    public void Auto_OtherFormats_ShouldBeDeflatedWhenSmaller(string path)
    {
        var content = RawPackageLayout.CompressibleText();
        var writer = new AssetPackageWriter();
        writer.Add(path, content);

        var entry = RawPackage.Parse(Write(writer)).Entry(path);

        entry.IsDeflated.Should().BeTrue();
        entry.Content.Should().Equal(content);
    }

    [Fact]
    public void Auto_WhenDeflateDoesNotShrink_ShouldBeStored()
    {
        var random = RawPackageLayout.RandomBytes(4096);
        var writer = new AssetPackageWriter();
        writer.Add("random.bin", random);
        writer.Add("tiny.txt", [(byte)'a']);
        writer.Add("empty.txt", []);

        var package = RawPackage.Parse(Write(writer));

        package.Entry("random.bin").IsDeflated.Should().BeFalse();
        package.Entry("random.bin").StoredData.Should().Equal(random);
        package.Entry("tiny.txt").IsDeflated.Should().BeFalse();
        package.Entry("empty.txt").IsDeflated.Should().BeFalse();
        package.Entry("empty.txt").StoredSize.Should().Be(0);
    }

    [Fact]
    public void Always_ShouldDeflateEvenKnownFormatsAndIncompressibleData()
    {
        var random = RawPackageLayout.RandomBytes(4096);
        var writer = new AssetPackageWriter();
        writer.Add("image.png", random, AssetCompression.Always);
        writer.Add("empty.txt", [], AssetCompression.Always);

        var package = RawPackage.Parse(Write(writer));

        package.Entry("image.png").IsDeflated.Should().BeTrue();
        package.Entry("image.png").Content.Should().Equal(random);
        package.Entry("empty.txt").IsDeflated.Should().BeTrue();
        package.Entry("empty.txt").OriginalSize.Should().Be(0);
        package.Entry("empty.txt").Content.Should().BeEmpty();
    }

    [Fact]
    public void Never_ShouldStoreEvenCompressibleText()
    {
        var content = RawPackageLayout.CompressibleText();
        var writer = new AssetPackageWriter();
        writer.Add("text.txt", content, AssetCompression.Never);

        var entry = RawPackage.Parse(Write(writer)).Entry("text.txt");

        entry.IsDeflated.Should().BeFalse();
        entry.StoredData.Should().Equal(content);
    }

    [Fact]
    public void Paths_ShouldBeStoredNormalizedAsNfcUtf8()
    {
        var writer = new AssetPackageWriter();
        writer.Add("/dir/./sub/../\u304B\u3099.txt", [1]);

        var entry = RawPackage.Parse(Write(writer)).Entries.Single();

        entry.Path.Should().Be("dir/が.txt");
        entry.PathBytes.Should().Equal(Encoding.UTF8.GetBytes("dir/が.txt"));
    }

    [Fact]
    public void AddFile_ShouldStoreFileContentReadAtWriteTime()
    {
        var file = _dir.CreateFile("src/a.txt", "before");
        var writer = new AssetPackageWriter();
        writer.AddFile("a.txt", file, AssetCompression.Never);
        File.WriteAllText(file, "after");

        var entry = RawPackage.Parse(Write(writer)).Entry("a.txt");

        Encoding.UTF8.GetString(entry.Content).Should().Be("after");
    }

    [Fact]
    public void AddFile_WithAuto_ShouldDecideByLogicalPathExtension()
    {
        var text = RawPackageLayout.CompressibleText();
        var source = Path.Combine(_dir.Path, "source.txt");
        File.WriteAllBytes(source, text);
        var writer = new AssetPackageWriter();
        writer.AddFile("as-image.png", source);
        writer.AddFile("as-text.txt", source);

        var package = RawPackage.Parse(Write(writer));

        package.Entry("as-image.png").IsDeflated.Should().BeFalse();
        package.Entry("as-text.txt").IsDeflated.Should().BeTrue();
        package.Entry("as-text.txt").Content.Should().Equal(text);
    }

    [Fact]
    public void WriteTo_ShouldBeDeterministicRegardlessOfAddOrder()
    {
        var first = new AssetPackageWriter();
        first.Add("a.txt", RawPackageLayout.CompressibleText("a"));
        first.Add("b/c.png", RawPackageLayout.RandomBytes(50));
        first.Add("z.bin", [9, 9, 9], AssetCompression.Always);
        var second = new AssetPackageWriter();
        second.Add("z.bin", [9, 9, 9], AssetCompression.Always);
        second.Add("b/c.png", RawPackageLayout.RandomBytes(50));
        second.Add("a.txt", RawPackageLayout.CompressibleText("a"));

        Write(first).Should().Equal(Write(second));
    }

    [Fact]
    public void WriteTo_Twice_ShouldProduceSameBytes()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", RawPackageLayout.CompressibleText());
        writer.Add("b.bin", [1, 2, 3]);

        Write(writer).Should().Equal(Write(writer));
    }

    [Fact]
    public void WriteTo_ShouldStartAtCurrentPositionAndLeaveStreamOpenAtEnd()
    {
        var prefix = new byte[] { 0xEE, 0xEE, 0xEE, 0xEE, 0xEE };
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", RawPackageLayout.CompressibleText());
        writer.Add("b.bin", [1, 2, 3], AssetCompression.Never);
        using var output = new MemoryStream();
        output.Write(prefix);

        writer.WriteTo(output);

        output.CanWrite.Should().BeTrue("ストリームは閉じないはず");
        output.Position.Should().Be(output.Length);
        var bytes = output.ToArray();
        bytes[..5].Should().Equal(prefix);
        var package = RawPackage.Parse(bytes, 5);
        package
            .Entries.First()
            .Offset.Should()
            .Be(64, "オフセットは書き出し始めた位置からの相対値");
        package.Entry("b.bin").StoredData.Should().Equal(1, 2, 3);
        ((long)(package.TocOffset + package.TocSize)).Should().Be(bytes.Length - 5);
    }

    [Fact]
    public void WriteTo_NonSeekableStream_ShouldThrowArgumentException()
    {
        var writer = new AssetPackageWriter();
        using var output = new NonSeekableWritableStream();

        var act = () => writer.WriteTo(output);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WriteTo_ReadOnlyStream_ShouldThrowArgumentException()
    {
        var writer = new AssetPackageWriter();
        using var output = new MemoryStream(new byte[100], false);

        var act = () => writer.WriteTo(output);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WriteTo_NullStream_ShouldThrowArgumentNull()
    {
        var act = () => new AssetPackageWriter().WriteTo((Stream)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteTo_WhenAddedFileIsDeleted_ShouldThrowFileNotFound()
    {
        var file = _dir.CreateFile("gone.txt", "x");
        var writer = new AssetPackageWriter();
        writer.AddFile("gone.txt", file);
        File.Delete(file);

        var act = () => writer.WriteTo(new MemoryStream());

        act.Should().Throw<FileNotFoundException>();
    }

    private static byte[] Write(AssetPackageWriter writer)
    {
        using var output = new MemoryStream();
        writer.WriteTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// 書き込みはできるが seek できないストリーム。
    /// </summary>
    private sealed class NonSeekableWritableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}
