---
title: フォーカスとモーダル
description: UIManagerによるフォーカスナビゲーション、FocusGroup、モーダルダイアログの使い方を解説します。
sidebar:
  order: 4
---

`UIManager` は、フォーカスナビゲーションとモーダルダイアログの管理を提供します。

## フォーカスナビゲーション

### Tab キーによる移動

`IsFocusable = true`（デフォルト）のUI要素は、Tab / Shift+Tab キーでフォーカスを順番に移動できます。移動順序は `NavigationOrder` プロパティで制御します。

```csharp
var nameInput = new TextInput("名前")
    .NavigationOrder(1);

var emailInput = new TextInput("メール")
    .NavigationOrder(2);

var submitButton = new Button("送信")
    .NavigationOrder(3);
```

### プログラムからのフォーカス制御

```csharp
// 特定の要素にフォーカスを設定
uiManager.SetFocus(nameInput);

// フォーカスを解除
uiManager.SetFocus(null);

// 次の要素 / 前の要素へ移動
uiManager.MoveFocusNext();
uiManager.MoveFocusPrevious();
```

### フォーカス状態の取得

```csharp
// UIManager から現在のフォーカス要素を取得
var focused = uiManager.FocusedElement;

// 各要素で自身のフォーカス状態を確認
if (button.IsFocused) { ... }
```

### キーボードによるアクティベーション

フォーカス中のUI要素は、Enter / Space キーで `Clicked` イベントが発火します。これにより、マウスを使わずにボタンやチェックボックスを操作できます。

:::note
`TextInput` にフォーカスがある場合、Enter は `Submitted` イベントの発火に使われ、Space は通常の文字入力として処理されます。`Slider` にフォーカスがある場合は、左右キーで値を変更できます。
:::

## FocusGroup

`FocusGroup` を使うと、フォーカスナビゲーションのスコープを分離できます。同じグループ内の要素だけが Tab キーで巡回されます。

```csharp
var menuGroup = new FocusGroup("menu");
var settingsGroup = new FocusGroup("settings");

// メニューグループ
var newGameButton = new Button("New Game")
    .NavigationOrder(1)
    .InFocusGroup(menuGroup);

var loadGameButton = new Button("Load Game")
    .NavigationOrder(2)
    .InFocusGroup(menuGroup);

// 設定グループ（別のフォーカススコープ）
var volumeSlider = new Slider(0, 100, 50)
    .NavigationOrder(1)
    .InFocusGroup(settingsGroup);
```

グループに属さない要素はグローバルスコープに属します。フォーカスがグループ内の要素にある場合、Tab キーはそのグループ内でのみ巡回します。

## モーダルダイアログ

モーダルダイアログは、表示中にモーダル外の要素への操作をブロックします。

### 基本的な使い方

```csharp
var modal = new Modal((300, 180));
modal.Size = View.Size;  // オーバーレイを画面全体に

// モーダル本体に要素を追加
var message = new Text("変更を保存しますか？", color: Color.White)
    .Location(20, 30);
modal.Body.Add(message);

var yesButton = new Button("はい")
    .Location(20, 120)
    .Size(120, 40)
    .OnClick(() => {
        Save();
        modal.Close();
    });
modal.Body.Add(yesButton);

var noButton = new Button("いいえ")
    .Location(160, 120)
    .Size(120, 40)
    .OnClick(() => modal.Close());
modal.Body.Add(noButton);

// 表示
modal.Show(uiManager, Root);
```

### モーダルスタック

モーダルは内部的にスタックで管理されます。複数のモーダルを重ねて表示でき、最上位のモーダルのみが操作を受け付けます。

```csharp
// 現在の最上位モーダルを取得
var current = uiManager.CurrentModal;
```

### オーバーレイクリックによるクローズ

`CloseOnOverlayClick = true`（デフォルト）の場合、モーダルの外側（オーバーレイ部分）をクリックすると自動的に閉じます。確認ダイアログなどで意図しないクローズを防ぎたい場合は `false` に設定してください。

```csharp
var modal = new Modal((300, 200))
{
    CloseOnOverlayClick = false,  // 明示的に閉じるボタンが必要
};
```

### Closed イベント

モーダルが閉じられたとき（`Close()` 呼び出し時、またはオーバーレイクリック時）に発火します。

```csharp
modal.Closed += () =>
{
    Console.WriteLine("モーダルが閉じられた");
};
```
