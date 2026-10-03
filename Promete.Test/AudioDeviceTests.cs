using FluentAssertions;
using Promete.Audio;

namespace Promete.Test;

public class AudioDeviceTests
{
    [Fact]
    public void TwoAudioPlayers_CanBeCreatedAndDisposedSimultaneously()
    {
        var createAndDispose = () =>
        {
            using var player1 = new AudioPlayer();
            using var player2 = new AudioPlayer();
        };

        createAndDispose.Should().NotThrow();
    }

    [Fact]
    public void Acquire_ReturnsSameInstance_WhileReferencesAreHeld()
    {
        var device1 = AudioDevice.Acquire();
        var device2 = AudioDevice.Acquire();

        device1.Should().BeSameAs(device2);

        device1.Dispose();
        device2.Dispose();
    }

    [Fact]
    public void Dispose_DoesNotReleaseDevice_WhileOtherReferencesAreHeld()
    {
        var device1 = AudioDevice.Acquire();
        var device2 = AudioDevice.Acquire();

        device1.Dispose();

        // device1 を解放しても device2 はまだ有効な参照を保持しているため、
        // device2 の破棄は例外を投げない
        var disposeSecond = () => device2.Dispose();
        disposeSecond.Should().NotThrow();
    }

    [Fact]
    public void Acquire_AfterAllReferencesReleased_ReinitializesSuccessfully()
    {
        var device1 = AudioDevice.Acquire();
        device1.Dispose();

        var device2 = AudioDevice.Acquire();
        var useDevice = device2.Al.GetError;

        useDevice.Should().NotThrow();

        device2.Dispose();
    }
}
