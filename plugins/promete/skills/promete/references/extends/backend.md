---
title: カスタムバックエンド
description: Prometeで独自のバックエンドを作成し、アプリケーションで利用する方法を解説します。
sidebar:
  order: 6
---

Prometeでは、標準のOpenGLデスクトップやヘッドレス以外にも、独自のバックエンドを作成して利用できます。
ここではカスタムバックエンドの作り方とアプリケーションでの利用例を解説します。

## 基本の使い方

バックエンドは `BackendBase` 抽象クラスを継承して作成します。
各抽象メソッドを実装することで、ウィンドウ管理・時間管理・入力処理・レンダリングなどを独自に構成できます。

```csharp
using Promete.Backends;

public class MyBackend : BackendBase
{
    public override void OnInitialize(PrometeApp app, WindowOptions windowOptions)
    {
        // 初期化処理
    }

    public override ITimeProvider SetupTimeProvider()
    {
        return new MyTimeProvider();
    }

    public override IGameView SetupGameView()
    {
        return new MyGameView();
    }

    public override InputProvider SetupInputProvider()
    {
        return new MyInputProvider();
    }

    public override IScreenBlitter SetupScreenBlitter()
    {
        return new MyScreenBlitter();
    }

    public override TextureFactoryBase SetupTextureFactory()
    {
        return new MyTextureFactory();
    }

    public override IRenderTextureProvider SetupRenderTextureProvider()
    {
        return new MyRenderTextureProvider();
    }

    public override IShaderFactory SetupShaderFactory()
    {
        return new MyShaderFactory();
    }

    public override void OnStart(PrometeApp app)
    {
        // アプリ開始時の処理（ゲームループの起動など）
    }

    public override void OnExit(PrometeApp app)
    {
        // 終了処理
    }
}
```

## 各メソッドの役割

| メソッド | 返す型 | 責務 |
|----------|--------|------|
| `SetupGameView()` | `IGameView` | ウィンドウ表示・サイズ・タイトル管理 |
| `SetupTimeProvider()` | `ITimeProvider` | フレーム時間・FPS・タイムスケール管理 |
| `SetupInputProvider()` | `InputProvider` | 入力デバイスのコンテキスト提供 |
| `SetupScreenBlitter()` | `IScreenBlitter` | 画面レンダリングとポストエフェクトの適用 |
| `SetupTextureFactory()` | `TextureFactoryBase` | テクスチャの読み込み・生成 |
| `SetupRenderTextureProvider()` | `IRenderTextureProvider` | オフスクリーンレンダリングターゲットの管理 |
| `SetupShaderFactory()` | `IShaderFactory` | シェーダーのコンパイル |

## アプリケーションでの利用

`Build<T>()` メソッドでカスタムバックエンドを指定してアプリを構築します。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Build<MyBackend>(WindowOptions.Default);
```

## ノート

- 実装の参考として、`OpenGLDesktopBackend` や `HeadlessBackend` のソースコードを参照してください
- `SetupRenderTextureProvider()` が `null` を返す（またはその実装が登録されない）場合、`FrameBuffer` は使用できません
