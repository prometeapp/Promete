using System;

namespace Promete.Audio.Filters;

/// <summary>
/// ディレイライン（フィードバック付きこだま効果）を適用する <see cref="IAudioFilter"/> の実装です。
/// L/R それぞれ独立したディレイラインを持ちます。
/// </summary>
public sealed class DelayFilter : IAudioFilter
{
    private const float DefaultTime = 0.3f;
    private const float DefaultFeedback = 0.4f;
    private const float DefaultMix = 0.5f;

    private float[] _lineL = [];
    private float[] _lineR = [];
    private int _writeIndex;
    private int _lastSampleRate;
    private float _time = DefaultTime;
    private float _feedback = DefaultFeedback;
    private float _mix = DefaultMix;

    /// <summary>
    /// ディレイタイム（秒）を取得または設定します。デフォルトは 0.3 秒です。
    /// 変更するとディレイラインの長さが次回 <see cref="Process"/> 呼び出し時に再確保されます。
    /// </summary>
    public float Time
    {
        get => _time;
        set => _time = float.IsFinite(value) ? Math.Max(0f, value) : DefaultTime;
    }

    /// <summary>
    /// フィードバック量を取得または設定します。範囲は 0.0～1.0 未満、デフォルトは 0.4 です。
    /// </summary>
    public float Feedback
    {
        get => _feedback;
        set => _feedback = float.IsFinite(value) ? Math.Clamp(value, 0f, 0.999f) : DefaultFeedback;
    }

    /// <summary>
    /// ウェット（ディレイ音）の混合比率を取得または設定します。範囲は 0.0～1.0、デフォルトは 0.5 です。
    /// </summary>
    public float Mix
    {
        get => _mix;
        set => _mix = float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : DefaultMix;
    }

    /// <inheritdoc />
    public void Process(Span<float> buffer, int channels, int sampleRate)
    {
        if (channels != 2)
            return;

        EnsureLineLength(sampleRate);

        var lineLength = _lineL.Length;
        if (lineLength == 0)
            return;

        var feedback = _feedback;
        var mix = _mix;
        var frameCount = buffer.Length / channels;

        for (var i = 0; i < frameCount; i++)
        {
            var inL = buffer[i * 2];
            var inR = buffer[(i * 2) + 1];

            var delayedL = _lineL[_writeIndex];
            var delayedR = _lineR[_writeIndex];

            _lineL[_writeIndex] = inL + (delayedL * feedback);
            _lineR[_writeIndex] = inR + (delayedR * feedback);

            buffer[i * 2] = inL + ((delayedL - inL) * mix);
            buffer[(i * 2) + 1] = inR + ((delayedR - inR) * mix);

            _writeIndex++;
            if (_writeIndex >= lineLength)
                _writeIndex = 0;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_lineL);
        Array.Clear(_lineR);
        _writeIndex = 0;
    }

    private void EnsureLineLength(int sampleRate)
    {
        var requiredLength = Math.Max(1, (int)(_time * sampleRate));
        if (requiredLength == _lineL.Length && sampleRate == _lastSampleRate)
            return;

        _lineL = new float[requiredLength];
        _lineR = new float[requiredLength];
        _writeIndex = 0;
        _lastSampleRate = sampleRate;
    }
}
