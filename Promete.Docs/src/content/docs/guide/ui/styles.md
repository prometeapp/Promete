---
title: スタイルシステム
description: IUIStyleインターフェースによるUI要素の見た目のカスタマイズ方法を解説します。
sidebar:
  order: 3
---

PrometeのUIライブラリでは、`IUIStyle<T>` インターフェースを使ってUI要素の見た目を自由にカスタマイズできます。デフォルトではShapeベースのスタイルが適用されますが、NineSliceSpriteによるテクスチャベースのスタイルに差し替えることもできます。

## 仕組み

各UI要素は `Style` プロパティを持ち、インタラクション状態（Normal, Hovered, Pressed, Focused, Disabled）が変化するたびにスタイルの `Apply` メソッドが呼ばれます。

```csharp
public interface IUIStyle<in T> where T : UIElement
{
    void Apply(T element, UIElementState state);
}
```

## デフォルトスタイルのカスタマイズ

デフォルトスタイルはプロパティで色やサイズを変更できます。

```csharp
var style = new DefaultButtonStyle
{
    NormalColor = Color.FromArgb(255, 20, 80, 60),
    HoveredColor = Color.FromArgb(255, 30, 120, 90),
    PressedColor = Color.FromArgb(255, 10, 50, 40),
    BorderColor = Color.FromArgb(255, 60, 180, 130),
    FocusedBorderColor = Color.FromArgb(255, 100, 255, 180),
    TextColor = Color.FromArgb(255, 200, 255, 220),
};

var button = new Button("保存") { Style = style };
```

### デフォルトスタイル一覧

| スタイル | 対象 | 説明 |
|---|---|---|
| `DefaultButtonStyle` | `Button` | Shape ベースのボタンスタイル |
| `DefaultCheckboxStyle` | `Checkbox` | Shape ベースのチェックボックススタイル |
| `DefaultSliderStyle` | `Slider` | Shape ベースのスライダースタイル |
| `DefaultTextInputStyle` | `TextInput` | Shape ベースのテキスト入力スタイル |
| `DefaultPanelStyle` | `Panel` | Shape ベースのパネルスタイル |
| `DefaultScrollViewStyle` | `ScrollView` | Shape ベースのスクロールビュースタイル |
| `DefaultModalStyle` | `Modal` | Shape ベースのモーダルスタイル |

## NineSliceSprite によるテクスチャスタイル

ボタンには `NineSliceButtonStyle` が用意されており、9スライステクスチャを使った見た目にできます。

```csharp
var texture = App.TextureFactory.Load9Sliced("assets/button.png", 16, 16, 16, 16);

var style = new NineSliceButtonStyle
{
    NormalTexture = texture,
    HoveredTexture = texture,    // 状態ごとに別のテクスチャも設定可能
    PressedTexture = texture,
    TextColor = Color.White,
};

var button = new Button("NineSlice ボタン") { Style = style };
```

## カスタムスタイルの作成

`IUIStyle<T>` を実装して、完全にオリジナルのスタイルを作ることもできます。

```csharp
public class MyButtonStyle : IUIStyle<Button>
{
    public void Apply(Button element, UIElementState state)
    {
        var color = state switch
        {
            _ when state.HasFlag(UIElementState.Disabled) => Color.Gray,
            _ when state.HasFlag(UIElementState.Pressed) => Color.DarkBlue,
            _ when state.HasFlag(UIElementState.Hovered) => Color.LightBlue,
            _ => Color.Blue,
        };

        // SetBackgroundNode で背景を差し替える
        var bg = Shape.CreateRect((0, 0), element.Size, color);
        element.SetBackgroundNode(bg);

        // テキスト色の変更
        element.Label.Color = Color.White;
    }
}
```

### Apply メソッドの実装ポイント

- `element.SetBackgroundNode(node)` で背景ノードを差し替えます。古いノードは自動的に削除されます
- 追加のノード（チェックマークやカーソルなど）は `element.Add()` / `element.Remove()` で管理します
- `Name()` を使ってノードに名前をつけておくと、次回の Apply 時に名前で検索して差し替えられます

:::caution
`Apply` は状態変化のたびに呼ばれるため、毎回新しいノードを作成してもパフォーマンス上問題ありません。ただし、大量のUI要素がある場合はノードのキャッシュを検討してください。
:::

## スタイルの動的変更

実行中にスタイルを切り替えることもできます。

```csharp
// ダークモードとライトモードの切り替え
button.Style = isDarkMode ? darkStyle : lightStyle;
```

`Style` プロパティへの代入時に自動的にスタイルの再適用がスケジュールされます。
