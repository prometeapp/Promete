---
title: UI要素
description: Button、Checkbox、Slider、TextInput、Panel、ScrollViewの各UI要素の使い方を解説します。
sidebar:
  order: 2
---

Promete の UIライブラリには、以下のUI要素が用意されています。

## Button

クリック可能なボタンです。テキストラベルを表示し、マウスクリックやキーボード（Enter/Space）で押下できます。

```csharp
var button = new Button("保存")
    .Location(100, 100)
    .Size(120, 40)
    .OnClick(() => Save());

Root.Add(button);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `TextContent` | `string` | ボタンのテキスト |
| `Label` | `Text` | 内部の Text ノード（色やフォントを直接操作可能） |
| `Style` | `IUIStyle<Button>` | 適用するスタイル |

## Checkbox

チェックボックスです。クリックで ON/OFF をトグルします。

```csharp
var checkbox = new Checkbox("サウンドを有効にする", isChecked: true)
    .Location(100, 200)
    .Size(250, 24);

checkbox.CheckedChanged += isChecked =>
{
    Console.WriteLine($"サウンド: {(isChecked ? "ON" : "OFF")}");
};

Root.Add(checkbox);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `IsChecked` | `bool` | チェック状態（外部から設定も可能） |
| `LabelText` | `string` | ラベルテキスト |
| `BoxSize` | `int` | チェックボックスのサイズ（デフォルト: 20） |

## Slider

ドラッグや左右キーで値を変更するスライダーです。

```csharp
var slider = new Slider(minimum: 0, maximum: 100, value: 50)
    .Location(100, 300)
    .Size(200, 28);

slider.ValueChanged += value =>
{
    Console.WriteLine($"音量: {value}");
};

Root.Add(slider);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Value` | `float` | 現在の値（外部から設定も可能） |
| `Minimum` / `Maximum` | `float` | 値の範囲 |
| `IntegerOnly` | `bool` | `true` の場合、値を整数に丸める（デフォルト: `true`） |
| `Step` | `float` | キーボード操作時の変化量（デフォルト: 1） |
| `NormalizedValue` | `float` | 0〜1 に正規化された値（読み取り専用） |
| `ThumbSize` | `VectorInt` | つまみのサイズ |
| `TrackHeight` | `int` | トラックの高さ |

:::tip
`IntegerOnly = false` にすると、小数値を扱えます。音量のように0.0〜1.0の範囲で使う場合に便利です。
:::

## TextInput

テキスト入力フィールドです。フォーカス中にキーボード入力を受け付けます。

```csharp
var input = new TextInput("名前を入力...")
    .Location(100, 400)
    .Size(250, 34);

input.TextChanged += text =>
{
    Console.WriteLine($"入力中: {text}");
};

input.Submitted += text =>
{
    Console.WriteLine($"確定: {text}");
};

Root.Add(input);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Text` | `string` | 入力テキスト（外部から設定も可能） |
| `Placeholder` | `string` | プレースホルダーテキスト |
| `CursorPosition` | `int` | カーソル位置 |
| `PaddingLeft` | `int` | テキストの左パディング |
| `CursorBlinkInterval` | `int` | カーソルの点滅間隔（フレーム数） |

### キーボード操作

| キー | 動作 |
|---|---|
| 文字キー | テキスト入力 |
| BackSpace | カーソル前の1文字を削除 |
| Delete | カーソル後の1文字を削除 |
| ← / → | カーソル移動 |
| Home / End | 先頭/末尾に移動 |
| Enter | `Submitted` イベント発火 |

## Panel

背景付きのコンテナです。UI要素のグループ化に使います。デフォルトではフォーカス不可です。

```csharp
var panel = new Panel()
    .Location(100, 100)
    .Size(300, 200);

var label = new Text("パネル内のテキスト", color: Color.White)
    .Location(10, 10);
panel.Add(label);

var button = new Button("OK")
    .Location(10, 50)
    .Size(100, 30);
panel.Add(button);

Root.Add(panel);
```

## ScrollView

スクロール可能なコンテナです。コンテンツがビューポートを超えた場合に、マウスホイールやドラッグでスクロールできます。内部的には `Container.IsTrimmable` によるクリッピングを利用しています。

```csharp
var scrollView = new ScrollView { ContentSize = (300, 800) };
scrollView
    .Location(100, 100)
    .Size(300, 200);

// Content コンテナに子ノードを追加する
for (var i = 0; i < 20; i++)
{
    var item = new Text($"アイテム {i + 1}", color: Color.White)
        .Location(10, i * 35);
    scrollView.Content.Add(item);
}

Root.Add(scrollView);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `ContentSize` | `VectorInt` | コンテンツの論理サイズ |
| `Content` | `Container` | 子要素を追加するコンテナ |
| `ScrollSpeed` | `float` | ホイールスクロール速度（ピクセル/ノッチ） |
| `HorizontalScrollEnabled` | `bool` | 水平スクロールの有効/無効 |
| `VerticalScrollEnabled` | `bool` | 垂直スクロールの有効/無効 |

:::note
子ノードは `scrollView.Content.Add()` で追加してください。`scrollView.Add()` ではなく `Content` プロパティを経由する点に注意してください。
:::

## Modal

モーダルダイアログです。表示中はモーダル外の要素への操作がブロックされます。

```csharp
var modal = new Modal((300, 200));
modal.Size = View.Size;  // オーバーレイを画面全体に

var label = new Text("本当に削除しますか？", color: Color.White)
    .Location(20, 20);
modal.Body.Add(label);

var okButton = new Button("OK")
    .Location(20, 140)
    .Size(100, 40)
    .OnClick(() => {
        Delete();
        modal.Close();
    });
modal.Body.Add(okButton);

var cancelButton = new Button("キャンセル")
    .Location(160, 140)
    .Size(120, 40)
    .OnClick(() => modal.Close());
modal.Body.Add(cancelButton);

// モーダルを表示
modal.Show(uiManager, Root);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Body` | `Panel` | モーダル本体のパネル（子要素はここに追加） |
| `CloseOnOverlayClick` | `bool` | オーバーレイクリックで自動的に閉じるか（デフォルト: `true`） |

### メソッド

| メソッド | 説明 |
|---|---|
| `Show(UIManager, Container)` | モーダルを表示し、UIManager のモーダルスタックにプッシュ |
| `Close()` | モーダルを閉じ、ノードツリーから除去 |

:::tip
`modal.Size` をウィンドウサイズに合わせると、半透明オーバーレイが画面全体を覆います。
:::
