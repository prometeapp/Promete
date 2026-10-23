using FluentAssertions;
using Promete.Audio;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="IAudioDecoder"/> の標準の実装と、DI への登録のテスト。
/// </summary>
public sealed class AudioDecoderTests
{
    [Fact]
    public void WaveDecoder_CanDecode_ShouldCheckRiffWaveMagic()
    {
        var decoder = new WaveAudioDecoder();
        var wav = TestAssetData.Wav();
        var ogg = TestAssetData.File("amaebi.ogg");

        decoder.CanDecode("se.wav", wav.AsSpan(0, 64)).Should().BeTrue();
        decoder
            .CanDecode("se.ogg", wav.AsSpan(0, 12))
            .Should()
            .BeTrue("拡張子ではなくマジックで判定するはず");
        decoder.CanDecode("bgm.wav", ogg.AsSpan(0, 64)).Should().BeFalse();
        decoder
            .CanDecode("x.wav", "RIFF\0\0\0\0AVI "u8)
            .Should()
            .BeFalse("RIFF でも WAVE でなければ対象外");
        decoder.CanDecode("x.wav", "RIFF"u8).Should().BeFalse("ヘッダが短すぎる場合は判定できない");
        decoder.CanDecode("x.wav", ReadOnlySpan<byte>.Empty).Should().BeFalse();
    }

    [Fact]
    public void VorbisDecoder_CanDecode_ShouldCheckOggSMagic()
    {
        var decoder = new VorbisAudioDecoder();
        var wav = TestAssetData.Wav();
        var ogg = TestAssetData.File("amaebi.ogg");

        decoder.CanDecode("bgm.ogg", ogg.AsSpan(0, 64)).Should().BeTrue();
        decoder
            .CanDecode("bgm.bin", ogg.AsSpan(0, 4))
            .Should()
            .BeTrue("拡張子ではなくマジックで判定するはず");
        decoder.CanDecode("se.ogg", wav.AsSpan(0, 64)).Should().BeFalse();
        decoder.CanDecode("x.ogg", "Ogg"u8).Should().BeFalse();
        decoder.CanDecode("x.ogg", ReadOnlySpan<byte>.Empty).Should().BeFalse();
    }

    [Fact]
    public void WaveDecoder_Decode_ShouldReturnWaveSourceAndCloseStream()
    {
        var stream = new TrackedStream(
            "se.wav",
            TestAssetData.Wav(channels: 2, sampleRate: 22050, frames: 10)
        );

        var source = new WaveAudioDecoder().Decode(stream);

        source.Should().BeOfType<WaveAudioSource>();
        source.Channels.Should().Be(2);
        source.SampleRate.Should().Be(22050);
        source.Frames.Should().Be(10);
        stream.IsDisposed.Should().BeTrue("WAVE はデコードの完了時に閉じるはず");
    }

    [Fact]
    public void VorbisDecoder_Decode_ShouldReturnVorbisSource()
    {
        var stream = new TrackedStream("bgm.ogg", TestAssetData.File("amaebi.ogg"));

        var source = new VorbisAudioDecoder().Decode(stream);

        try
        {
            source.Should().BeOfType<VorbisAudioSource>();
            source.Channels.Should().BeGreaterThan(0);
            source.SampleRate.Should().BeGreaterThan(0);
        }
        finally
        {
            ((IDisposable)source).Dispose();
        }
    }

    [Fact]
    public void Build_ShouldRegisterStandardDecoders()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        var decoders = app.GetPlugin<IEnumerable<IAudioDecoder>>();

        decoders
            .Select(d => d.GetType())
            .Should()
            .Equal(typeof(WaveAudioDecoder), typeof(VorbisAudioDecoder));
    }

    [Fact]
    public void Build_UserDecoders_ShouldComeBeforeStandardDecodersInRegistrationOrder()
    {
        using var app = PrometeApp
            .Create()
            .Use<IAudioDecoder, FirstUserDecoder>()
            .Use<IAudioDecoder, SecondUserDecoder>()
            .BuildWithHeadless();

        var decoders = app.GetPlugin<IEnumerable<IAudioDecoder>>();

        decoders
            .Select(d => d.GetType())
            .Should()
            .Equal(
                typeof(FirstUserDecoder),
                typeof(SecondUserDecoder),
                typeof(WaveAudioDecoder),
                typeof(VorbisAudioDecoder)
            );
    }

    private sealed class FirstUserDecoder : IAudioDecoder
    {
        public bool CanDecode(string path, ReadOnlySpan<byte> header) => false;

        public IAudioSource Decode(Stream stream) => throw new NotSupportedException();
    }

    private sealed class SecondUserDecoder : IAudioDecoder
    {
        public bool CanDecode(string path, ReadOnlySpan<byte> header) => false;

        public IAudioSource Decode(Stream stream) => throw new NotSupportedException();
    }
}
