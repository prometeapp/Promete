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
using Promete.Nodes;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

public class MyNode : Node
{
    private Texture2D _texture;

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        queue.Enqueue(new DrawTextureCommand
        {
            Texture = _texture,
            Location = AbsoluteLocation,
            Scale = AbsoluteScale,
            Angle = AbsoluteAngle,
            Size = Size,
            TintColor = Color.White,
            ZIndex = AbsoluteZIndex,
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
| `BeginStencilMaskCommand` / `EndStencilMaskCommand` | ステンシルマスク |
| `BeginAlphaMaskCommand` / `EndAlphaMaskCommand` | アルファマスク |

## RenderContext

`RenderContext` は描画時のコンテキスト情報を保持します。

```csharp
public override void Collect(RenderCommandQueue queue, RenderContext ctx)
{
    // 現在のビューポートサイズなどの情報を参照できる
    var viewportSize = ctx.FrameBuffer?.Size ?? ctx.ViewportSize;
}
```

## ノート

- `AbsoluteLocation`・`AbsoluteScale`・`AbsoluteAngle` などの `Absolute*` プロパティを使用することで、親ノードの変形が反映された座標を取得できます
- レンダリングはバックエンド固有の処理を直接書く必要はなく、コマンドをキューに追加するだけでエンジンが描画を行います
