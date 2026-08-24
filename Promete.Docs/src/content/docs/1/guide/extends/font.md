---
title: カスタムフォントプロバイダー
description: Prometeで独自のフォントプロバイダー（IFont実装）を作成し、Textノードで利用する方法を解説します。
sidebar:
  order: 5
---

Prometeでは、標準のフォント以外にも独自のフォントプロバイダー（`IFont`実装）を作成してTextノードで利用できます。
ここではカスタムフォントプロバイダーの作り方と利用例を解説します。

## 基本の使い方

フォントプロバイダーは `IFont` インターフェースを実装して作成します。
必要に応じてテキスト描画やサイズ計算の処理を実装します。

```csharp
using Promete.Graphics.Fonts;
using Promete.Graphics;

public class MyFont : IFont
{
    public Rect GetTextBounds(string text, TextRenderingOptions options)
    {
        // テキストのサイズ計算処理
        return ...;
    }

    public Texture2D GenerateTexture(TextureFactory factory, string text, TextRenderingOptions options)
    {
        // テキストのテクスチャ生成処理
        return ...;
    }
}
```

## 主なAPI

- `GetTextBounds(text, options)`<br/>テキストのサイズ計算
- `GenerateTexture(factory, text, options)`<br/>テキストのテクスチャ生成

## サンプル：Textノードでの利用

```csharp
var font = new MyFont();
var text = new Text("Hello", font);
```

## ノート

- カスタムフォントプロバイダーはTextノードのコンストラクタで直接指定できます
- 標準のFontクラスを参考に実装すると便利です
- テクスチャ生成やサイズ計算の最適化も可能です
