---
title: カスタムバックエンド
description: Prometeで独自のバックエンド（IWindow実装）を作成し、アプリケーションで利用する方法を解説します。
sidebar:
  order: 6
---

Prometeでは、標準のOpenGLデスクトップやヘッドレス以外にも、独自のバックエンド（`IWindow`実装）を作成して利用できます。
ここではカスタムバックエンドの作り方とアプリケーションでの利用例を解説します。

## 基本の使い方

バックエンドは `IWindow` インターフェースを実装して作成します。
ウィンドウ管理や描画、入力処理などを独自に実装できます。

```csharp
using Promete.Windowing;

public class MyWindow : IWindow
{
    // 必要なプロパティ・イベント・メソッドを実装
    public event Action? Update;
    public event Action? Render;
    public event Action? Destroy;
    public void Run(WindowOptions opts) { ... }
    public void Exit() { ... }
    // その他IWindowのメンバーを実装
}
```

## 主なAPI

- `Update`, `Render`, `Destroy`<br/>各種イベント
- `Run(opts)`<br/>ウィンドウの起動
- `Exit()`<br/>ウィンドウの終了

## サンプル：アプリケーションでの利用

```csharp
var app = PrometeApp.Create()
    .Use<MyWindow>()
    .Build<MyWindow>();
```

## ノート

- カスタムバックエンドはIWindowの全メンバーを実装する必要があります
- 標準のOpenGLDesktopWindowやHeadlessWindowを参考に実装すると便利です
- プラットフォーム固有の処理や最適化も可能です
