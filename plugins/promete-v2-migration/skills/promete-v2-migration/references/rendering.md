# 描画

## ピクセルスナップ

v2 では、ノードの描画位置が既定で整数ピクセルにスナップされる。奇数サイズのノードを `Pivot(0.5f, 0.5f)` で中央に置いたときなどのにじみを防ぐため。

サブピクセル単位で滑らかに移動・回転・ズームするノード (カメラ追従、イージングでゆっくり動く UI、拡大縮小アニメーションなど) はカクつくことがある。その場合はノードごとに無効化する。

```csharp
var sprite = new Sprite(texture).PixelSnap(false);
// または
sprite.IsPixelSnapEnabled = false;
```

スナップの設定は子に継承されない。各ノードが、親の変形を含めた自分の最終的な位置を個別に丸める。そのため、カメラ用の `Container` だけを `PixelSnap(false)` にしても、子は丸められたまま。実際に描画される子孫ノードの側で無効化する。

無効化するかどうかは見た目の判断になるため、候補となるノードを洗い出してユーザーに伝える。勝手に全ノードで無効化しない。

## LoadSpriteSheet のテクスチャ共有

v2 の `TextureFactory.LoadSpriteSheet` は、画像を 1 枚のテクスチャ (アトラス) として読み込み、全セルがその GL ハンドルを共有する。v1 はセルごとに独立したテクスチャだった。

- セルを 1 つ `Dispose` すると、全セルが使えなくなる
- セルごとに `Dispose` していたコード (`foreach (var t in cells) t.Dispose();` など) は、2 回目以降が同じハンドルの二重解放になる。どれか 1 つを 1 回だけ `Dispose` する形に直す
- 一部のセルだけを破棄して残りを使い続けるコードは成り立たない。必要なら、使い続けるセルを `LoadSpriteSheet` 以外の方法で読み込み直す

## NodeRenderer から Collect() への移行

v1 では各ノード型に対して `NodeRendererBase` を継承したレンダラーを実装し、`UseRenderer<TNode, TRenderer>()` で登録していた。この仕組みは v2 で廃止された。

v2 では、ノード自身が `Collect()` をオーバーライドして描画コマンドをキューに積む。キューのコマンドは、バックエンドが登録したコマンドランナーが実行する。同じテクスチャ・同じマテリアルの `DrawTextureCommand` が連続すると、自動的にまとめて描画される。

```csharp
// v1
public class MyNode : Node { /* ... */ }

public class MyNodeRenderer : NodeRendererBase
{
    public override void Render(Node node) { /* ... */ }
}

var app = PrometeApp.Create()
    .UseRenderer<MyNode, MyNodeRenderer>()
    .BuildWithOpenGLDesktop();

// v2
using System.Drawing;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;

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

`UseRenderer<,>()` の登録コードはすべて削除する。v1 のレンダラーで直接 OpenGL を呼んでいた処理は、標準のコマンドで表現できるか検討し、できなければ下の「独自の描画コマンド」で実装する。

標準のコマンド (いずれも `Promete.Graphics.Rendering.Commands` 名前空間):

| コマンド | 用途 |
| --- | --- |
| `DrawTextureCommand` | テクスチャの描画 |
| `DrawPrimitiveCommand` | 図形の描画 |
| `DrawPieTextureCommand` | 扇形に切り抜いた描画 |
| `BeginTrimCommand` / `EndTrimCommand` | 矩形での切り抜き |
| `BeginStencilMaskCommand` / `BeginAlphaMaskCommand` / `EndMaskCommand` | マスク |

## コンテナ系のノード

- v1 の `PrometeApp.RenderNode(Node)` は削除された。子ノードの描画には `PrometeApp.CollectNode(Node, RenderCommandQueue, RenderContext)` を使う。ただし `ContainableNode` の `Collect()` は子ノードを自動で収集するので、通常は `base.Collect(queue, ctx)` を呼べば足りる
- `ContainableNode` の `isTrimmable` / `sortedChildren` は `IsTrimmable` / `SortedChildren` に名前が変わった
- `IsTrimmable` によるトリミングを処理するのは `Container` / `MaskedContainer` の `Collect()`。`ContainableNode` を直接継承してトリミングする場合は、子ノードの収集を `queue.PushTrim(this, ctx)` / `queue.PopTrim()` で囲む

## 独自の描画コマンド

標準のコマンドで表現できない描画は、`IRenderCommand` を実装したコマンドと、それを実行する `CommandRunner<T>` を作り、`RenderCommandQueue` に登録する。

```csharp
public sealed class MyCommand : IRenderCommand
{
    public required int Value { get; init; }
}

public sealed class MyCommandRunner : CommandRunner<MyCommand>
{
    public override void Execute(MyCommand command) { /* 描画処理 */ }
}

// アプリのビルド後に登録する
app.GetPlugin<RenderCommandQueue>().RegisterRunner(new MyCommandRunner());
```

ランナーが登録されていないコマンドは、何もせず読み飛ばされる。
