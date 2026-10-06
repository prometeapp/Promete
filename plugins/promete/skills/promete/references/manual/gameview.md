---
title: ゲームビュー
description: ウィンドウの表示・サイズ・状態を管理するIGameViewについて解説します。
sidebar:
  order: 1
---

`IGameView` は、ウィンドウの表示・サイズ・タイトル・表示状態などを管理するインターフェースです。シーン内では `View` プロパティを通じてアクセスします。

```csharp
public class MyScene : Scene
{
    public override void OnStart()
    {
        View.Title = "ゲーム開始！";
        var (width, height) = View.Size;
    }
}
```

## プロパティ

### 位置とサイズ

```csharp
// ウィンドウの位置
View.Location = new VectorInt(100, 100);

// ゲーム解像度（論理ピクセル）
View.Size = new VectorInt(800, 600);

// 実際のデバイス解像度（DPIスケーリング後）
var actualSize = View.ActualSize;

// Retinaなどの高解像度ディスプレイのピクセル比
var pixelRatio = View.PixelRatio;
```

### スケール設定

ゲーム内の論理解像度を変えずに、ウィンドウの表示倍率を整数倍で拡大できます。ピクセルアートゲームに便利です。

```csharp
View.Scale = 1;  // 等倍
View.Scale = 2;  // 2倍（320x240 → 640x480）
View.Scale = 4;  // 4倍
```

### 表示状態

```csharp
View.IsVisible = true;        // 表示/非表示
var focused = View.IsFocused; // フォーカス状態（読み取り専用）
View.IsFullScreen = false;    // フルスクリーン
```

### タイトル

```csharp
View.Title = "My Game";
```

### ウィンドウモード

```csharp
View.Mode = WindowMode.Resizable;  // リサイズ可能
View.Mode = WindowMode.Fixed;      // 固定サイズ
View.Mode = WindowMode.NoFrame;    // フレームなし
```

## スクリーンショット

```csharp
// テクスチャとして取得
var texture = View.TakeScreenshot();

// ファイルに保存
await View.SaveScreenshotAsync("screenshot.png");
```

## イベント

```csharp
// ウィンドウリサイズ
View.Resize += () =>
{
    Console.WriteLine($"新しいサイズ: {View.Size}");
};

// ファイルドロップ
View.FileDropped += e =>
{
    foreach (var file in e.Files)
        Console.WriteLine($"ドロップされたファイル: {file}");
};
```

## 関連項目

- [時間管理](/guide/manual/timeprovider) - フレーム時間・FPS・タイムスケール
- [PrometeApp](/guide/manual/app) - アプリケーションの初期化
