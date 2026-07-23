using System;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan バックエンドにおける <see cref="IShaderFactory"/> の実装です。
/// </summary>
/// <remarks>
/// TODO: Phase 3+ でカスタムシェーダー (Vulkan 方言 GLSL 450) のコンパイルとマテリアル適用に対応する。
/// </remarks>
internal sealed class VulkanShaderFactory : IShaderFactory
{
    public void Compile(ShaderProgram program)
    {
        throw new NotSupportedException(
            "Vulkan バックエンドはまだカスタムシェーダーをサポートしていません。"
        );
    }
}
