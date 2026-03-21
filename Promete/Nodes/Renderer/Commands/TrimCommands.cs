namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// トリム（クリッピング）開始コマンドです。計算済みの物理ピクセル座標を保持します。
/// </summary>
public sealed class BeginTrimCommand : IRenderCommand
{
    /// <summary>トリム矩形のX座標（物理ピクセル、左端基準）</summary>
    public required int X { get; init; }
    /// <summary>トリム矩形のY座標（物理ピクセル、下端基準）</summary>
    public required int Y { get; init; }
    /// <summary>トリム矩形の幅（物理ピクセル）</summary>
    public required int Width { get; init; }
    /// <summary>トリム矩形の高さ（物理ピクセル）</summary>
    public required int Height { get; init; }
}

/// <summary>
/// トリム（クリッピング）終了コマンドです。前の状態を復元するための情報を保持します。
/// </summary>
public sealed class EndTrimCommand : IRenderCommand
{
    /// <summary>復元するトリム矩形X</summary>
    public required int X { get; init; }
    /// <summary>復元するトリム矩形Y</summary>
    public required int Y { get; init; }
    /// <summary>復元するトリム矩形幅</summary>
    public required int Width { get; init; }
    /// <summary>復元するトリム矩形高さ</summary>
    public required int Height { get; init; }
    /// <summary>トリムを有効状態に戻すかどうか</summary>
    public required bool WasEnabled { get; init; }
}
