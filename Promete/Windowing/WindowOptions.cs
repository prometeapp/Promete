namespace Promete.Windowing;

public readonly record struct WindowOptions(
    VectorInt Location,
    VectorInt Size,
    string Title,
    int Scale = 1,
    bool IsFullScreen = false,
    WindowMode Mode = WindowMode.Fixed,
    int TargetFps = 60,
    int TargetUps = 60,
    bool IsVsyncMode = false
)
{
    public static WindowOptions Default { get; } = new(
        (50, 50),
        (640, 480),
        "Promete Window"
    );
}
