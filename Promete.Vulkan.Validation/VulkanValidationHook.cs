using Promete.Graphics.Rendering.Vulkan;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Promete.Vulkan.Validation;

/// <summary>
/// Vulkan のバリデーションレイヤーを有効化し、その指摘を出力するプラグインです。
/// </summary>
/// <remarks>
/// <para>
/// 開発時にのみ使用してください。バリデーションレイヤーは描画性能を大きく低下させます。
/// リリースビルドではこのパッケージへの参照ごと外すことを推奨します。
/// </para>
/// <para>
/// 使用には Vulkan SDK のインストールが必要です (https://vulkan.lunarg.com/sdk/home)。
/// レイヤーが見つからない場合は警告を出したうえで、何もせず動作を続けます。
/// </para>
/// <code>
/// PrometeApp.Create()
///     .UseVulkanValidation()
///     .BuildWithVulkanDesktop();
/// </code>
/// </remarks>
public sealed unsafe class VulkanValidationHook : IVulkanInstanceHook, IDisposable
{
    private const string ValidationLayerName = "VK_LAYER_KHRONOS_validation";

    /// <summary>
    /// 静的コールバックから参照するための現在のインスタンス。
    /// </summary>
    private static VulkanValidationHook? _current;

    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _messenger;
    private bool _disposed;

    /// <summary>
    /// バリデーションメッセージを受け取ったときに呼び出されます。
    /// 未設定の場合は標準エラー出力へ書き出します。
    /// </summary>
    public Action<VulkanValidationMessage>? OnMessage { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> GetRequestedLayers() => [ValidationLayerName];

    /// <inheritdoc />
    public IEnumerable<string> GetRequestedExtensions() => [ExtDebugUtils.ExtensionName];

    /// <inheritdoc />
    public void OnInstanceCreated(Vk vk, Instance instance)
    {
        if (!vk.TryGetInstanceExtension(instance, out ExtDebugUtils debugUtils))
        {
            Console.Error.WriteLine(
                "[Promete] VK_EXT_debug_utils を取得できませんでした。バリデーションメッセージは表示されません。"
            );
            return;
        }

        _debugUtils = debugUtils;

        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity =
                DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType =
                DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback,
        };

        // コールバックは静的関数ポインタとして渡るため、インスタンスを静的に保持する。
        // プラグインはシングルトンとして登録されるので、実質 1 つに限られる
        _current = this;

        var result = _debugUtils.CreateDebugUtilsMessenger(
            instance,
            in createInfo,
            null,
            out _messenger
        );

        if (result != Result.Success)
        {
            Console.Error.WriteLine($"[Promete] デバッグメッセンジャーの作成に失敗しました: {result}");
            _debugUtils.Dispose();
            _debugUtils = null;
        }
    }

    /// <inheritdoc />
    public void OnInstanceDestroying(Vk vk, Instance instance)
    {
        if (_debugUtils is null)
            return;

        _debugUtils.DestroyDebugUtilsMessenger(instance, _messenger, null);
        _debugUtils.Dispose();
        _debugUtils = null;

        if (ReferenceEquals(_current, this))
            _current = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        // インスタンス破棄は OnInstanceDestroying で行うため、ここでは参照を落とすのみ
        _debugUtils = null;
        if (ReferenceEquals(_current, this))
            _current = null;
    }

    private static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT type,
        DebugUtilsMessengerCallbackDataEXT* callbackData,
        void* userData
    )
    {
        var text = SilkMarshal.PtrToString((nint)callbackData->PMessage) ?? string.Empty;
        var level = severity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt)
            ? VulkanValidationLevel.Error
            : VulkanValidationLevel.Warning;

        var handler = _current?.OnMessage;
        if (handler is not null)
            handler(new VulkanValidationMessage(level, text));
        else
            Console.Error.WriteLine($"[Vulkan {level}] {text}");

        return Vk.False;
    }
}
