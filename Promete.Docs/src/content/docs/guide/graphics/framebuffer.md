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

:::note
バックエンドが`IRenderTextureProvider`をサポートしていない場合、`FrameBuffer`のコンストラクタで例外がスローされます。事前に `App.IsFrameBufferSupported` で確認してください。
:::

## 背景色の設定

```csharp
var frameBuffer = new FrameBuffer(256, 256);

// 背景色を設定
frameBuffer.BackgroundColor = Color.White;       // 白背景
frameBuffer.BackgroundColor = Color.Black;       // 黒背景
frameBuffer.BackgroundColor = Color.Transparent; // 透明背景（デフォルト）
```

## 自動レンダリングの制御

`AutoRender` を `false` にすると、毎フレームの自動レンダリングを停止できます。この場合、`Render()` を手動で呼び出すことで任意のタイミングでレンダリングできます。

```csharp
var frameBuffer = new FrameBuffer(256, 256);

// 自動レンダリングを無効化（毎フレーム更新しない）
frameBuffer.AutoRender = false;

// 手動でレンダリングを実行
frameBuffer.Render();
```

`AutoClear` を `false` にすると、レンダリング前のクリアを省略できます。前フレームの内容に追記する場合などに使用します。

```csharp
// レンダリング前のクリアを無効化
frameBuffer.AutoClear = false;
```

## ノート

### バックエンドサポート

フレームバッファはすべてのバックエンドでサポートされているわけではありません。使用前に`App.IsFrameBufferSupported`で確認してください。

```csharp title="サポート確認"
if (!App.IsFrameBufferSupported)
{
    Console.WriteLine("このバックエンドはフレームバッファをサポートしていません");
    return;
}

var frameBuffer = new FrameBuffer(256, 256);
```

### メモリ管理

フレームバッファは`IDisposable`を実装しています。不要になったら`Dispose()`を呼んでリソースを解放してください。

```csharp title="メモリ管理"
public class GameScene : Scene
{
    private FrameBuffer _frameBuffer;

    public override void OnStart()
    {
        _frameBuffer = new FrameBuffer(256, 256);
    }

    public override void OnDestroy()
    {
        _frameBuffer?.Dispose();
    }
}
```
