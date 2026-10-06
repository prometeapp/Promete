---
title: Text
description: テキストを表示するTextノードの使用方法について解説します。
sidebar:
  order: 5
---

`Text`は、文字列を画面に表示するためのノードです。フォント、色、配置、ボーダーなど、豊富なテキスト表示オプションを提供し、ゲームやアプリケーションでのテキスト表示に必要な機能を備えています。

## 作成

```csharp title="基本的なテキストの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // シンプルなテキスト（デフォルトフォント、白色）
        var text = new Text("Hello, World!");
        Root.Add(text);

        // フォントとカラーを指定
        var styledText = new Text("Styled Text", Font.GetDefault(24), Color.Red)
            .Location(10, 50);
        Root.Add(styledText);
    }
}
```

## テキストのプロパティ

### 基本プロパティ

#### テキスト内容
```csharp title="テキスト内容の設定と変更"
var text = new Text("初期テキスト");

// テキストを変更
text.Content = "新しいテキスト";

// 動的なテキスト更新
text.Content = $"スコア: {score}";
```

#### フォント
```csharp title="フォントの設定"
// デフォルトフォント
var defaultText = new Text("Default Font", Font.GetDefault());

// サイズ指定
var largeText = new Text("Large Text", Font.GetDefault(32));

// カスタムフォント
var customText = new Text("Custom Font", Font.FromFile("assets/MyFont.ttf", 24));
```

#### 色
```csharp title="テキストの色設定"
// 基本色
text.Color = Color.Red;
text.Color = Color.Blue;
text.Color = Color.Green;

// カスタムカラー
text.Color = Color.FromArgb(255, 200, 100, 50);

// 半透明
text.Color = Color.FromArgb(128, 255, 255, 255);
```

### ボーダー

テキストに縁取り（ボーダー）を追加できます。`BorderColor` を指定すると縁取りが有効になり、`BorderThickness` で太さを変更できます。

```csharp title="ボーダーの設定"
var outlinedText = new Text("Outlined Text");
outlinedText.BorderColor = Color.Black;
outlinedText.BorderThickness = 2;

// 太いボーダー
var thickBorderText = new Text("Thick Border");
thickBorderText.Color = Color.White;
thickBorderText.BorderColor = Color.DarkBlue;
thickBorderText.BorderThickness = 4;

// 縁取りを無効にする
outlinedText.BorderColor = null;
```

### 配置とレイアウト

#### 水平配置
```csharp title="水平配置の設定"
// 左揃え（デフォルト）
text.HorizontalAlignment = HorizontalAlignment.Left;

// 中央揃え
text.HorizontalAlignment = HorizontalAlignment.Center;

// 右揃え
text.HorizontalAlignment = HorizontalAlignment.Right;
```

#### 垂直配置
```csharp title="垂直配置の設定"
// 上揃え（デフォルト）
text.VerticalAlignment = VerticalAlignment.Top;

// 中央揃え
text.VerticalAlignment = VerticalAlignment.Center;

// 下揃え
text.VerticalAlignment = VerticalAlignment.Bottom;
```

#### サイズと折り返し

`PreferredSize` でテキストを配置する領域の大きさを、`WrapMode` で折り返し方を指定します。

```csharp title="サイズと折り返し"
var longText = new Text("これは非常に長いテキストで、指定された幅で自動的に折り返されます。");
longText.PreferredSize = (200, 100);  // 200x100の領域に表示
longText.WrapMode = WrapMode.Mixed;   // 自動折り返しを有効化

Root.Add(longText);
```

`WrapMode` には次の値を指定できます。

- `WrapMode.None`<br/>折り返しません（デフォルト）。改行文字の位置でのみ改行されます
- `WrapMode.Character`<br/>幅を超えた位置の文字で折り返します。単語の途中でも改行されます
- `WrapMode.Word`<br/>単語の区切りで折り返します。英語のテキストに向いています
- `WrapMode.Mixed`<br/>日本語は任意の文字で、英語は単語の区切りで折り返します。日本語と英語が混ざったテキストに向いています

`PreferredSize` を `(0, 0)` にすると、テキストが収まる大きさに自動で調整されます。

#### 行間隔
```csharp title="行間隔の調整"
var multilineText = new Text("Line 1\nLine 2\nLine 3");
multilineText.LineSpacing = 1.5f;  // 1.5倍の行間隔
```

## リッチテキスト

部分的に文字装飾が可能です。[リッチテキスト機能](/guide/text/ptml)を参照してください。

```csharp title="リッチテキストの使用"
var richText = new Text("", Font.GetDefault(16));
richText.UseRichText = true;

// PTMLタグを使用
richText.Content = """
    <color=red>赤い文字</color> と <color=blue>青い文字</color>
    <size=24>大きな文字</size> と <size=12>小さな文字</size>
    <b>太字</b> と <i>斜体</i>
    """;
```
