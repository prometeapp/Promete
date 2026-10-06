using System.Collections.Generic;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan インスタンスの生成に割り込むためのフックです。
/// レイヤーや拡張の追加、インスタンス生成後の初期化を行えます。
/// </summary>
/// <remarks>
/// <para>
/// <b>これは Promete 内部のプラグイン用 API です。通常のゲーム開発で実装しないでください。</b>
/// Vulkan のインスタンス生成に直接影響するため、誤った実装は初期化の失敗や
/// 未定義動作を招きます。予告なく変更される場合があります。
/// </para>
/// <para>
/// バリデーションレイヤーのようなデバッグ機能を本体から切り離すために用意されています。
/// 実装を DI コンテナへ登録すると、Vulkan バックエンドが初期化時に呼び出します。
/// 登録しなければ関連するコードもアセンブリも一切読み込まれません。
/// </para>
/// <code>
/// PrometeApp.Create()
///     .Use&lt;IVulkanInstanceHook, VulkanValidationHook&gt;()
///     .BuildWithVulkanDesktop();
/// </code>
/// </remarks>
public interface IVulkanInstanceHook
{
    /// <summary>
    /// 有効化したいインスタンスレイヤーの名前を返します。
    /// 実際に利用できないレイヤーは呼び出し側で除外されます。
    /// </summary>
    public IEnumerable<string> GetRequestedLayers();

    /// <summary>
    /// 有効化したいインスタンス拡張の名前を返します。
    /// <see cref="GetRequestedLayers"/> が 1 つも有効にならなかった場合は呼び出されません。
    /// </summary>
    public IEnumerable<string> GetRequestedExtensions();

    /// <summary>
    /// インスタンスの生成直後に呼び出されます。
    /// </summary>
    /// <param name="vk">Vulkan API のエントリポイント。</param>
    /// <param name="instance">生成されたインスタンス。</param>
    public void OnInstanceCreated(Vk vk, Instance instance);

    /// <summary>
    /// インスタンスの破棄直前に呼び出されます。
    /// <see cref="OnInstanceCreated"/> で確保したリソースを解放してください。
    /// </summary>
    /// <param name="vk">Vulkan API のエントリポイント。</param>
    /// <param name="instance">破棄されるインスタンス。</param>
    public void OnInstanceDestroying(Vk vk, Instance instance);
}
