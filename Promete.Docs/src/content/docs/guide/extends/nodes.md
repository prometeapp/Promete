---
title: カスタムノード
description: Prometeで独自のノードを作成し、シーンに追加する方法を解説します。
sidebar:
  order: 1
---

既存のノードだけでは表現が足りない場合や、独自の描画処理を行いたい場合などに、独自のノード（描画要素やロジック要素）を簡単に作成し、シーンに追加できます。
ここでは、基本的なカスタムノードの作り方と、シーンでの利用例を解説します。

ノードは `Node` クラスを継承して作成します。
必要に応じて `OnUpdate` などのメソッドをオーバーライドして、独自の動作を実装します。

```csharp
using Promete.Nodes;

public class MyNode : Node
{
    protected override void OnUpdate()
    {
        // 毎フレームの処理
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

独自の描画処理を行うには、`Collect()` メソッドをオーバーライドして、レンダリングコマンドをキューに追加します。詳細は[カスタムノードレンダラー](/guide/extends/renderers)のページを参照してください。

## レイアウトの変更を通知する `v2.4~`

`Container` の `AutoSize` のように、子ノードの位置や大きさに依存して動作するコンテナがあります。
これらは、子ノードから通知を受けたときに再計算を行います。

`Location` / `Size` / `Scale` / `Angle` / `Pivot` など、`Node` が標準で持つプロパティは、値が変わると自動で通知されます。
一方、カスタムノードが独自に追加した、**サイズや描画範囲に影響するプロパティ**は、値が変わったときに `NotifyLayoutChanged()` を呼んでください。

```csharp
public class Gauge : Node
{
    private int _max = 100;

    public int Max
    {
        get => _max;
        set
        {
            if (_max == value)
                return;
            _max = value;
            Size = (value * 2, 8); // Node 標準の Size は、代入すれば自動で通知される
        }
    }
}
```

`Size` をオーバーライドして独自に管理する場合は、値が変わったときに自分で通知します。

```csharp
public class Icon : Node
{
    private VectorInt _size;

    public override VectorInt Size
    {
        get => _size;
        set
        {
            if (_size == value)
                return;
            _size = value;
            NotifyLayoutChanged();
        }
    }
}
```

内容によってサイズが変わる場合 (画像の差し替え、テキストの変更など) も同様です。通知を忘れると `AutoSize` などが古いサイズのままになります。

