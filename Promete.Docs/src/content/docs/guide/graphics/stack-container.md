---
title: StackContainer
description: 子ノードを縦または横に自動で積み重ねるStackContainerの使用方法について解説します。
sidebar:
  order: 9
---

`StackContainer` は、子ノードを縦または横に並べて、自動で配置する `Container` です。`v2.4~`
メニューの項目、アイコンの列、ステータス表示など、「順番に並べるだけ」のレイアウトで、座標を手計算する必要がなくなります。

## 基本的な使い方

並べる方向と、子ノード同士の間隔 (`gap`) を指定して作成します。
`Container` と同じく、コレクション初期化子で子ノードを追加できます。

```csharp title="縦に並べる"
var stack = new StackContainer(StackDirection.Vertical, gap: 3)
{
    new Sprite(texture1),
    new Sprite(texture2),
    new Sprite(texture3),
};

Root.Add(stack);
```

`StackDirection` には、次の2つがあります。

- `Vertical`: 上から下へ、縦に並べます
- `Horizontal`: 左から右へ、横に並べます

子ノードは、追加した順に並びます。
子ノードの `Location` は `StackContainer` が自動で設定するので、自分で設定しても上書きされます。

## 余白と揃え位置

### 周囲の余白 (Padding)

`Padding` で、`StackContainer` の周囲に余白を設けられます。

```csharp title="Padding の指定"
var stack = new StackContainer(StackDirection.Vertical, gap: 2, padding: 8)
{
    new Text("HP"),
    new Text("MP"),
};

stack.Padding = new Padding(8, 4, 8, 4); // 左, 上, 右, 下
stack.Padding = new Padding(8, 4); // 左右 8、上下 4
```

### 揃え位置 (Align)

並べる方向と直交する方向の揃え位置を、`Align` で指定します。
縦に並べる場合は左右、横に並べる場合は上下の揃え位置になります。

```csharp title="中央揃え"
var stack = new StackContainer(StackDirection.Vertical, align: StackAlignment.Center)
{
    new Sprite(wideTexture),
    new Sprite(narrowTexture),
};
```

- `Start` (既定): 左端 (縦に並べる場合) / 上端 (横に並べる場合)
- `Center`: 中央
- `End`: 右端 (縦に並べる場合) / 下端 (横に並べる場合)

## サイズ

`StackContainer` の `Size` は、常に自動で決まります。
並べた子ノードがすべて収まる大きさに、`Padding` を加えたものです。これは [Container の `AutoSize`](/guide/graphics/container#サイズの自動調整-v24) が常に有効な状態と同じです。

```csharp
Console.WriteLine(stack.Size);
```

子ノードの追加・削除や、子ノードのサイズ・スケール・回転などを変更すると、並び順と `Size` が自動的に更新されます。
`StackContainer` を別の `StackContainer` に入れて、入れ子にすることもできます。

:::note
並べ直しが実際に反映されるのは、`Size` を読み取ったときと、次の `Update`・描画の直前です。
子ノードを追加した直後に、子ノードの `Location` や `AbsoluteLocation` を読むと、まだ古い値の場合があります。
:::

## 子ノードの回転・スケール・ピボット

配置には、子ノードの回転・スケール・ピボットを含めた外接矩形を使います。
たとえば、ピボットを中央に設定したスプライトや、回転したスプライトでも、見た目の矩形が重ならずに並びます。
