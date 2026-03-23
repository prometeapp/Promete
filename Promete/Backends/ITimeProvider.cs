namespace Promete.Backends;

public interface ITimeProvider
{
    /// <summary>
    /// ゲーム起動時からの経過時間を取得または設定します。
    /// </summary>
    public float TotalTime { get; }

    /// <summary>
    /// ゲームが開始してからの、<see cref="TimeScale"/> に影響しない実際の時間を取得します。
    /// </summary>
    public float TotalTimeWithoutScale { get; }

    /// <summary>
    /// 前回の更新フレームからの経過時間を取得します。
    /// </summary>
    public float DeltaTime { get; }

    /// <summary>
    /// レンダリングFPSを取得します。
    /// </summary>
    public long FramePerSeconds { get; }

    /// <summary>
    /// 更新FPS (UPS) を取得します。
    /// </summary>
    public long UpdatePerSeconds { get; }

    /// <summary>
    /// ゲームが開始してからの総フレーム数を取得または設定します。
    /// </summary>
    public long TotalFrame { get; }

    /// <summary>
    /// FPS目標を取得または設定します。
    /// </summary>
    public int TargetFps { get; set; }

    /// <summary>
    /// UPS目標を取得または設定します。
    /// </summary>
    public int TargetUps { get; set; }

    /// <summary>
    /// 時間が流れる速度（通常の速度を<c>1.0f</c>とした倍率）を取得または設定します。
    /// </summary>
    public float TimeScale { get; set; }
}
