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
| `Rows` | `int` | 表示する行数（デフォルト: `1`）。`2` 以上にすると複数行の入力欄になる |
| `PaddingLeft` | `int` | テキストの左右のパディング |
| `PaddingTop` | `int` | 複数行のときの、テキストの上下のパディング（デフォルト: `6`） |
| `WrapMode` | `WrapMode` | 複数行のときの折り返し方（デフォルト: `Mixed`） |
| `KinsokuMode` | `KinsokuMode` | 複数行のときの禁則処理（デフォルト: `Standard`） |
| `CursorBlinkInterval` | `float` | カーソルの点滅間隔（秒）。既定値は 0.5 |

### キーボード操作

| キー | 動作 |
|---|---|
| 文字キー | テキスト入力 |
| BackSpace | カーソル前の1文字を削除 |
| Delete | カーソル後の1文字を削除 |
| ← / → | カーソル移動 |
| Home / End | 先頭/末尾に移動（複数行では、カーソルがある行の先頭/末尾） |
| Enter | `Submitted` イベント発火（複数行では改行） |

### 複数行の入力

`Rows` に 2 以上を指定すると、複数行の入力欄になります。高さは、指定した行数が収まる大きさに自動で決まります。

```csharp
var memo = new TextInput("メモを入力...") { Rows = 5 }
    .Location(100, 400)
    .Size(300, 0);  // 幅だけを指定 (高さに 0 を渡すと、Rows から決まる)

memo.Submitted += text =>
{
    Console.WriteLine($"確定: {text}");
};
```

複数行では、操作が次のように変わります。

| キー | 動作 |
|---|---|
| Enter | 改行を入力 |
| Ctrl + Enter | `Submitted` イベント発火 |
| ↑ / ↓ | カーソルを1行上/下へ移動（フォーカスは移動しない） |
| Home / End | カーソルがある行の先頭/末尾に移動 |

- 長い行は、入力欄の幅に合わせて自動で折り返されます。折り返し方と禁則処理は、`WrapMode` と `KinsokuMode` で指定できます（[Text](/guide/graphics/text/) と同じです）。1行の入力欄では折り返しません。
- 行数が `Rows` を超えると、カーソルのある行が見えるように、縦にスクロールします。表示範囲からはみ出した部分は、切り抜かれます。
- ↑ / ↓ でカーソルを動かすとき、短い行を通っても、元の桁の位置を覚えています。
- 高さは、`Size` で指定しない（または `0` を指定する）と、`Rows` から決まります。`Size` で高さを指定した場合はその高さが優先され、収まる行数だけが表示されます。

:::note
複数行の入力欄は、フォーカスを移動するときは Tab / Shift+Tab を使ってください。↑ / ↓ はカーソルの移動に使われます。
:::

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

## Popup

アンカー（ドロップダウン本体やボタンなど）の近くに表示するポップアップです。最前面に表示され、画面からはみ出さないように位置が自動で調整されます。

```csharp
var popup = new Popup();
popup.Add(new Button("コピー").Size(120, 32));

// ボタンの下に表示する
popup.Show(uiManager, anchorButton);
```

子ノードを追加して中身を作ります。サイズは中身に合わせて自動で決まります。複数の項目を並べるときは [`StackContainer`](/guide/graphics/stack-container/) が便利です。

```csharp
var popup = new Popup();
popup.Add(new StackContainer(StackDirection.Vertical, gap: 4, padding: 8)
{
    new Button("コピー").Size(120, 32),
    new Button("貼り付け").Size(120, 32),
});
```

### 表示位置

既定ではアンカーの下に、アンカーの左端に揃えて表示します。下に収まらないときは上に反転します。上下どちらにも収まらないときは、空きが広い方に表示します。左右にはみ出す場合は、画面内に収まるようにずらします。

アンカーが移動（スクロールなど）すると、ポップアップも追従します。ウィンドウサイズが変わった場合も、位置が調整されます。

```csharp
popup.Side = PopupSide.Right;     // アンカーの右に出す（収まらなければ左）
popup.Align = PopupAlign.Center;  // アンカーの中央に揃える
popup.Gap = 4;                    // アンカーとの間隔
```

画面上の任意の位置を基準にする場合は、`ShowAt` を使います。

```csharp
popup.ShowAt(uiManager, mouse.Position);
```

### 閉じ方

- ポップアップの外側をクリックすると閉じます。このクリックは、下にある要素には届きません（アンカーをもう一度クリックすると、閉じるだけです）。
- Esc キーで閉じます。
- `Close()` で閉じます。

閉じたときは、表示する前にフォーカスしていた要素にフォーカスが戻ります。

アンカーがノードツリーから外れると、自動で閉じます。

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Side` | `PopupSide` | 優先する表示位置（デフォルト: `Bottom`） |
| `Align` | `PopupAlign` | アンカーに対する揃え位置（デフォルト: `Start`） |
| `Gap` | `float` | アンカーとの間隔 |
| `CloseOnOutsideClick` | `bool` | 外側のクリックで閉じるか（デフォルト: `true`） |
| `CloseOnCancel` | `bool` | Esc で閉じるか（デフォルト: `true`） |
| `FocusTarget` | `UIElement?` | 表示時にフォーカスを移す要素 |
| `AvailableSize` | `Vector` | 現在の表示位置に収まる最大のサイズ |
| `IsOpen` | `bool` | 表示中かどうか |

### メソッドとイベント

| 名前 | 説明 |
|---|---|
| `Show(UIManager, UIElement)` | アンカーの近くに表示 |
| `ShowAt(UIManager, Vector)` | 画面上の位置を基準に表示 |
| `Close()` | 閉じる |
| `Opened` / `Closed` | 表示された / 閉じられたときに発火 |

:::note
中身が `AvailableSize` を超える場合、画面からはみ出します。項目が多い場合は、[`ScrollView`](#scrollview) で包んで高さを制限してください。
:::

:::note
シーン側で Esc を別の用途（シーンの終了など）に使っている場合、ポップアップを閉じた Esc でシーンも終了してしまいます。`uiManager.IsCancelConsumed` が `true` のフレームは、Esc を無視してください。

```csharp
if (keyboard.Escape.IsKeyDown && !uiManager.IsCancelConsumed)
    App.LoadScene<MainScene>();
```
:::

## Dropdown

項目の一覧から1つを選ぶドロップダウンです。クリック、またはフォーカス中の Enter / Space で、項目の一覧を [`Popup`](#popup) として開きます。

```csharp
var language = new Dropdown("言語を選択")
    .Location(30, 60)
    .Size(240, 34);

language.AddItem("日本語").AddItem("English").AddItem("한국어");
language.SelectedIndex = 0;

language.SelectionChanged += index =>
{
    Console.WriteLine($"選ばれた項目: {language.SelectedItem}");
};

Root.Add(language);
```

一覧は、ドロップダウンの下に表示されます。画面の下に収まらないときは、上に表示されます。

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Items` | `IList<string>` | 項目の一覧（`AddItem` でも追加できる） |
| `SelectedIndex` | `int` | 選択中の項目の位置。未選択は `-1` |
| `SelectedItem` | `string?` | 選択中の項目。未選択は `null` |
| `Placeholder` | `string` | 未選択のときに表示する文字列 |
| `MaxVisibleItems` | `int` | 一度に表示する項目の最大数（デフォルト: `8`）。これを超えるとスクロールする |
| `ItemHeight` | `int` | 項目1つの高さ（デフォルト: `28`） |
| `IsOpen` | `bool` | 一覧が開いているかどうか |

### メソッドとイベント

| 名前 | 説明 |
|---|---|
| `AddItem(string)` | 項目を末尾に追加 |
| `Open()` / `Close()` | 一覧を開く / 閉じる |
| `SelectionChanged` | 選択が変わったときに発火（引数は新しい位置、未選択は `-1`） |

### キーボード操作

一覧が開くと、選択中の項目（なければ先頭の項目）にフォーカスが移ります。

| キー | 動作 |
|---|---|
| ↑ / ↓ | 項目を移動（`SpatialNavigation` が有効でも同じ） |
| Enter / Space | フォーカス中の項目を選んで閉じる |
| Esc | 選択を変えずに閉じる |

一覧の外側をクリックしたときも、選択を変えずに閉じます。閉じたあとは、フォーカスがドロップダウンに戻ります。

項目が画面に収まらないときは、収まる数まで表示する項目を減らし、スクロールして選びます。

:::note
一覧を開くには、`UIManager` が一度この要素を処理している必要があります。ノードツリーに追加した直後のフレームでは、まだ開けません。
:::

:::note
シーンで Esc を別の用途に使っている場合は、一覧を閉じた Esc を無視してください。詳しくは [Popup](#popup) を参照してください。
:::

## ProgressBar

進捗を表示するプログレスバーです。`Minimum` から `Maximum` の範囲のうち、`Value` の割合だけを横方向に塗ります。表示専用の要素で、フォーカスもクリックも受け付けません。

```csharp
var progress = new ProgressBar(0, 100, 30)
    .Location(30, 60)
    .Size(300, 16);

Root.Add(progress);

// 進捗を更新する
progress.Value = 65;
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Value` | `float` | 現在の値。範囲外の値は、最小値から最大値の間に収められる |
| `Minimum` | `float` | 最小値（デフォルト: `0`） |
| `Maximum` | `float` | 最大値（デフォルト: `1`） |
| `Fraction` | `float` | 値の割合（0〜1、読み取り専用） |

:::tip
`Fraction` は、ラベルに「65%」のようなパーセント表示をしたいときに便利です。

```csharp
label.Content = $"{progress.Fraction:P0}";
```
:::

## RadioGroup

項目の一覧から1つを選ぶ、ラジオボタンのグループです。`Dropdown` と同じように、項目の文字列と選択位置を扱います。項目は縦にも横にも並べられます。

```csharp
var quality = new RadioGroup(["低", "中", "高"])
    .Location(30, 60);

quality.SelectedIndex = 1;
quality.SelectionChanged += index =>
{
    Console.WriteLine($"画質: {quality.SelectedItem}");
};

Root.Add(quality);
```

横に並べる場合は、`Direction` を指定します。

```csharp
var difficulty = new RadioGroup(["かんたん", "ふつう", "むずかしい"], StackDirection.Horizontal);
```

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `Items` | `IList<string>` | 項目の一覧。変更すると表示も更新される（`AddItem` でも追加できる） |
| `SelectedIndex` | `int` | 選択中の項目の位置。未選択は `-1` |
| `SelectedItem` | `string?` | 選択中の項目。未選択は `null` |
| `Direction` | `StackDirection` | 項目を並べる方向（デフォルト: `Vertical`） |
| `ItemStyle` | `IUIStyle<RadioItem>` | 各項目のスタイル |
| `SelectionChanged` | イベント | 選択が変わったときに発火（引数は新しい位置） |

### キーボード操作

各項目が、個別にフォーカスを受け取ります。Tab や方向キーはフォーカスの移動だけを行い、選択は確定操作で行います。

| 操作 | 動作 |
|---|---|
| Tab / ↑ / ↓ | 項目間のフォーカス移動 |
| Enter / Space | フォーカス中の項目を選択 |
| クリック | クリックした項目を選択 |

### スタイルによるレイアウト

`RadioGroup` の並べ方と見た目は、2段のスタイルで決まります。

- **グループのスタイル** (`IUIStyle<RadioGroup>`): 項目の間隔・余白・揃え位置・背景を決めます。内部の `StackContainer` は `group.Stack` で取得でき、スタイルの `Apply` から設定します。
- **項目のスタイル** (`IUIStyle<RadioItem>`): マーカーとラベルの並び、項目の大きさ、見た目を決めます。

```csharp
quality.Style = new DefaultRadioGroupStyle
{
    VerticalGap = 12,                           // 縦に並べるときの間隔
    HorizontalGap = 24,                         // 横に並べるときの間隔
    Padding = new Padding(8),                   // 周囲の余白
    BackgroundColor = Color.FromArgb(120, 0, 0, 0),
};

quality.ItemStyle = new DefaultRadioItemStyle
{
    MarkerSize = 20,
    Spacing = 10,
};
```

独自のスタイルを作る場合は、`IUIStyle<RadioGroup>` で `element.Stack` の `Gap` / `Padding` / `Align` を設定し、`IUIStyle<RadioItem>` で `element.Size` と `element.Label.Location` を設定します。`element.IsSelected` で、選択中かどうかを判定できます。

:::note
`group.IsEnabled = false` にすると、全ての項目が操作できなくなります。
:::

## TabBar

上部の見出し（タブ）で、複数のページを切り替えるタブバーです。ページは `Container` で、タブごとに1つ用意します。

```csharp
var general = new Container();
general.Add(new Checkbox("全画面表示").Location(0, 0).Size(200, 24));

var sound = new Container();
sound.Add(new Slider(0, 10, 7).Location(0, 0).Size(200, 24));

var tabs = new TabBar()
    .Location(30, 60)
    .Size(400, 240);

tabs.AddTab("一般", general).AddTab("サウンド", sound);

tabs.SelectionChanged += index =>
{
    Console.WriteLine($"選択: {index}");
};

Root.Add(tabs);
```

最初に追加したタブが、自動で選択されます。選択されているページだけが表示され、それ以外のページは非表示になります。
非表示のページの中の要素は、クリックにもフォーカス移動にも反応しません。

ページの原点は、見出しの下の `ContentPadding`（デフォルト: 8px）の内側の左上です。タブバー全体の大きさは `Size` で指定します。

### 主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `SelectedIndex` | `int` | 選択中のタブの位置。タブがなければ `-1`。範囲外の値は無視される |
| `SelectedPage` | `Container?` | 選択中のページ |
| `Pages` | `IReadOnlyList<Container>` | ページの一覧 |
| `TabCount` | `int` | タブの数 |
| `ContentPadding` | `Padding` | ページ領域の内側の余白 |
| `TabStyle` | `IUIStyle<TabButton>` | 見出し1つ分のスタイル |
| `SelectionChanged` | イベント | 選択が変わったときに発火（引数は新しい位置） |

### メソッド

| メソッド | 説明 |
|---|---|
| `AddTab(string, Container)` | 見出しの文字列とページを組にして、末尾に追加 |
| `RemoveTab(int)` | タブとページを取り除く。選択中のタブを取り除くと、隣のタブが選択される |
| `GetTabButton(int)` | 見出し（`TabButton`）を取得 |

### キーボード操作

見出しは、それぞれフォーカスを受け取ります。

| 操作 | 動作 |
|---|---|
| Tab / ↑ / ↓ | 見出しとページ内の要素の間のフォーカス移動 |
| Enter / Space | フォーカス中の見出しのタブを選択 |
| クリック | クリックした見出しのタブを選択 |

ページの中にフォーカスがある状態でタブを切り替えると、フォーカスは新しく選ばれた見出しに移ります。

### スタイルによるレイアウト

`RadioGroup` と同じように、2段のスタイルで決まります。

- **タブバーのスタイル** (`IUIStyle<TabBar>`): 見出し同士の間隔と、ページ領域の背景・枠線を決めます。見出しを並べる `StackContainer` は `tabs.Header` で取得できます。
- **見出しのスタイル** (`IUIStyle<TabButton>`): 見出し1つ分の大きさ・色・選択中の表示を決めます。

```csharp
tabs.Style = new DefaultTabBarStyle { HeaderGap = 6 };
tabs.TabStyle = new DefaultTabButtonStyle
{
    HorizontalPadding = 24,
    Height = 36,
};
```
