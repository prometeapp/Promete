using System;

namespace Promete.Audio.Filters;

/// <summary>
/// tanh ベースのソフトクリップにより波形整形を行う <see cref="IAudioFilter"/> の実装です。
/// 内部状態を持たないステートレスなフィルターです。
/// </summary>
public sealed class DistortionFilter : IAudioFilter
{
    private const float DefaultDrive = 2f;
    private const float DefaultLevel = 1f;

    private float _drive = DefaultDrive;
    private float _level = DefaultLevel;

    /// <summary>
    /// 増幅量を取得または設定します。値が大きいほど歪みが強くなります。デフォルトは 2 です。
    /// </summary>
    public float Drive
    {
        get => _drive;
        set => _drive = float.IsFinite(value) ? Math.Max(0f, value) : DefaultDrive;
    }

    /// <summary>
    /// 出力レベルを取得または設定します。範囲は 0.0～1.0、デフォルトは 1 です。
    /// </summary>
    public float Level
    {
        get => _level;
        set => _level = float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : DefaultLevel;
    }

    /// <inheritdoc />
    public void Process(Span<float> buffer, int channels, int sampleRate)
    {
        var drive = _drive;
        var level = _level;

        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = MathF.Tanh(buffer[i] * drive) * level;
    }

    /// <summary>
    /// ステートレスなフィルターのため、何も行いません。
    /// </summary>
    public void Reset() { }
}
