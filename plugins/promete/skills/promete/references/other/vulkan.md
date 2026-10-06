---
title: Vulkanバックエンド
description: Prometeの実験的なVulkanバックエンドを使ってアプリケーションを起動する方法と注意点を解説します。
sidebar:
  order: 3
---

Prometeでは、標準のOpenGLバックエンドに加えて、Vulkanを使って描画するバックエンドを利用できます。

:::caution
Vulkanバックエンドは**実験的な機能**です。動作が不安定な場合や、OpenGLバックエンドと描画結果が異なる場合があります。また、今後のバージョンでAPIや挙動が予告なく変更される可能性があります。

製品として公開するゲームでは、OpenGLバックエンド（`BuildWithOpenGLDesktop()`）の使用をおすすめします。
:::

## 動作要件

- Vulkan 1.2 に対応したGPUとグラフィックスドライバー
- Windows / macOS / Linux

macOSには標準でVulkanが搭載されていませんが、Vulkanを動かすためのライブラリ（MoltenVK）がPrometeに同梱されているため、追加でインストールする必要はありません。

## 基本の使い方

アプリケーションを構築する際に、`BuildWithOpenGLDesktop()` の代わりに `BuildWithVulkanDesktop()` を呼び出します。
`BuildWithVulkanDesktop()` は `Promete.VulkanDesktop` 名前空間にあります。

```csharp title="Program.cs"
using Promete;
using Promete.Input;
using Promete.VulkanDesktop;

var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .BuildWithVulkanDesktop();

return app.Run<MainScene>();
```

これだけで、シーンやノードのコードを変更することなく、Vulkanで描画されるようになります。

ウィンドウの設定は、OpenGLバックエンドと同じように `WindowOptions` で指定できます。

```csharp title="ウィンドウの設定を指定する"
using Promete.Windowing;

var app = PrometeApp.Create()
    .Use<Keyboard>()
    .BuildWithVulkanDesktop(WindowOptions.Default with
    {
        Title = "My Game (Vulkan)",
        Mode = WindowMode.Resizable,
    });
```

## 起動時にバックエンドを切り替える

実験的な機能のため、問題が起きたときにすぐOpenGLへ戻せるようにしておくと安心です。
たとえば、コマンドライン引数に `--vulkan` が指定されたときだけVulkanバックエンドを使うようにできます。

```csharp title="Program.cs"
using Promete;
using Promete.GLDesktop;
using Promete.Input;
using Promete.VulkanDesktop;

var builder = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>();

// --vulkan が指定されたときだけ Vulkan バックエンドを使う
var app = args.Contains("--vulkan")
    ? builder.BuildWithVulkanDesktop()
    : builder.BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

```sh
dotnet run -- --vulkan
```

## 使用中のバックエンドを調べる

シーンの中で、現在Vulkanバックエンドで動いているかどうかを調べたい場合は、`App.View` の型を確認します。

```csharp
using Promete.Backends.Vulkan;

public class MainScene : Scene
{
    public override void OnStart()
    {
        if (App.View is VulkanDesktopGameView)
        {
            // Vulkan バックエンドで動作している
        }
    }
}
```

## 注意点

### カスタムシェーダー

OpenGLバックエンド向けに書いたシェーダー（GLSL 3.30）は、Vulkanバックエンドではそのまま使えません。
`ShaderProgram` や `Material`、ポストプロセスでカスタムシェーダーを使っている場合は、Vulkan向けのシェーダーを別途用意する必要があります。

カスタムシェーダーを使っていない場合は、特に気にする必要はありません。

### ImGui連携

[ImGui連携](/guide/plugins/imgui)プラグインは、Vulkanバックエンドでも利用できます。

## ノート

- Vulkanバックエンドは実験的な機能です。不具合を見つけた場合は、OpenGLバックエンドで同じ問題が起きるかどうかを確認してみてください
- Vulkanに対応したGPUが見つからない環境では、起動時に `NotSupportedException` がスローされます
- シーンやノード、入力、オーディオなどのAPIは、OpenGLバックエンドと同じように使えます
