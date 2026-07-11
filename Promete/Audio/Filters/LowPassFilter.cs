using System;

namespace Promete.Audio.Filters;

/// <summary>
/// RBJ Audio EQ Cookbook 準拠の2次ローパスフィルター（biquad）を適用する <see cref="IAudioFilter"/> の実装です。
/// L/R チャンネルは独立した状態変数を持ちます。
/// </summary>
public sealed class LowPassFilter : IAudioFilter
{
    private const float DefaultCutoffFrequency = 1000f;
    private const float DefaultResonance = 0.707f;
    private const float DefaultMix = 1f;
    private const float MinCutoffFrequency = 10f;

    private float _cutoffFrequency = DefaultCutoffFrequency;
    private float _resonance = DefaultResonance;
    private float _mix = DefaultMix;
    private int _lastSampleRate;
    private bool _coefficientsValid;

    private float _b0;
    private float _b1;
    private float _b2;
    private float _a1;
    private float _a2;

    private float _x1L;
    private float _x2L;
    private float _y1L;
    private float _y2L;
    private float _x1R;
    private float _x2R;
    private float _y1R;
    private float _y2R;

    /// <summary>
    /// カットオフ周波数（Hz）を取得または設定します。デフォルトは 1000Hz です。
    /// </summary>
    public float CutoffFrequency
    {
        get => _cutoffFrequency;
        set
        {
            _cutoffFrequency = float.IsFinite(value)
                ? Math.Max(MinCutoffFrequency, value)
                : DefaultCutoffFrequency;
            _coefficientsValid = false;
        }
    }

    /// <summary>
    /// レゾナンス（Q値）を取得または設定します。デフォルトは 0.707（バターワース特性）です。
    /// </summary>
    public float Resonance
    {
        get => _resonance;
        set
        {
            _resonance = float.IsFinite(value) ? Math.Max(0.01f, value) : DefaultResonance;
            _coefficientsValid = false;
        }
    }

    /// <summary>
    /// ウェット（フィルター適用後の音）の混合比率を取得または設定します。範囲は 0.0～1.0、デフォルトは 1（ウェットのみ）です。
    /// 0 に近づけるほど元の音が残るため、値を徐々に変化させることでフィルターの掛かり具合をスムーズに遷移できます。
    /// ミックス比率に関わらずフィルターの内部状態は常に更新されるため、遷移中も波形が不連続になりません。
    /// </summary>
    public float Mix
    {
        get => _mix;
        set => _mix = float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : DefaultMix;
    }

    /// <inheritdoc />
    public void Process(Span<float> buffer, int channels, int sampleRate)
    {
        if (!_coefficientsValid || sampleRate != _lastSampleRate)
            RecalculateCoefficients(sampleRate);

        var mix = _mix;
        var frameCount = buffer.Length / channels;
        for (var i = 0; i < frameCount; i++)
        {
            var dryL = buffer[i * channels];
            var wetL = ProcessSample(dryL, ref _x1L, ref _x2L, ref _y1L, ref _y2L);
            buffer[i * channels] = dryL + ((wetL - dryL) * mix);

            if (channels < 2)
                continue;

            var dryR = buffer[(i * channels) + 1];
            var wetR = ProcessSample(dryR, ref _x1R, ref _x2R, ref _y1R, ref _y2R);
            buffer[(i * channels) + 1] = dryR + ((wetR - dryR) * mix);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _x1L = _x2L = _y1L = _y2L = 0f;
        _x1R = _x2R = _y1R = _y2R = 0f;
    }

    private float ProcessSample(float input, ref float x1, ref float x2, ref float y1, ref float y2)
    {
        var output = (_b0 * input) + (_b1 * x1) + (_b2 * x2) - (_a1 * y1) - (_a2 * y2);
        x2 = x1;
        x1 = input;
        y2 = y1;
        y1 = output;
        return output;
    }

    private void RecalculateCoefficients(int sampleRate)
    {
        _lastSampleRate = sampleRate;
        _coefficientsValid = true;

        var effectiveSampleRate = Math.Max(1, sampleRate);
        var omega = 2f * MathF.PI * _cutoffFrequency / effectiveSampleRate;
        var sinOmega = MathF.Sin(omega);
        var cosOmega = MathF.Cos(omega);
        var alpha = sinOmega / (2f * _resonance);

        var a0 = 1f + alpha;
        _b0 = (1f - cosOmega) / 2f / a0;
        _b1 = (1f - cosOmega) / a0;
        _b2 = (1f - cosOmega) / 2f / a0;
        _a1 = -2f * cosOmega / a0;
        _a2 = (1f - alpha) / a0;
    }
}
