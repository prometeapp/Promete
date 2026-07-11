namespace Promete.Audio;

/// <summary>
/// <see cref="AudioRenderPipeline"/> が取りうる再生状態を表します。
/// </summary>
internal enum AudioRenderPipelineState
{
    /// <summary>
    /// 停止中。
    /// </summary>
    Stopped,

    /// <summary>
    /// 再生中。
    /// </summary>
    Playing,

    /// <summary>
    /// 一時停止中。
    /// </summary>
    Pausing,

    /// <summary>
    /// フェードアウト中。
    /// </summary>
    FadingOut,
}
