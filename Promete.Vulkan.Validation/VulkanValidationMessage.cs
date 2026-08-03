namespace Promete.Vulkan.Validation;

/// <summary>バリデーションメッセージの深刻度。</summary>
public enum VulkanValidationLevel
{
    /// <summary>警告。</summary>
    Warning,

    /// <summary>エラー。仕様違反や未定義動作を示します。</summary>
    Error,
}

/// <summary>バリデーションレイヤーからのメッセージ。</summary>
/// <param name="Level">深刻度。</param>
/// <param name="Message">本文。</param>
public readonly record struct VulkanValidationMessage(
    VulkanValidationLevel Level,
    string Message
);
