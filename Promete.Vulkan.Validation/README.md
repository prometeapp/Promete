# Promete.Vulkan.Validation

Promete の Vulkan バックエンドで、Vulkan バリデーションレイヤー (`VK_LAYER_KHRONOS_validation`) を有効にするプラグインです。

**開発時のみ**参照してください。描画性能が大きく下がるため、リリースビルドでは参照を外します。

## 前提

- バリデーションレイヤー本体は含まれません。[Vulkan SDK](https://vulkan.lunarg.com/) をインストールしてください。
- SDK が見つからない環境では、警告を出したうえでバリデーション無効のまま動作します。

## 使い方

`BuildWithVulkanDesktop` より前に `UseVulkanValidation()` を呼びます。

```csharp
var app = PrometeApp.Create()
    .UseVulkanValidation()
    .BuildWithVulkanDesktop();
```

詳しくは [ドキュメント](https://promete.app/guide/other/vulkan/) を参照してください。
