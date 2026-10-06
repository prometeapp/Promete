---
title: PTML（リッチテキスト）
description: Prometeのリッチテキスト（PTML）機能の使い方・タグ一覧・サンプル・注意点を解説します。
sidebar:
  order: 3
---

Promete独自のリッチテキスト記法「PTML（Promete Text Markup Language）」を用いると、テキストに装飾を加えることができます。
PTMLを使うことで、テキストの一部に色やサイズ、太字・斜体などの装飾を簡単に適用できます。

ここでは、`Text`ノードでサポートされているタグを紹介します。

## 記法
PTMLは、HTMLのようなタグベースの記法を採用しています。開始タグ`<tag>`と終了タグ`</tag>`でテキストを囲むことで、そのテキストを装飾します。タグには `=` で属性を指定することもできます。

`<tex=名前>` のように、終了タグを持たない単独のタグもあります。

```html
<color=red>赤い文字</color>
<size=24>大きな文字</size>
<b>太字</b>
<i>斜体</i>
<tex=heart>
```

## Textノードでの使い方

`Text`ノードの`UseRichText`プロパティを`true`に設定し、`Content`にPTMLタグを含む文字列を指定します。

```csharp
var text = new Text("", Font.GetDefault(16));
text.UseRichText = true;

text.Content = """
<color=red>赤い文字</color> と <color=blue>青い文字</color>
<size=24>大きな文字</size> と <size=12>小さな文字</size>
<b>太字</b> と <i>斜体</i>
""";
```

## サポートされるタグ
`Text` ノードでサポートされているタグです。

| タグ         | 属性例         | 効果                         |
|--------------|---------------|------------------------------|
| `<b>...</b>` | なし          | 太字                         |
| `<i>...</i>` | なし          | 斜体                         |
| `<color=red>...</color>` | 色名/HTMLカラー | 文字色を変更                 |
| `<size=24>...</size>`    | 数値          | フォントサイズを変更         |
| `<tex=heart>`            | 外字の名前    | 名前で登録した外字を1文字として差し込む（終了タグなし） |

- 色名は `"red"`, `"blue"`, `"black"` などのHTML標準名、または `#RRGGBB` 形式が使えます。
- タグは入れ子にできます。

## 外字を差し込む

`<tex=名前>` を使うと、名前を付けて登録した画像（外字）を本文の中に1文字として差し込めます。アイコンや記号を文章中に表示したいときに便利です。

`<tex>` は終了タグを持たない単独のタグです。`</tex>` は書きません。

外字は `BitmapGlyphSource` に名前を付けて登録し、`Font.WithOverride` で本文のフォントに重ねて使います。

```csharp
// 外字を登録する（16 は外字画像の本来の高さ）
var icons = new BitmapGlyphSource(16);
icons.Register("heart", "assets/heart.png");
icons.Register("coin", "assets/coin.png");

// 本文のフォントに外字を重ねる
var text = new Text("", Font.GetDefault(16).WithOverride(icons));
text.UseRichText = true;
text.Content = "ライフ <tex=heart><tex=heart><tex=heart>\n所持金 <tex=coin> 999";
```

- 差し込んだ外字は通常の文字と同じように扱われ、折り返しや整列の対象になります
- 登録されていない名前を指定した場合、その位置には何も描画されません
- 外字は元の画像の大きさの整数倍で拡大されるため、フォントサイズも外字画像の高さの倍数にするときれいに表示されます
- `BitmapGlyphSource` は使い終わったら `Dispose()` で破棄してください

グリフソースについて詳しくは[カスタムグリフソース](/guide/extends/font)を参照してください。

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

## ノート

- タグの書式ミスや未対応のタグは無視されるか、エラーになります。
- 入れ子のタグや複数属性の同時指定は一部制限があります。
- PTMLは描画時にパースされるため、動的なテキストにも利用できます。
