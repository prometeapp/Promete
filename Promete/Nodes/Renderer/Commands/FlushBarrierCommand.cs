namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// バッチを強制フラッシュするマーカーコマンドです。
/// </summary>
public sealed class FlushBarrierCommand : IRenderCommand
{
    /// <summary>デバッグ用のフラッシュ理由</summary>
    public string Reason { get; init; } = "";
}
