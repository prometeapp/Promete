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

```csharp title="Program.cs" {3}
using Promete;

var app = PrometeApp.Create()
```

`PrometeApp.Create` メソッドは、Prometeアプリケーションのインスタンスを生成するためのビルダーを返します。
ビルダーには、メソッドチェーンでアプリケーションの初期化に必要なコンポーネントを追加することができます。

## プラグインの追加
プラグインを追加する場合、ビルダーの `Use` メソッドを呼び出します。

```csharp title="Program.cs" {4-5}
using Promete;

var app = PrometeApp.Create()
  .Use<YourAwesomePlugin>()
  .Use<YourSweetPlugin>()
```

## プラットフォームを指定してビルド
プラグイン追加後のメソッドチェーンで、ビルドメソッドを呼び出します。

`BuildWithOpenGLDesktop` メソッドは、PrometeをOpenGLを用いたデスクトップアプリとして初期化する拡張メソッドです。

内部的に必要なプラグインをすべて読み込んで初期化し、`PrometeApp` クラスのインスタンスを返します。

最終的には、以下のような初期化コードになるでしょう。

```csharp title="Program.cs" {6}
using Promete;

var app = PrometeApp.Create()
  .Use<YourAwesomePlugin>()
  .Use<YourSweetPlugin>()
  .BuildWithOpenGLDesktop();
```

:::tip
現時点では、OpenGLを用いたデスクトップアプリのみがサポートされています。

将来的に他のプラットフォームに対応するとき、初期化メソッドが新たに追加される予定です。
:::

## 実行

アプリケーションを実際に起動する場合、`PrometeApp.Run()` メソッドを呼び出します。
なお、起動にはシーンが必須なので、空のシーンを作成します。

```csharp title="Program.cs" {8-10}
using Promete;

var app = PrometeApp.Create()
  .Use<YourAwesomePlugin>()
  .Use<YourSweetPlugin>()
  .BuildWithOpenGLDesktop();

return app.Run<MainScene>();

public class MainScene : Scene { }
```

これを実行すると、最小構成のPrometeアプリが起動します。 具体的には、640x480の黒いウィンドウです。

Run() メソッドは、アプリケーションが終了するまでブロックします。停止すると、アプリケーションの終了コードが返されます。この例では、終了コードを `return` しています。

## 初期値の変更

`app.Run()` メソッドの引数として、`WindowOptions` オブジェクトを渡すことで、アプリケーションの初期値を変更することができます。

```cs title="例"
return app.Run(WindowOptions.Default with
{
  Title = "My App",
  Width = 800,
  Height = 600
});
```

| プロパティ名 | 型 | 説明                                                |
| --- | --- |---------------------------------------------------|
| Location | VectorInt | ウィンドウの表示位置                                        |
| Size | VectorInt | ウィンドウのサイズ                                         |
| Title | string | ウィンドウのタイトル                                        |
| Scale | int | ウィンドウのスケール値。<br/>1, 2, 4, 8を指定でき、その倍率に描画内容を拡大します。 |
| IsFullScreen | bool | フルスクリーンモードで起動するかどうか                               |
| Mode | WindowMode | ウィンドウの表示モード。<br/>Resizable, Fixed, NoFrameを指定できます。 |
| TargetFps | int | ターゲットのFPS（描画フレームのレート）。<br/>デフォルトは60fpsです。         |
| TargetUps | int | ターゲットのUPS（更新フレームのレート）。<br/>デフォルトは60upsです。         |
| IsVsyncMode | bool | 垂直同期を有効化するかどうか                                    |
