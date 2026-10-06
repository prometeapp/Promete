using FluentAssertions;
using Promete.Audio;

namespace Promete.Test;

public class CaptureAudioOutputTests
{
    [Fact]
    public void RenderNext_InvokesCallbackAndCapturesBuffer()
    {
        using var output = new CaptureAudioOutput();
        output.Start(FillIncrementing, channels: 1, sampleRate: 44100, bufferSizeInFrames: 4);

        output.RenderNext();

        output.CapturedBuffers.Should().HaveCount(1);
        output.CapturedBuffers[0].Should().Equal(0f, 1f, 2f, 3f);
    }

    [Fact]
    public void RenderNext_WithBufferCount_InvokesCallbackMultipleTimes()
    {
        using var output = new CaptureAudioOutput();
        output.Start(FillIncrementing, channels: 1, sampleRate: 44100, bufferSizeInFrames: 2);

        output.RenderNext(3);

        output.CapturedBuffers.Should().HaveCount(3);
        output.CapturedBuffers[0].Should().Equal(0f, 1f);
        output.CapturedBuffers[1].Should().Equal(0f, 1f);
        output.CapturedBuffers[2].Should().Equal(0f, 1f);
    }

    [Fact]
    public void GetAllSamples_ConcatenatesCapturedBuffersInOrder()
    {
        using var output = new CaptureAudioOutput();
        var counter = 0;
        output.Start(
            buffer =>
            {
                for (var i = 0; i < buffer.Length; i++)
                    buffer[i] = counter++;
            },
            channels: 1,
            sampleRate: 44100,
            bufferSizeInFrames: 2
        );

        output.RenderNext(2);

        output.GetAllSamples().Should().Equal(0f, 1f, 2f, 3f);
    }

    [Fact]
    public void RenderNext_AfterStop_DoesNothing()
    {
        using var output = new CaptureAudioOutput();
        output.Start(FillIncrementing, channels: 1, sampleRate: 44100, bufferSizeInFrames: 4);

        output.Stop();
        output.RenderNext();

        output.CapturedBuffers.Should().BeEmpty();
    }

    [Fact]
    public void RenderNext_BeforeStart_DoesNothing()
    {
        using var output = new CaptureAudioOutput();

        output.RenderNext();

        output.CapturedBuffers.Should().BeEmpty();
    }

    private static void FillIncrementing(Span<float> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = i;
    }
}
