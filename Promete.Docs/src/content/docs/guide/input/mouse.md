---
title: マウス入力
description: PrometeのMouseプラグインによるマウス入力の基本・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 2
---

Prometeでは、`Mouse`プラグインを使ってマウスカーソルの位置やボタン入力、ホイールスクロールなどを簡単に取得できます。

## 基本の使い方

`Mouse`はプラグインとして登録し、シーンのコンストラクタで依存性注入を利用して受け取ります。

```csharp
// プラグイン登録例（Program.cs）
var app = PrometeApp.Create()
    .Use<Mouse>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

```csharp
// シーンでの利用例
public class MainScene(Mouse mouse) : Scene
{
    public override void OnUpdate()
    {
        var pos = mouse.Position;
        if (mouse[MouseButtonType.Left].IsButtonDown)
        {
            // 左クリック時の処理
        }
    }
}
```

## 主なAPI

- `mouse.Position`<br/>マウスカーソルの現在位置（VectorInt型）
- `mouse.Scroll`<br/>ホイールのスクロール量（Vector型）
- `mouse[MouseButtonType.Left]`<br/>左ボタンの状態（IsPressed, IsButtonDown, IsButtonUp など）
- `mouse[0]`<br/>インデックスでボタン指定も可能

## サンプル：お絵かきツール

```csharp
public override void OnUpdate()
{
    if (mouse[MouseButtonType.Left].IsPressed)
        DrawLine(previousPosition, mouse.Position);
    if (mouse[MouseButtonType.Right].IsButtonDown)
        ClearCanvas();
    previousPosition = mouse.Position;
}
```

## 注意点

- ボタンは `MouseButtonType` で指定できます（Left, Right, Middle など）。
- ボタンの状態はフレームごとにリセットされます（IsButtonDown/IsButtonUp）。
- マウスがウィンドウ外に出た場合のイベントも取得可能です。
