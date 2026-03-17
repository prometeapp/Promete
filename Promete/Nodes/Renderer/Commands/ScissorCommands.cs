namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// シザーテスト開始コマンドです。計算済み（親との積集合済み）のピクセル座標を保持します。
/// </summary>
public sealed class BeginScissorCommand : IRenderCommand
{
    /// <summary>シザー矩形のX座標（物理ピクセル、左端基準）</summary>
    public required int X { get; init; }
    /// <summary>シザー矩形のY座標（物理ピクセル、下端基準）</summary>
    public required int Y { get; init; }
    /// <summary>シザー矩形の幅（物理ピクセル）</summary>
    public required int Width { get; init; }
    /// <summary>シザー矩形の高さ（物理ピクセル）</summary>
    public required int Height { get; init; }
}

/// <summary>
/// シザーテスト終了コマンドです。前の状態を復元するための情報を保持します。
/// </summary>
public sealed class EndScissorCommand : IRenderCommand
{
    /// <summary>復元するシザー矩形X</summary>
    public required int X { get; init; }
    /// <summary>復元するシザー矩形Y</summary>
    public required int Y { get; init; }
    /// <summary>復元するシザー矩形幅</summary>
    public required int Width { get; init; }
    /// <summary>復元するシザー矩形高さ</summary>
    public required int Height { get; init; }
    /// <summary>シザーテストを有効状態に戻すかどうか</summary>
    public required bool WasEnabled { get; init; }
}
