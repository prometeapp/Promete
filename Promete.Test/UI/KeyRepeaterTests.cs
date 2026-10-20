using FluentAssertions;
using Promete.Input;
using Promete.UI;

namespace Promete.Test.UI;

/// <summary>
/// 時間は 2 進数で正確に表せる値 (0.125 の倍数) を使い、浮動小数点の誤差が出ないようにしています。
/// </summary>
public class KeyRepeaterTests
{
    private const float Delay = 0.5f;
    private const float Interval = 0.25f;

    private readonly KeyRepeater _repeater = new();
    private readonly Key _key = new();

    private void Press()
    {
        _key.IsPressed = true;
        _key.IsKeyDown = true;
    }

    private void Hold()
    {
        _key.IsPressed = true;
        _key.IsKeyDown = false;
    }

    private void Release()
    {
        _key.IsPressed = false;
        _key.IsKeyDown = false;
    }

    private int Poll(float deltaTime) => _repeater.Poll(_key, deltaTime, Delay, Interval);

    [Fact]
    public void Press_ShouldCountOnce_ImmediatelyRegardlessOfDeltaTime()
    {
        Press();

        Poll(0.016f).Should().Be(1);
    }

    [Fact]
    public void Hold_ShouldNotRepeat_BeforeDelay()
    {
        Press();
        Poll(0);
        Hold();

        Poll(0.125f).Should().Be(0);
        Poll(0.125f).Should().Be(0);
        Poll(0.125f).Should().Be(0);
    }

    [Fact]
    public void Hold_ShouldRepeatAtDelay_ThenEveryInterval()
    {
        Press();
        Poll(0);
        Hold();

        var counts = new List<int>();
        for (var i = 0; i < 8; i++)
            counts.Add(Poll(0.125f));

        // 経過: 0.125 0.25 0.375 0.5(開始) 0.625 0.75(繰り返し) 0.875 1.0(繰り返し)
        counts.Should().Equal(0, 0, 0, 1, 0, 1, 0, 1);
    }

    [Fact]
    public void LongFrame_ShouldCountEveryMissedRepeat()
    {
        Press();
        Poll(0);
        Hold();

        // 0.5 / 0.75 / 1.0 の 3 回分
        Poll(1.0f).Should().Be(3);
    }

    [Fact]
    public void Repeats_ShouldBeCappedPerFrame()
    {
        Press();
        Poll(0);
        Hold();

        _repeater.Poll(_key, 1000f, 0.5f, 0.001f).Should().Be(50);
    }

    [Fact]
    public void Release_ShouldStopRepeating_AndRestartOnNextPress()
    {
        Press();
        Poll(0);
        Hold();
        Poll(0.5f).Should().Be(1);

        Release();
        Poll(1.0f).Should().Be(0);

        Press();
        Poll(0).Should().Be(1);
        Hold();
        // 押し直したので、また遅延から数え直す
        Poll(0.25f).Should().Be(0);
        Poll(0.25f).Should().Be(1);
    }

    [Fact]
    public void KeyHeldWithoutSeeingThePress_ShouldNeverRepeat()
    {
        // フォーカスの前から押されていたキーなど、押した瞬間を見ていないキー
        Hold();

        Poll(1.0f).Should().Be(0);
        Poll(1.0f).Should().Be(0);
    }

    [Fact]
    public void Reset_ShouldForgetTheHeldKey()
    {
        Press();
        Poll(0);
        Hold();

        _repeater.Reset();

        Poll(2.0f).Should().Be(0);
    }

    [Fact]
    public void PressWhileHeld_ShouldRestartTheDelay()
    {
        Press();
        Poll(0);
        Hold();
        Poll(0.25f);

        Press();
        Poll(0).Should().Be(1);
        Hold();
        Poll(0.25f).Should().Be(0);
    }
}
