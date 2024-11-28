---
title: Promete Appの初期化
description: Promete Appの初期化方法について説明します。
sidebar:
  order: 1
  badge:
    text: WIP
    variant: danger
---

Promete製アプリの初期化には、まず `PrometeApp.Create` 静的メソッドを呼び出す必要があります。

```csharp
using Promete;
using Promete.GLDesktop;
var app = PrometeApp.Create()
  .BuildWithOpenGLDesktop();
```

`BuildWithOpenGLDesktop` メソッドは、PrometeをOpenGLを用いたデスクトップアプリとして初期化する拡張メソッドで、
内部的に必要なコンポーネントをすべて読み込みます。

:::tip
現時点では、OpenGLを用いたデスクトップアプリのみがサポートされています。

将来的に他のプラットフォームに対応するとき、初期化メソッドが新たに追加される予定です。
:::

アプリケーションを実際に起動する場合、Createメソッドの戻り値である `PrometeApp` インスタンスの `Run` メソッドを呼び出します。

```csharp
app.Run();
```

これを実行すると、最小構成のPrometeアプリが起動します。具体的には、640x480の黒いウィンドウです。

## 初期値の変更

`app.Run()` メソッドの引数として、`WindowOptions` オブジェクトを渡すことで、アプリケーションの初期値を変更することができます。

```cs title="例"
app.Run(new WindowOptions
{
  Title = "My App",
  Width = 800,
  Height = 600
});
```

| プロパティ名 |
