---
title: カスタムノードレンダラー
description: Prometeで独自のノードレンダラーを実装し、描画処理を拡張する方法を解説します。
sidebar:
  order: 2
---

独自にノードを作成した場合は、そのノードを描画するためのロジックを「ノードレンダラー」として実装し、エンジンに登録する必要があります。
また、既存のノードを独自のレンダラーに差し替えることも可能です。

ノードレンダラーは `NodeRendererBase` を継承して作成し、`Render(Node node)` メソッドを実装します。
`node` 引数は描画対象のノードです。
必要に応じて型チェックやキャストを行い、描画処理を記述します。

ノードの描画処理は、バックエンド固有のものとなります。現状存在するOpenGLバックエンド向けのレンダラーの場合、Silk.NETのOpenGL機能を用いて描画を行う必要があります。詳しくは、Prometeのソースコードを参照してください。ここでは、OpenGLに関する解説は割愛します。

```csharp
using Promete.Nodes;
using Promete.Nodes.Renderer;

public class MyNodeRenderer : NodeRendererBase
{
    public override void Render(Node node)
    {
        if (node is not MyNode myNode) return;
        // myNodeの情報を使って独自の描画処理
    }
}
```
