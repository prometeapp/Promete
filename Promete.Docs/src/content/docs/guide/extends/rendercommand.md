---
title: レンダリングコマンドシステム
description: v2のコマンドキューベースのレンダリングアーキテクチャを解説します。
sidebar:
  order: 3
---

v2 のレンダリングは **コマンドキュー方式** で動作します。各ノードはフレームごとに `Collect()` メソッドを呼ばれ、自身の描画内容をコマンドとして `RenderCommandQueue` に積みます。エンジンはその後、登録された `CommandRunner` を使って各コマンドを実行します。

```
ノードツリーの走査
    ↓
各ノードの Collect() 呼び出し
    ↓
RenderCommandQueue にコマンドが蓄積
    ↓
CommandRunner が各コマンドを実行（OpenGL描画など）
```

## Collect() の実装

カスタムノードに描画処理を追加するには `Collect()` をオーバーライドします。`ModelMatrix` プロパティは位置・回転・スケールを合成した変換行列で、親ノードの変形も含まれます。

```csharp
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

public class MyNode : Node
{
    public Texture2D? Texture { get; set; }
    public Color TintColor { get; set; } = Color.White;

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (Texture is not { } tex) return;

        queue.Enqueue(new DrawTextureCommand
        {
            Texture = tex,
            ModelMatrix = ModelMatrix,  // 親の変形込みの変換行列
            TintColor = TintColor,
            Width = Size.X,
            Height = Size.Y,
            Material = Material,        // null でデフォルトシェーダーを使用
        });
    }
}
```

## コマンドの種類

### DrawTextureCommand

テクスチャを描画します。連続する同一テクスチャ・同一マテリアルのコマンドは自動的にバッチにまとめられます。

```csharp
queue.Enqueue(new DrawTextureCommand
{
    Texture = tex,
    ModelMatrix = ModelMatrix,
    TintColor = Color.White,
    Width = Size.X,
    Height = Size.Y,
    Pivot = new Vector(0.5f, 0.5f),  // 描画オフセット（省略可）
    Material = material,              // 省略可、null でデフォルトシェーダー
});
```

### DrawPrimitiveCommand

矩形・円・三角形などのプリミティブを描画します。

```csharp
queue.Enqueue(new DrawPrimitiveCommand
{
    WorldVertices = vertices,     // ワールド座標頂点配列
    ShapeType = ShapeType.Rect,
    Color = Color.Red,
    LineWidth = 0,                // 0 で塗りつぶし
    LineColor = null,
});
```

### DrawPieTextureCommand

扇形のテクスチャを描画します。`PieSprite` で使用されます。

```csharp
queue.Enqueue(new DrawPieTextureCommand
{
    Texture = tex,
    ModelMatrix = ModelMatrix,
    TintColor = Color.White,
    Width = Size.X,
    Height = Size.Y,
    StartPercent = 0f,   // 開始位置（0〜1）
    Percent = 0.75f,     // 表示する割合（0〜1）
});
```

### シザーテスト（描画範囲のクリッピング）

`BeginTrimCommand` / `EndTrimCommand` で描画範囲を矩形に制限します。`Container` ノードの実装で使用されます。

```csharp
// トリム開始（物理ピクセル座標・左上原点で指定）
queue.Enqueue(new BeginTrimCommand
{
    X = physicalX,
    Y = physicalY,
    Width = physicalWidth,
    Height = physicalHeight,
});

// 描画コマンド...

// トリム終了（前の状態に戻す）
queue.Enqueue(new EndTrimCommand
{
    X = prevX, Y = prevY, Width = prevW, Height = prevH,
    WasEnabled = prevEnabled,
});
```

### マスクコマンド

`MaskedContainer` で使用されるマスク処理コマンドです。

| コマンド | 用途 |
|----------|------|
| `BeginStencilMaskCommand` | ステンシルバッファを使ったマスク開始 |
| `BeginAlphaMaskCommand` | アルファ値を使ったマスク開始 |
| `EndMaskCommand` | マスク終了 |

## RenderContext

`Collect()` の第2引数 `RenderContext` は描画時のコンテキスト情報を持ちます。

```csharp
public override void Collect(RenderCommandQueue queue, RenderContext ctx)
{
    // FrameBuffer内かどうかの確認
    bool inFrameBuffer = ctx.FrameBuffer is not null;
}
```

## CommandRunner の実装（バックエンド開発者向け）

カスタムコマンドを追加する場合は、`CommandRunner<T>` を継承して `Execute()` を実装し、`RenderCommandQueue` に登録します。

```csharp
public class MyCommandRunner : CommandRunner<MyCommand>
{
    public override void Execute(MyCommand command)
    {
        // コマンドの実行処理
    }
}

// バックエンドの初期化時に登録
app.GetPlugin<RenderCommandQueue>().RegisterRunner(new MyCommandRunner());
```

## 関連項目

- [カスタムノード](/guide/extends/nodes) - カスタムノードの作成
- [カスタムノードのレンダリング](/guide/extends/renderers) - Collect() の基本的な使い方
- [カスタムバックエンド](/guide/extends/backend) - バックエンドの作成
