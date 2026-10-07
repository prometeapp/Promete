using System.Threading.Tasks;

namespace Promete.Audio;

/// <summary>
/// 音声の出力先を提供するインターフェースです。
/// </summary>
/// <remarks>
/// <see cref="AudioPlayer"/> は、実行中のバックエンドが提供する実装 (<see cref="Backends.BackendBase.SetupAudioProvider"/>) を使って音声を出力します。
/// </remarks>
public interface IAudioProvider
{
    /// <summary>
    /// <see cref="AudioPlayer"/> が使用する出力を生成します。生成した出力は、<see cref="AudioPlayer"/> が破棄します。
    /// </summary>
    /// <returns>生成した出力。</returns>
    public IAudioOutput CreateOutput();

    /// <summary>
    /// 指定した音源を、その場で 1 回だけ再生します。
    /// </summary>
    /// <param name="source">再生する音源。長さが確定している必要があります。</param>
    /// <param name="gain">再生する音量。</param>
    /// <param name="pitch">再生時のピッチ。</param>
    /// <param name="pan">再生時のパン。</param>
    /// <returns>再生が終わると完了するタスク。</returns>
    public ValueTask PlayOneShotAsync(IAudioSource source, float gain, float pitch, float pan);
}
