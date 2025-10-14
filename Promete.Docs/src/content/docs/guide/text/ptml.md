---
title: PTML（リッチテキスト）
description: Prometeのリッチテキスト（PTML）機能の使い方・タグ一覧・サンプル・注意点を解説します。
sidebar:
  order: 3
---

Prometeは、独自のリッチテキスト記法「PTML（Promete Text Markup Language）」をサポートしています。
PTMLを使うことで、テキストの一部に色やサイズ、太字・斜体などの装飾を簡単に適用できます。

## 基本の使い方

`Text`ノードや`ConsoleLayer`で、`UseRichText(true)`を設定し、`Content`にPTMLタグを含む文字列を指定します。

```csharp
var text = new Text("", Font.GetDefault(16))
    .UseRichText(true);

text.Content = """
<color=red>赤い文字</color> と <color=blue>青い文字</color>
<size=24>大きな文字</size> と <size=12>小さな文字</size>
<b>太字</b> と <i>斜体</i>
""";
```

## サポートされるタグ

| タグ         | 属性例         | 効果                         |
|--------------|---------------|------------------------------|
| `<b>...</b>` | なし          | 太字                         |
| `<i>...</i>` | なし          | 斜体                         |
| `<color=red>...</color>` | 色名/HTMLカラー | 文字色を変更                 |
| `<size=24>...</size>`    | 数値          | フォントサイズを変更         |

- 色名は `"red"`, `"blue"`, `"black"` などのHTML標準名、または `#RRGGBB` 形式が使えます。
- タグは入れ子にできます。

## サンプル：PTMLエディタ

```csharp
// 入力文字列をPTMLとして解析し、装飾情報をダンプ表示
public override void OnUpdate()
{
    editorView.Content = buf.ToString();
    ptmlView.Content = buf.ToString();

    try
    {
        var (plainText, decorations) = PtmlParser.Parse(buf.ToString(), true);
        dumpView.Content = $"（デバッグビュー）\nプレーンテキスト：{plainText}\n\nダンプ：\n{string.Join('\n', decorations)}";
        dumpView.Color = Color.Lime;
    }
    catch (PtmlParserException e)
    {
        dumpView.Content = e.Message;
        dumpView.Color = Color.Red;
    }
}
```

## 注意点

- タグの書式ミスや未対応のタグは無視されるか、エラーになります。
- 入れ子のタグや複数属性の同時指定は一部制限があります。
- PTMLは描画時にパースされるため、動的なテキストにも利用できます。
