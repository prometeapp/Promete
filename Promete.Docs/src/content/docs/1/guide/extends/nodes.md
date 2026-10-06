---
title: カスタムノード
description: Prometeで独自のノードを作成し、シーンに追加する方法を解説します。
sidebar:
  order: 1
---

既存のノードだけでは表現が足りない場合や、独自の描画処理を行いたい場合などに、独自のノード（描画要素やロジック要素）を簡単に作成し、シーンに追加できます。
ここでは、基本的なカスタムノードの作り方と、シーンでの利用例を解説します。

ノードは `Node` クラスを継承して作成します。
必要に応じて `OnUpdate` や `OnRender` などのメソッドをオーバーライドして、独自の動作や描画を実装します。

```csharp
using Promete.Nodes;

public class MyNode : Node
{
    protected override void OnUpdate()
    {
        // 毎フレームの処理
    }

    protected override void OnRender()
    {
        // 独自の描画処理
    }
}
```

作成したら、シーン上で他のノードと同様に追加して利用します。

```csharp
public class MainScene : Scene
{
    public override void OnStart()
    {
        var node = new MyNode();
        Root.Add(node);
    }
}
```

## レンダリング

純粋な `Node` クラスから派生した場合、ノードレンダラーを独自に実装して登録する必要があります。詳細は[カスタムノードレンダラー](/guide/extends/renderers)のページを参照してください。
