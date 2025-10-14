---
title: フレームバッファ
description: ノードをテクスチャにレンダリングするFrameBufferの使用方法について解説します。
sidebar:
  order: 10
---

`FrameBuffer`を用いると、ノードをテクスチャにレンダリングできます。ミニマップ、リアルタイム反射、ポストエフェクト、動的テクスチャ生成など、高度なグラフィック表現に使用されます。

## フレームバッファの作成

```csharp title="基本的なフレームバッファの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // フレームバッファサポートの確認
        if (!App.IsFrameBufferSupported)
        {
            Console.WriteLine("このバックエンドはFrameBufferをサポートしていません");
            return;
        }

        // 256x256のフレームバッファを作成
        var frameBuffer = new FrameBuffer(256, 256);

        // ノードを追加（これらはテクスチャにレンダリングされる）
        frameBuffer.Add(new Text("Hello, World!", Font.GetDefault(), Color.Black));
        frameBuffer.Add(new Sprite(playerTexture).Location(50, 50));

        // フレームバッファのテクスチャを画面に表示
        var resultSprite = new Sprite(frameBuffer.Texture)
            .Location(100, 100);

        Root.Add(resultSprite);
    }
}
```

## 背景色の設定

```csharp title="背景色の設定"
var frameBuffer = new FrameBuffer(256, 256);

// 背景色を設定
frameBuffer.BackgroundColor = Color.White;      // 白背景
frameBuffer.BackgroundColor = Color.Black;      // 黒背景
frameBuffer.BackgroundColor = Color.Transparent; // 透明背景（デフォルト）
```

## ノート

### バックエンドサポート

フレームバッファはすべてのバックエンドでサポートされているわけではありません。使用前に`PrometeApp.IsFrameBufferSupported`で確認してください。

```csharp title="サポート確認"
public void CheckFrameBufferSupport()
{
    if (!App.IsFrameBufferSupported)
    {
        Console.WriteLine("このバックエンドはフレームバッファをサポートしていません");
        // フォールバック処理
        return;
    }

    // フレームバッファを使用した処理
    var frameBuffer = new FrameBuffer(256, 256);
    // ...
}
```

### メモリ使用量

フレームバッファはメモリを消費するため、適切に管理してください。

```csharp title="メモリ管理"
public class FrameBufferManager : IDisposable
{
    private readonly List<FrameBuffer> frameBuffers = new();

    public FrameBuffer CreateFrameBuffer(int width, int height)
    {
        var buffer = new FrameBuffer(width, height);
        frameBuffers.Add(buffer);
        return buffer;
    }

    public void Dispose()
    {
        foreach (var buffer in frameBuffers)
        {
            buffer.Dispose();
        }
        frameBuffers.Clear();
    }
}
```
