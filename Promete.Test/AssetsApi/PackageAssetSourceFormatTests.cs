using System.Text;
using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="PackageAssetSource"/> が形式のバージョン・フラグ・壊れたパッケージをどう扱うかのテスト。
/// 壊れたものは <see cref="InvalidDataException"/>、知らないバージョンや機能は <see cref="NotSupportedException"/>。
/// </summary>
public class PackageAssetSourceFormatTests
{
    private static RawPackageBuilder Valid() =>
        new RawPackageBuilder()
            .Stored("a.txt", "A")
            .Deflated("b.txt", RawPackageLayout.CompressibleText());

    private static PackageAssetSource Open(byte[] bytes) => new(new MemoryStream(bytes, false));

    private static Action OpenAction(byte[] bytes) => () => Open(bytes).Dispose();

    [Fact]
    public void ValidPackage_ShouldOpen()
    {
        using var source = Open(Valid().Build());

        source.Exists("a.txt").Should().BeTrue();
    }

    [Fact]
    public void NewerMinorVersion_WithExtendedHeaderEntriesAndToc_ShouldBeAccepted()
    {
        var builder = new RawPackageBuilder
        {
            Minor = 7,
            HeaderSize = 96,
            HeaderExtra = Enumerable.Repeat((byte)0xAB, 32).ToArray(),
            TocTrailing = [1, 2, 3, 4, 5],
        }
            .Stored("a.txt", "A", e => e.Extra = [9, 9, 9])
            .Deflated("b.txt", RawPackageLayout.CompressibleText(), e => e.Extra = [8]);

        using var source = Open(builder.Build());

        using (var a = source.OpenRead("a.txt"))
            a.ReadAllText().Should().Be("A");
        using (var b = source.OpenRead("b.txt"))
            PackageAssetSourceTests
                .ReadAllBytes(b)
                .Should()
                .Equal(RawPackageLayout.CompressibleText());
    }

    [Fact]
    public void NonZeroNonceOnUnencryptedEntry_ShouldBeIgnored()
    {
        var builder = new RawPackageBuilder().Stored(
            "a.txt",
            "A",
            e => e.Nonce = Enumerable.Range(1, 12).Select(i => (byte)i).ToArray()
        );

        using var source = Open(builder.Build());

        using var stream = source.OpenRead("a.txt");
        stream.ReadAllText().Should().Be("A");
    }

    [Theory]
    [InlineData((ushort)2, (ushort)0, "2.0")]
    [InlineData((ushort)0, (ushort)9, "0.9")]
    [InlineData((ushort)65535, (ushort)1, "65535.1")]
    public void UnknownMajorVersion_ShouldThrowNotSupportedWithVersion(
        ushort major,
        ushort minor,
        string text
    )
    {
        var builder = Valid();
        builder.Major = major;
        builder.Minor = minor;

        var act = OpenAction(builder.Build());

        act.Should().Throw<NotSupportedException>().WithMessage($"*{text}*");
    }

    [Fact]
    public void BadMagic_ShouldThrowInvalidData()
    {
        var builder = Valid();
        builder.Magic = Encoding.ASCII.GetBytes("PK\u0003\u0004zip!");

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(63)]
    public void ShorterThanHeader_ShouldThrowInvalidData(int length)
    {
        var bytes = Valid().Build()[..length];

        OpenAction(bytes).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void HeaderSizeSmallerThan64_ShouldThrowInvalidData()
    {
        var builder = Valid();
        builder.HeaderSize = 32;

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void TocEncryptedFlag_ShouldThrowNotSupported()
    {
        var builder = Valid();
        builder.Flags = RawPackageLayout.HeaderFlagTocEncrypted;

        OpenAction(builder.Build()).Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(1u << 1)]
    [InlineData(1u << 31)]
    public void UnknownHeaderFlag_ShouldThrowNotSupported(uint flag)
    {
        var builder = Valid();
        builder.Flags = flag;

        OpenAction(builder.Build()).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void EncryptedEntryFlag_ShouldThrowNotSupported()
    {
        var builder = new RawPackageBuilder().Stored(
            "a.txt",
            "A",
            e => e.Flags = RawPackageLayout.EntryFlagEncrypted
        );

        OpenAction(builder.Build()).Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(1u << 2)]
    [InlineData(1u << 31)]
    public void UnknownEntryFlag_ShouldThrowNotSupported(uint flag)
    {
        var builder = new RawPackageBuilder().Stored("a.txt", "A", e => e.Flags = flag);

        OpenAction(builder.Build()).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void TocBeyondEnd_ShouldThrowInvalidData()
    {
        var builder = Valid();
        builder.TocSizeOverride = 100000;

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void TocOffsetInsideHeader_ShouldThrowInvalidData()
    {
        var builder = Valid();
        builder.TocOffsetOverride = 10;

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void TocOffsetHuge_ShouldThrowInvalidData()
    {
        var builder = Valid();
        builder.TocOffsetOverride = ulong.MaxValue - 2;
        builder.TocSizeOverride = 10;

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void TruncatedFile_ShouldThrowInvalidData()
    {
        var bytes = Valid().Build();

        OpenAction(bytes[..^3]).Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData(3u)]
    [InlineData(uint.MaxValue)]
    public void EntryCountLargerThanToc_ShouldThrowInvalidData(uint count)
    {
        var builder = Valid();
        builder.EntryCountOverride = count;

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void EntryDataBeyondEnd_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Stored(
            "a.txt",
            "A",
            e => e.OffsetOverride = 1_000_000
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void EntrySizeOverflowing_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Stored(
            "a.txt",
            "A",
            e =>
            {
                e.StoredSizeOverride = ulong.MaxValue;
                e.OriginalSize = ulong.MaxValue;
            }
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void EntryDataInsideHeader_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Stored("a.txt", "A", e => e.OffsetOverride = 0);

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void StoredEntryWithDifferentOriginalSize_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Stored("a.txt", "AAAA", e => e.OriginalSize = 3);

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void DeflatedEntryWithHugeOriginalSize_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Deflated(
            "a.txt",
            "AAAA",
            e => e.OriginalSize = 1UL << 40
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/a.txt")]
    [InlineData("a/../b.txt")]
    [InlineData("a//b.txt")]
    [InlineData("./a.txt")]
    [InlineData("a\\b.txt")]
    [InlineData("\u304B\u3099.txt")]
    public void NonNormalizedPath_ShouldThrowInvalidData(string path)
    {
        var builder = new RawPackageBuilder().Stored(path, "A");

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void InvalidUtf8Path_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Stored(
            "a.txt",
            "A",
            e => e.PathBytes = [0x61, 0xFF, 0xFE]
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void DuplicatePath_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder().Stored("a.txt", "A").Stored("a.txt", "B");

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void PathsDifferingOnlyInCase_ShouldBothBeReadable()
    {
        var builder = new RawPackageBuilder().Stored("A.txt", "upper").Stored("a.txt", "lower");

        using var source = Open(builder.Build());

        using (var upper = source.OpenRead("A.txt"))
            upper.ReadAllText().Should().Be("upper");
        using (var lower = source.OpenRead("a.txt"))
            lower.ReadAllText().Should().Be("lower");
    }

    [Fact]
    public void CorruptDeflateData_ShouldThrowInvalidDataOnOpenRead()
    {
        var builder = new RawPackageBuilder().Deflated(
            "a.txt",
            "AAAA",
            e => e.Stored = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]
        );
        using var source = Open(builder.Build());

        var act = () => source.OpenRead("a.txt").Dispose();

        act.Should().Throw<InvalidDataException>();
    }

    // 実際 (4600 バイト) より大きい・小さい場合。大きい方は、開く時点の圧縮率の上限 (storedSize * 1032 + 64) に収まる値にする
    [Theory]
    [InlineData(6000UL)]
    [InlineData(2UL)]
    public void DeflatedSizeMismatch_ShouldThrowInvalidDataOnOpenRead(ulong declared)
    {
        var builder = new RawPackageBuilder().Deflated(
            "a.txt",
            RawPackageLayout.CompressibleText(),
            e => e.OriginalSize = declared
        );
        using var source = Open(builder.Build());

        var act = () => source.OpenRead("a.txt").Dispose();

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void FailedConstruction_ShouldCloseOwnedStream()
    {
        var builder = Valid();
        builder.Magic = new byte[8];
        var stream = new ProbeStream(builder.Build());

        var act = () => new PackageAssetSource(stream);

        act.Should().Throw<InvalidDataException>();
        stream.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void FailedConstruction_WithLeaveOpen_ShouldNotCloseStream()
    {
        var builder = Valid();
        builder.Major = 9;
        var stream = new ProbeStream(builder.Build());

        var act = () => new PackageAssetSource(stream, leaveOpen: true);

        act.Should().Throw<NotSupportedException>();
        stream.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void DeflatedEntryWithOriginalSizeAtArrayMaxLength_ShouldThrowInvalidDataOnOpen()
    {
        // 129 バイトほどの壊れたパッケージで 2 GiB を確保させないよう、開く時点 (OpenRead より前) で拒否する
        var builder = new RawPackageBuilder().Deflated(
            "a.txt",
            "AAAA",
            e => e.OriginalSize = (ulong)Array.MaxLength
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void DeflatedRatioLimit_ShouldBe1032TimesStoredSizePlus64()
    {
        var stored = (ulong)RawPackageLayout.Deflate("AAAA"u8.ToArray()).Length;
        var limit = stored * 1032 + 64;
        var atLimit = new RawPackageBuilder().Deflated(
            "a.txt",
            "AAAA",
            e => e.OriginalSize = limit
        );
        var overLimit = new RawPackageBuilder().Deflated(
            "a.txt",
            "AAAA",
            e => e.OriginalSize = limit + 1
        );

        OpenAction(atLimit.Build())
            .Should()
            .NotThrow("上限ちょうどは開ける (中身の破損は OpenRead で分かる)");
        OpenAction(overLimit.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void DeflatedRatioLimit_ShouldNotOverflowWithHugeStoredSize()
    {
        // storedSize * 1032 が ulong で桁あふれしても、範囲外のデータとして InvalidData になる
        var builder = new RawPackageBuilder().Deflated(
            "a.txt",
            "AAAA",
            e =>
            {
                e.StoredSizeOverride = ulong.MaxValue / 1000;
                e.OriginalSize = 10;
            }
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void HighlyCompressibleEntry_ShouldStillOpenAndRead()
    {
        var zeros = new byte[1024 * 1024];
        var builder = new RawPackageBuilder().Deflated("zeros.bin", zeros);

        using var source = Open(builder.Build());

        using var stream = source.OpenRead("zeros.bin");
        stream.Length.Should().Be(zeros.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void StreamPositionedAtOrBeyondEnd_ShouldThrowInvalidData(int beyond)
    {
        var bytes = Valid().Build();
        var stream = new MemoryStream(bytes, false);
        stream.Position = bytes.Length + beyond;

        var act = () => new PackageAssetSource(stream).Dispose();

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void HeaderSizeLargerThanPackage_ShouldThrowInvalidData()
    {
        var bytes = Valid().Build();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(12),
            (uint)bytes.Length + 1
        );

        OpenAction(bytes).Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("count")]
    [InlineData("path")]
    [InlineData("fixed")]
    [InlineData("extra")]
    public void TocCutInTheMiddleOfAnEntry_ShouldThrowInvalidData(string cutInside)
    {
        // 件数の事前検査 (1 件あたり 44 バイト以上) は通るが、エントリを読む途中で目次が尽きる
        var builder = new RawPackageBuilder().Stored(
            new string('p', 100),
            "A",
            e => e.Extra = new byte[20]
        );
        var tocSize = RawPackage.Parse(builder.Build()).TocSize;
        builder.TocSizeOverride = cutInside switch
        {
            "count" => 2,
            "path" => 4 + 2 + 50,
            "fixed" => 4 + 2 + 100 + 10,
            _ => tocSize - 3,
        };

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ExtendedHeader_EntryOffsetInsideExtendedHeader_ShouldThrowInvalidData()
    {
        // headerSize を延ばしたパッケージでは、64 ではなく headerSize と比べる
        var builder = new RawPackageBuilder { HeaderSize = 96 }.Stored(
            "a.txt",
            "A",
            e => e.OffsetOverride = 80
        );

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ExtendedHeader_TocOffsetInsideExtendedHeader_ShouldThrowInvalidData()
    {
        var builder = new RawPackageBuilder { HeaderSize = 96 };
        builder.TocOffsetOverride = 80;
        builder.TocSizeOverride = 4;

        OpenAction(builder.Build()).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ExtendedHeader_EntryRightAfterHeader_ShouldOpen()
    {
        var builder = new RawPackageBuilder { HeaderSize = 96 }.Stored("a.txt", "A");

        using var source = Open(builder.Build());

        using var stream = source.OpenRead("a.txt");
        stream.ReadAllText().Should().Be("A");
    }
}
