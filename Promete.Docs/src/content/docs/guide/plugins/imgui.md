---
title: ImGUI連携
description: PrometeでImGUIを利用する方法・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

ImGUIプラグイン（Promete.ImGui）を利用することで、アプリケーションにImGUIベースのUIを組み込むことができます。

:::caution
本プラグインは、OpenGLDesktopバックエンド限定で動作します。
:::

## インストール

ImGUI連携を利用するには、外部NuGetパッケージ `Promete.ImGui` をプロジェクトに追加してください。

```sh
dotnet add package Promete.ImGui
```

## 動作要件

- .NET 8
- Promete最新版
- OpenGL Desktop バックエンド

## 基本の使い方

ImGuiPluginをアプリケーションに登録し、シーンでインジェクションして利用します。

```csharp
using Promete;
using Promete.ImGui;

var app = PrometeApp.Create()
    .Use<ImGuiPlugin>()
    .BuildWithOpenGLDesktop();

app.Run<MainScene>();

class MainScene(ImGuiPlugin imgui) : Scene
{
    public override void OnStart()
    {
        imgui.Render += OnRenderUI;
    }

    private void OnRenderUI()
    {
        ImGuiNET.ImGui.ShowDemoWindow();
    }
}
```

## ImGuiPluginのカスタマイズ

ImGuiPluginを継承し、`OnConfigure`メソッドをオーバーライドすることでImGuiの初期設定をカスタマイズできます。

初期設定の具体的な説明については、ImGui.NETのドキュメント等を参照してください。本ドキュメントでは割愛します。

```csharp
using Promete.ImGui;
using ImGuiNET;

public class MyImGuiPlugin : ImGuiPlugin
{
    protected override void OnConfigure(ImGuiIOPtr io)
    {
        // ここでフォントや設定をカスタマイズ
        io.FontGlobalScale = 2.0f;
    }
}
```

アプリケーション登録時に `Use<MyImGuiPlugin>()` として利用できます。

```csharp
var app = PrometeApp.Create()
    .Use<MyImGuiPlugin>()
    .BuildWithOpenGLDesktop();
```

## 主なAPI

- `ImGuiPlugin.Render`<br/>ImGUI描画時に呼ばれるイベント
- `IsSyncronizeWithWindowScaling`<br/>ウィンドウのスケーリング値と同期するかどうか

## サンプル：ImGUIでUIを表示

```csharp
imgui.Render += () =>
{
    ImGuiNET.ImGui.Begin("ImGui Window");
    ImGuiNET.ImGui.Text("Hello, ImGui from Promete!");
    if (ImGuiNET.ImGui.Button("Button")) { /* ... */ }
    ImGuiNET.ImGui.End();
};
```

## ノート

- ImGuiPluginはOpenGL Desktopバックエンドでのみ利用可能です
- シーン破棄時は `imgui.Render -= ...` でイベント解除を推奨します
- ImGuiの初期設定をカスタマイズしたい場合はImGuiPluginを継承し、OnConfigureをオーバーライドしてください
