namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// シザーテスト開始コマンドです。計算済みのピクセル座標を保持します。
/// </summary>
public sealed class BeginScissorCommand : IRenderCommand
{
    /// <summary>シザー矩形のX座標（スクリーン座標）</summary>
    public required int X { get; init; }
    /// <summary>シザー矩形のY座標（スクリーン座標、下端基準）</summary>
    public required int Y { get; init; }
    /// <summary>シザー矩形の幅（ピクセル）</summary>
    public required int Width { get; init; }
    /// <summary>シザー矩形の高さ（ピクセル）</summary>
    public required int Height { get; init; }
    /// <summary>親のシザー状態が有効だったかどうか</summary>
    public required bool ParentWasEnabled { get; init; }
    /// <summary>親のシザー矩形X</summary>
    public required int ParentX { get; init; }
    /// <summary>親のシザー矩形Y</summary>
    public required int ParentY { get; init; }
    /// <summary>親のシザー矩形幅</summary>
    public required int ParentWidth { get; init; }
    /// <summary>親のシザー矩形高さ</summary>
    public required int ParentHeight { get; init; }
}

/// <summary>
/// シザーテスト終了コマンドです。
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
