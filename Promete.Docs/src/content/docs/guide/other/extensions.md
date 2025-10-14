---
title: 便利な拡張メソッド
description: PrometeのRandomExtension/StringExtensionによる便利な拡張メソッド・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 2
---

Prometeには、乱数や文字列操作などを簡単にする拡張メソッドが用意されています。
ゲーム開発でよく使うユーティリティを効率よく記述できます。

## 基本の使い方

```csharp
// ランダムな色を生成
var color = new Random().NextColor();

// ランダムな座標を生成
var v = new Random().NextVector(100, 100);

// 文字列の一部を置換
var s = "abcdef".ReplaceAt(2, "ZZ"); // "abZZef"
```

## 主なAPI

- `Random.NextColor(max)`<br/>ランダムなColorを生成
- `Random.NextVector(xMax, yMax)`<br/>ランダムなVectorを生成
- `Random.NextVectorInt(xMax, yMax)`<br/>ランダムなVectorIntを生成
- `Random.NextVectorFloat(xMax, yMax)`<br/>小数も含むランダムなVectorを生成
- `string.ReplaceAt(index, replace)`<br/>指定位置から部分文字列を置換

## サンプル：ランダムな位置にオブジェクトを配置

```csharp
var rand = new Random();
for (int i = 0; i < 10; i++)
{
    var pos = rand.NextVector(800, 600);
    // posの位置にオブジェクトを生成
}
```

## 注意点

- NextColorのmaxは0～255の範囲で指定
- ReplaceAtは範囲外の場合自動で空白を補完
