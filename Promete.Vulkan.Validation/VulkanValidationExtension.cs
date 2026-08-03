using Promete.Graphics.Rendering.Vulkan;

namespace Promete.Vulkan.Validation;

/// <summary>
/// Vulkan バリデーションレイヤーを有効化する拡張メソッドを提供します。
/// </summary>
public static class VulkanValidationExtension
{
    /// <summary>
    /// Vulkan のバリデーションレイヤーを有効化します。
    /// <c>BuildWithVulkanDesktop</c> より前に呼び出してください。
    /// </summary>
    /// <remarks>
    /// 開発時にのみ使用してください。バリデーションレイヤーは描画性能を大きく低下させます。
    /// Vulkan SDK が未インストールの環境では、警告を出したうえで無効のまま動作します。
    /// </remarks>
    /// <param name="builder">PrometeApp のビルダー。</param>
    /// <returns>同じビルダー。</returns>
    public static PrometeApp.PrometeAppBuilder UseVulkanValidation(
        this PrometeApp.PrometeAppBuilder builder
    )
    {
        return builder.Use<IVulkanInstanceHook, VulkanValidationHook>();
    }
}
