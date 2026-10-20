using FluentAssertions;
using Promete.Assets;
using Promete.Audio;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetManager.LoadAudio(string, AssetScope)"/> のテスト。デコーダーの選択とストリームの所有権。
/// </summary>
public sealed class AssetManagerAudioTests
{
    private static readonly byte[] Wav = TestAssetData.Wav(
        channels: 2,
        sampleRate: 22050,
        frames: 16
    );

    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("sfx/se.wav", Wav)
        .Add("bgm/amaebi.ogg", TestAssetData.File("amaebi.ogg"))
        .Add("misc/tiny.bin", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
        .Add("misc/data.custom", TestAssetData.Text("custom-audio"));

    public AssetManagerAudioTests()
    {
        CustomDecoder.Reset();
    }

    [Fact]
    public void LoadAudio_Wav_ShouldUseWaveDecoder()
    {
        using var app = AssetsTestApp.Build(_source);

        var audio = app.Assets().LoadAudio("sfx/se.wav", AssetScope.Global);

        audio.Should().BeOfType<WaveAudioSource>();
        audio.Channels.Should().Be(2);
        audio.SampleRate.Should().Be(22050);
        audio.Frames.Should().Be(16);
        _source.StreamsOf("sfx/se.wav").Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void LoadAudio_Ogg_ShouldUseVorbisDecoder()
    {
        using var app = AssetsTestApp.Build(_source);

        var audio = app.Assets().LoadAudio("bgm/amaebi.ogg", AssetScope.Global);

        audio.Should().BeOfType<VorbisAudioSource>();
        audio.Channels.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadAudio_SameKey_ShouldShareInstance()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        var first = assets.LoadAudio("sfx/se.wav", AssetScope.Global);
        var second = assets.LoadAudio("./sfx/se.wav", AssetScope.Global);

        second.Should().BeSameAs(first);
        _source.OpenCount("sfx/se.wav").Should().Be(1);
    }

    [Fact]
    public void LoadAudio_UserDecoder_ShouldBeCheckedBeforeStandardDecoders()
    {
        CustomDecoder.Accept = (_, _) => true;
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        var audio = app.Assets().LoadAudio("sfx/se.wav", AssetScope.Global);

        audio
            .Should()
            .BeOfType<FakeAudioSource>("利用者のデコーダーが標準の wav より先に判定されるはず");
    }

    [Fact]
    public void LoadAudio_UserDecoderDeclining_ShouldFallBackToStandardDecoders()
    {
        CustomDecoder.Accept = (_, _) => false;
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        var audio = app.Assets().LoadAudio("sfx/se.wav", AssetScope.Global);

        audio.Should().BeOfType<WaveAudioSource>();
        CustomDecoder.CanDecodeCalls.Should().ContainSingle();
    }

    [Fact]
    public void CanDecode_ShouldReceiveNormalizedPathAndHeader()
    {
        CustomDecoder.Accept = (_, _) => false;
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        app.Assets().LoadAudio("./sfx/x/../se.wav", AssetScope.Global);

        var (path, header) = CustomDecoder.CanDecodeCalls.Should().ContainSingle().Subject;
        path.Should().Be("sfx/se.wav");
        header.Should().Equal(Wav.Take(64), "先頭から最大 64 バイトを渡すはず");
    }

    [Fact]
    public void CanDecode_ShortFile_ShouldReceiveWholeFileAsHeader()
    {
        CustomDecoder.Accept = (path, _) => path.EndsWith(".bin", StringComparison.Ordinal);
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        app.Assets().LoadAudio("misc/tiny.bin", AssetScope.Global);

        CustomDecoder
            .CanDecodeCalls.Should()
            .ContainSingle()
            .Which.Header.Should()
            .Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Fact]
    public void Decode_ShouldReceiveStreamAtStartWithWholeContent()
    {
        CustomDecoder.Accept = (path, _) => path.EndsWith(".custom", StringComparison.Ordinal);
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        var audio = (FakeAudioSource)app.Assets().LoadAudio("misc/data.custom", AssetScope.Global);

        audio
            .Data.Should()
            .Equal(TestAssetData.Text("custom-audio"), "判定のために読んだ分を戻してから渡すはず");
    }

    [Fact]
    public void LoadAudio_ShouldNotCloseStreamHandedToDecoder()
    {
        CustomDecoder.Accept = (_, _) => true;
        CustomDecoder.KeepStreamOpen = true;
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        var audio = (FakeAudioSource)app.Assets().LoadAudio("misc/data.custom", AssetScope.Global);

        _source
            .StreamsOf("misc/data.custom")
            .Should()
            .ContainSingle()
            .Which.IsDisposed.Should()
            .BeFalse("閉じる責任はデコーダーにあるはず");
        audio.Stream!.Dispose();
    }

    [Fact]
    public void LoadAudio_NoDecoder_ShouldThrowNotSupportedCloseStreamAndNotCache()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        var act = () => assets.LoadAudio("misc/tiny.bin", AssetScope.Global);

        act.Should().Throw<NotSupportedException>().WithMessage("*misc/tiny.bin*");
        act.Should().Throw<NotSupportedException>();
        _source.OpenCount("misc/tiny.bin").Should().Be(2);
        _source.StreamsOf("misc/tiny.bin").Should().AllSatisfy(s => s.IsDisposed.Should().BeTrue());
    }

    [Fact]
    public void LoadAudio_DecoderThrows_ShouldPropagateAndCloseStream()
    {
        CustomDecoder.Accept = (_, _) => true;
        CustomDecoder.Throw = true;
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());

        var act = () => app.Assets().LoadAudio("misc/data.custom", AssetScope.Global);

        act.Should().Throw<InvalidDataException>();
        _source
            .StreamsOf("misc/data.custom")
            .Should()
            .ContainSingle()
            .Which.IsDisposed.Should()
            .BeTrue();
    }

    [Fact]
    public void LoadAudio_Release_ShouldDisposeDisposableSource()
    {
        CustomDecoder.Accept = (_, _) => true;
        using var app = AssetsTestApp.Build(_source, b => b.Use<IAudioDecoder, CustomDecoder>());
        var assets = app.Assets();
        var audio = (FakeAudioSource)assets.LoadAudio("misc/data.custom", AssetScope.Global);

        assets.Unload("misc/data.custom", AssetScope.Global);
        assets.Dispose();

        audio.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void LoadAudio_ReleaseNonDisposableSource_ShouldNotThrow()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        assets.LoadAudio("sfx/se.wav", AssetScope.Global);

        var act = () => assets.Unload("sfx/se.wav");

        act.Should().NotThrow();
    }

    /// <summary>
    /// 振る舞いを静的なプロパティで切り替えるデコーダー (DI が生成するため)。
    /// </summary>
    internal sealed class CustomDecoder : IAudioDecoder
    {
        public static Func<string, byte[], bool> Accept { get; set; } = (_, _) => false;

        public static bool KeepStreamOpen { get; set; }

        public static bool Throw { get; set; }

        public static List<(string Path, byte[] Header)> CanDecodeCalls { get; } = [];

        public static void Reset()
        {
            Accept = (_, _) => false;
            KeepStreamOpen = false;
            Throw = false;
            CanDecodeCalls.Clear();
        }

        public bool CanDecode(string path, ReadOnlySpan<byte> header)
        {
            var copy = header.ToArray();
            CanDecodeCalls.Add((path, copy));
            return Accept(path, copy);
        }

        public IAudioSource Decode(Stream stream)
        {
            if (Throw)
                throw new InvalidDataException("壊れた音声");

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            if (KeepStreamOpen)
                return new FakeAudioSource(buffer.ToArray(), stream);

            stream.Dispose();
            return new FakeAudioSource(buffer.ToArray(), null);
        }
    }

    /// <summary>
    /// 読んだデータと Dispose の回数を持つ、テスト用の音声。
    /// </summary>
    internal sealed class FakeAudioSource(byte[] data, Stream? stream) : IAudioSource, IDisposable
    {
        private int _disposeCount;

        public byte[] Data { get; } = data;

        public Stream? Stream { get; } = stream;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public int? Frames => 0;

        public int Channels => 1;

        public int SampleRate => 44100;

        public (int FilledFrames, bool IsFinished) FillSamples(
            Span<float> buffer,
            int offsetFrames
        ) => (0, true);

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }
}
