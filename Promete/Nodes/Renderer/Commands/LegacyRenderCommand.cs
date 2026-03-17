namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// 旧 <see cref="NodeRendererBase.Render"/> API との後方互換のためのラッパーコマンドです。
/// </summary>
public sealed class LegacyRenderCommand : IRenderCommand
{
    public required NodeRendererBase Renderer { get; init; }
    public required Node Node { get; init; }
}
