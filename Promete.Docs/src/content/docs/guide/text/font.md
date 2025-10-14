---
title: フォント
description: フォントの基本・カスタムフォントの利用・主なAPIについて解説します。
sidebar:
  order: 2
---

Prometeでは、デフォルトで識別したOSにインストールされているデフォルトフォントを使用しますが、任意のTrueType/OpenTypeフォントファイル（.ttf/.otf）も簡単に読み込めます。

## フォントの基本

`Font` クラスは、テキストノードやコンソールレイヤーなどで利用されます。
フォントサイズ・スタイル・アンチエイリアスの有無などを指定できます。

```csharp
// デフォルトフォント（サイズ16）
var font = Font.GetDefault();

// サイズ指定
var font24 = Font.GetDefault(24);

// スタイル指定
var boldFont = Font.GetDefault(16, FontStyle.Bold);
```

## カスタムフォントの利用

プロジェクト内の .ttf ファイルなどを読み込んで利用できます。

```csharp
// ファイルからフォントを読み込む
var customFont = Font.FromFile("assets/JfDotShinonome14.ttf", 20);

// スタイルやアンチエイリアスも指定可能
var customFont2 = Font.FromFile("assets/Koruri.ttf", 24, FontStyle.Italic, false);
```

埋め込みリソースを `Stream` として読み込み、`Font.FromStream` で生成することも可能です。

## システムフォントの利用

OSにインストールされているフォント名を指定して利用することもできます。

```csharp
// Windows例
var sysFont = Font.FromSystem("Yu Gothic", 18);

// Mac例
var sysFontMac = Font.FromSystem("Hiragino Sans", 18);
```

## 主なAPI

- `Font.GetDefault(size, style, isAntialiased)`<br/>
  デフォルトフォントを取得します。
- `Font.FromFile(path, size, style, isAntialiased)`<br/>
  ファイルからフォントを生成します。
- `Font.FromSystem(fontFamily, size, style, isAntialiased)`<br/>
  システムフォント名から生成します。
- `With(size)` / `With(style)` / `With(size, style, isAntialiased)`<br/>
  既存のフォントからサイズやスタイルを変更した新しいフォントを生成します。

## サンプル：カスタムフォントでテキスト表示

```csharp
var font = Font.FromFile("assets/JfDotShinonome14.ttf", 24);
var text = new Text("カスタムフォント", font, Color.Black);
Root.Add(text);
```

## 注意点

- フォントファイルが存在しない場合は例外が発生します。
- システムフォント名は環境ごとに異なります。
  利用可能なフォント名は各OSの設定を参照してください。
- アンチエイリアスを無効にするとドットフォント風の表示も可能です。
