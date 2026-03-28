---
title: UIライブラリ概要
description: PrometeのUIライブラリの全体像とセットアップ方法を解説します。
sidebar:
  order: 1
---

Prometeには、ゲーム内UIを構築するためのUIライブラリが組み込まれています。ボタン、チェックボックス、スライダーなどの標準的なUI要素を備え、ノードシステムの上に構築されているため、既存のグラフィック機能とシームレスに連携します。

## 特徴

- **ノードベース**: UI要素は `Container` を継承しており、通常のノードと同じように配置・変形できます
- **スタイルシステム**: `IUIStyle<T>` インターフェースにより、見た目を自由にカスタマイズ可能
- **フォーカスナビゲーション**: Tab/Shift+Tabによるキーボードナビゲーション対応
- **モーダルダイアログ**: モーダルスタックによるダイアログ表示と入力制御

## セットアップ

UIライブラリを使用するには、`UIManager` プラグインを登録します。

```csharp title="Program.cs"
using Promete.UI;

var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<UIManager>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

:::caution
`UIManager` は `Keyboard` と `Mouse` に依存しています。必ず先に登録してください。
:::

## 基本的な使い方

シーンで `UIManager` を受け取り、UI要素をノードツリーに追加します。

```csharp
using Promete.UI;
using Promete.UI.Elements;

public class MainScene(UIManager uiManager) : Scene
{
    public override void OnStart()
    {
        var button = new Button("クリック！")
            .Location(100, 100)
            .Size(180, 50)
            .OnClick(() => Console.WriteLine("ボタンが押された！"));

        Root.Add(button);
    }
}
```

UI要素はノードなので、`Location`、`Scale`、`ZIndex` などの標準的な Setup API がすべて使えます。

## UIElement の共通機能

すべてのUI要素は `UIElement` を継承しており、以下の共通機能を持ちます。

| プロパティ | 説明 |
|---|---|
| `IsEnabled` | 要素が操作可能かどうか。`false` にすると Disabled 状態になります |
| `IsFocusable` | キーボードフォーカスの対象にするかどうか |
| `NavigationOrder` | Tab キーによるフォーカス移動の順序（小さいほど先） |
| `FocusGroup` | フォーカスナビゲーションのスコープを分離するグループ |
| `State` | 現在のインタラクション状態（Hovered, Pressed, Focused, Disabled） |

| イベント | 説明 |
|---|---|
| `Clicked` | クリック（マウスまたはキーボード）されたとき |
| `PointerEntered` / `PointerLeft` | マウスカーソルの出入り |
| `GotFocus` / `LostFocus` | フォーカスの取得/喪失 |
| `PointerPressed` / `PointerMoved` / `PointerReleased` | ドラッグ操作 |

## 次のステップ

- [UI要素](/guide/ui/elements/) — 各UI要素の詳細な使い方
- [スタイルシステム](/guide/ui/styles/) — 見た目のカスタマイズ方法
- [フォーカスとモーダル](/guide/ui/focus-and-modal/) — フォーカスナビゲーションとモーダルダイアログ
- [InputMap](/guide/input/input-map/) — 入力アクションマッピング
