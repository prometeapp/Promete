using Promete.Graphics;
using Promete.Nodes;

namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// ステンシルマスク開始コマンドです。
/// </summary>
public sealed class BeginStencilMaskCommand : IRenderCommand
{
    public required MaskedContainer Container { get; init; }
    public required Texture2D MaskTexture { get; init; }
}

/// <summary>
/// アルファマスク開始コマンドです。子ノードをサブレンダリングします。
/// </summary>
public sealed class BeginAlphaMaskCommand : IRenderCommand
{
    public required MaskedContainer Container { get; init; }
    public required Texture2D MaskTexture { get; init; }
}

/// <summary>
/// マスク終了コマンドです。
/// </summary>
public sealed class EndMaskCommand : IRenderCommand;
