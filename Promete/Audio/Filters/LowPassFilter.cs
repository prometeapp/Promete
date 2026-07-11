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

    // 自動メイクアップゲインの安全域: 補正は最大+12dB(4倍)まで、無音に近い入力では補正値を更新しない
    private const float MaxMakeupGain = 4f;
    private const float MinMakeupGain = 0.25f;
    private const float SilenceRmsThreshold = 1e-4f;
    private const float MakeupSmoothingTimeConstant = 0.2f;

    private float _cutoffFrequency = DefaultCutoffFrequency;
    private float _resonance = DefaultResonance;
    private float _mix = DefaultMix;
    private float _makeupGain = 1f;
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

    /// <summary>
    /// 自動メイクアップゲインを有効にするかどうかを取得または設定します。デフォルトは <c>false</c> です。
    /// 有効にすると、フィルターで失われたエネルギー分だけ出力を自動的に増幅し、
    /// カットオフを絞っても体感音量が元の音に近づきます。
    /// 補正量は入力と出力（<see cref="Mix"/> 適用後）の RMS 比から求め、時間方向に平滑化して適用されます。
    /// 増幅は最大 +12dB までにクランプされ、無音に近い入力では補正値の更新を凍結します。
    /// </summary>
    public bool AutoMakeupGain { get; set; }

    /// <inheritdoc />
    public void Process(Span<float> buffer, int channels, int sampleRate)
    {
        if (!_coefficientsValid || sampleRate != _lastSampleRate)
            RecalculateCoefficients(sampleRate);

        var mix = _mix;
        var frameCount = buffer.Length / channels;
        var drySquaredSum = 0.0;
        var outSquaredSum = 0.0;

        for (var i = 0; i < frameCount; i++)
        {
            var dryL = buffer[i * channels];
            var wetL = ProcessSample(dryL, ref _x1L, ref _x2L, ref _y1L, ref _y2L);
            var outL = dryL + ((wetL - dryL) * mix);
            buffer[i * channels] = outL;
            drySquaredSum += dryL * dryL;
            outSquaredSum += outL * outL;

            if (channels < 2)
                continue;

            var dryR = buffer[(i * channels) + 1];
            var wetR = ProcessSample(dryR, ref _x1R, ref _x2R, ref _y1R, ref _y2R);
            var outR = dryR + ((wetR - dryR) * mix);
            buffer[(i * channels) + 1] = outR;
            drySquaredSum += dryR * dryR;
            outSquaredSum += outR * outR;
        }

        if (AutoMakeupGain)
            ApplyMakeupGain(buffer, drySquaredSum, outSquaredSum, sampleRate, frameCount, channels);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _x1L = _x2L = _y1L = _y2L = 0f;
        _x1R = _x2R = _y1R = _y2R = 0f;
        _makeupGain = 1f;
    }

    /// <summary>
    /// 入力(dry)と出力(Mix適用後)のRMS比から自動メイクアップゲインを算出し、平滑化した上でバッファへ適用します。
    /// ゲインの段差によるノイズを防ぐため、前回値から今回値までバッファ内で線形にランプさせます。
    /// </summary>
    private void ApplyMakeupGain(
        Span<float> buffer,
        double drySquaredSum,
        double outSquaredSum,
        int sampleRate,
        int frameCount,
        int channels
    )
    {
        var previousGain = _makeupGain;

        var dryRms = Math.Sqrt(drySquaredSum / Math.Max(1, buffer.Length));
        var outRms = Math.Sqrt(outSquaredSum / Math.Max(1, buffer.Length));

        // 無音に近い入力では比率が発散するため、補正値の更新を凍結する
        if (dryRms > SilenceRmsThreshold && outRms > SilenceRmsThreshold)
        {
            var targetGain = Math.Clamp((float)(dryRms / outRms), MinMakeupGain, MaxMakeupGain);

            // バッファ長に応じた1次ローパス平滑化（時定数 MakeupSmoothingTimeConstant 秒）
            var bufferSeconds = frameCount / (float)Math.Max(1, sampleRate);
            var smoothing = 1f - MathF.Exp(-bufferSeconds / MakeupSmoothingTimeConstant);
            _makeupGain += (targetGain - _makeupGain) * smoothing;
        }

        for (var i = 0; i < frameCount; i++)
        {
            var t = frameCount > 1 ? i / (float)(frameCount - 1) : 1f;
            var gain = previousGain + ((_makeupGain - previousGain) * t);
            for (var ch = 0; ch < channels; ch++)
                buffer[(i * channels) + ch] *= gain;
        }
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
