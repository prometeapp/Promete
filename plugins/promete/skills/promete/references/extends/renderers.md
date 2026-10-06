---
title: カスタムノードのレンダリング
description: Prometeでカスタムノードに描画処理を実装する方法を解説します。
sidebar:
  order: 2
---

カスタムノードに描画処理を追加するには、`Node` クラスの `Collect()` メソッドをオーバーライドして、`RenderCommandQueue` にレンダリングコマンドを追加します。

## 基本的な使い方

`Collect()` メソッドは描画フェーズに呼び出されます。引数の `queue` にコマンドを追加することで描画を行います。

```csharp
using System.Drawing;
using Promete.Graphics;
using Promete.Nodes;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

public class MyNode(Texture2D texture) : Node
{
    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        queue.Enqueue(new DrawTextureCommand
        {
            Texture = texture,
            ModelMatrix = ModelMatrix, // 親の変形込みの変換行列
            TintColor = Color.White,
            Width = texture.Size.X,
            Height = texture.Size.Y,
            Material = Material,
        });
    }
}
```

## レンダリングコマンドの種類

| コマンド | 用途 |
|----------|------|
| `DrawTextureCommand` | テクスチャ描画（自動バッチング対応） |
| `DrawPrimitiveCommand` | プリミティブ図形（矩形・円・三角形）の描画 |
| `DrawPieTextureCommand` | 扇形テクスチャの描画 |
| `BeginTrimCommand` / `EndTrimCommand` | シザーテスト（描画範囲の制限） |
| `BeginStencilMaskCommand` / `EndMaskCommand` | ステンシルマスク |
| `BeginAlphaMaskCommand` / `EndMaskCommand` | アルファマスク |

各コマンドのプロパティは [レンダリングコマンドシステム](/guide/extends/rendercommand) を参照してください。

## RenderContext

`RenderContext` は描画時のコンテキスト情報を保持します。

```csharp
public override void Collect(RenderCommandQueue queue, RenderContext ctx)
{
    // ウィンドウの論理サイズと、物理ピクセル単位のサイズを参照できる
    var windowSize = ctx.WindowSize;
    var actualSize = (ctx.ActualWidth, ctx.ActualHeight);
}
```

## ノート

- `ModelMatrix` は、位置・回転・スケール・ピボットに親ノードの変形を合成した変換行列です。描画コマンドにはこれを渡します
- レンダリングはバックエンド固有の処理を直接書く必要はなく、コマンドをキューに追加するだけでエンジンが描画を行います
