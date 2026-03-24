---
title: ウィンドウ
description: Prometeのウィンドウシステムと概念について
sidebar:
  order: 1
---

Prometeのウィンドウシステムは、`IWindow`インターフェースを通じてプラットフォームに依存しない統一的なAPIを提供します。OpenGLなどの低レベルな描画APIの詳細は完全に隠蔽されており、開発者は高レベルなAPIのみを使用してゲームを開発できます。

## ウィンドウの概念

Prometeにおけるウィンドウは、単なる表示領域以上の役割を持ちます：

- **描画のコンテキスト**: すべての描画処理の基盤
- **イベントの発生源**: 更新、描画、リサイズなどのイベント
- **入力の受け口**: キーボードやマウスなどの入力デバイスの管理
- **リソース管理**: テクスチャやその他のGraphics リソースの作成・管理

## ウィンドウへのアクセス

### シーンからのアクセス

シーン内では`Window`プロパティを通じてウィンドウにアクセスできます。

```csharp
public class MyScene : Scene
{
    public override void OnStart()
    {
        // ウィンドウのサイズを取得
        var (width, height) = Window.Size;

        // ウィンドウのタイトルを変更
        Window.Title = "ゲーム開始！";

        // デルタタイムを取得
        var deltaTime = Window.DeltaTime;
    }
}
```

### アプリケーションからのアクセス

`PrometeApp`からもウィンドウにアクセスできます。

```csharp
var app = PrometeApp.Current;
var window = app.Window;
```

## ウィンドウのプロパティ

### 基本的なプロパティ

```csharp
// 位置とサイズ
window.Location = new VectorInt(100, 100);
window.Size = new VectorInt(800, 600);
window.X = 100;
window.Y = 100;
window.Width = 800;
window.Height = 600;

// 実際のデバイス解像度
var actualSize = window.ActualSize;  // DPIスケーリングを考慮した実際のサイズ
var actualWidth = window.ActualWidth;
var actualHeight = window.ActualHeight;

// 表示状態
window.IsVisible = true;
window.IsFocused; // 読み取り専用
window.IsFullScreen = false;
window.TopMost = false;

// タイトル
window.Title = "My Promete Game";
```

### ウィンドウモード

```csharp
// ウィンドウモードの設定
window.Mode = WindowMode.Resizable;  // リサイズ可能
window.Mode = WindowMode.Fixed;      // 固定サイズ
window.Mode = WindowMode.NoFrame;    // フレームなし
```

### スケール設定

Prometeでは、ゲーム内の座標系を変更せずにウィンドウの表示サイズを拡大できます。

```csharp
// ピクセルアート向けの整数倍拡大
window.Scale = 1;  // 等倍
window.Scale = 2;  // 2倍
window.Scale = 4;  // 4倍
window.Scale = 8;  // 8倍
```

この機能により、320×240のピクセルアートゲームを640×480や1280×960のウィンドウで表示できます。

## パフォーマンスと時間管理

### フレームレート制御

```csharp
// FPS目標値の設定
window.TargetFps = 60;

// UPS（Updates Per Second）目標値の設定
window.TargetUps = 60;

// VSync の有効/無効
window.IsVsyncMode = true;

// 時間スケールの調整（スローモーションやファストフォワード）
window.TimeScale = 1.0f;  // 通常速度
window.TimeScale = 0.5f;  // 半分の速度
window.TimeScale = 2.0f;  // 2倍速
```

### 時間とフレーム情報

```csharp
// 現在のフレーム情報
var deltaTime = window.DeltaTime;        // 前フレームからの経過時間
var totalTime = window.TotalTime;        // ゲーム開始からの経過時間
var totalFrame = window.TotalFrame;      // 総フレーム数

// パフォーマンス情報
var fps = window.FramePerSeconds;        // 現在のFPS
var ups = window.UpdatePerSeconds;       // 現在のUPS

// タイムスケールの影響を受けない時間
var realTime = window.TotalTimeWithoutScale;
```

## ウィンドウイベント

ウィンドウは様々なイベントを発生させます。

```csharp
// ゲームのライフサイクルイベント
window.Start += OnGameStart;
window.Update += OnGameUpdate;
window.Render += OnGameRender;
window.Destroy += OnGameDestroy;

// 追加のイベント
window.PreUpdate += OnPreUpdate;     // 更新前
window.PostUpdate += OnPostUpdate;   // 更新後
window.Resize += OnWindowResize;     // ウィンドウリサイズ
window.FileDropped += OnFileDropped; // ファイルドロップ

void OnGameStart()
{
    Console.WriteLine("ゲーム開始");
}

void OnWindowResize()
{
    Console.WriteLine($"ウィンドウサイズ変更: {window.Size}");
}

void OnFileDropped(FileDroppedEventArgs e)
{
    foreach (var file in e.Files)
    {
        Console.WriteLine($"ファイルがドロップされました: {file}");
    }
}
```

## テクスチャファクトリー

ウィンドウは、テクスチャの作成を担当する`TextureFactory`を提供します。

```csharp
// 画像ファイルからテクスチャを作成
var texture = window.TextureFactory.Load("assets/player.png");

// 単色のテクスチャを作成
var whiteTexture = window.TextureFactory.CreateSolid(Color.White, 32, 32);

// 空のテクスチャを作成
var emptyTexture = window.TextureFactory.CreateEmpty(256, 256);
```

## スクリーンショット機能

ウィンドウの内容をキャプチャできます。

```csharp
// スクリーンショットをテクスチャとして取得
var screenshotTexture = window.TakeScreenshot();

// スクリーンショットをファイルに保存
await window.SaveScreenshotAsync("screenshot.png");
```

## 描画APIの隠蔽

Prometeのウィンドウシステムでは、OpenGL等の低レベルな描画APIの詳細は完全に隠蔽されています。

他の描画バックエンドに移行したとしても、基本的にゲームコードを変更する必要はありません。

## 関連項目

- [PrometeApp](/guide/manual/app) - アプリケーションの初期化
- [テクスチャ](/guide/graphics/textures) - テクスチャの管理
- [シーン](/guide/graphics/scene) - シーンの基本概念
