---
title: ゲームパッド入力
description: PrometeのGamepads/Gamepadプラグインによるゲームパッド入力の基本・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 3
---

Prometeでは、`Gamepads`プラグインを使って複数のゲームパッド入力を簡単に取得できます。
各種ボタン・スティック・トリガー・振動など、一般的なゲームパッド操作に対応しています。

## 基本の使い方

`Gamepads`はプラグインとして登録し、シーンのコンストラクタで依存性注入を利用して受け取ります。

```csharp
// プラグイン登録例（Program.cs）
var app = PrometeApp.Create()
    .Use<Gamepads>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

```csharp
// シーンでの利用例
public class MainScene(Gamepads gamepads) : Scene
{
    public override void OnUpdate()
    {
        var pad = gamepads[0];
        if (pad != null && pad[GamepadButtonType.A].IsButtonDown)
        {
            // Aボタンが押されたときの処理
        }
    }
}
```

## 主なAPI

- `gamepads[index]`<br/>指定インデックスのゲームパッドを取得します。
- `gamepad[GamepadButtonType.A]`<br/>指定ボタンの状態（IsPressed, IsButtonDown, IsButtonUp など）
- `gamepad.LeftStick` / `gamepad.RightStick`<br/>左右スティックの位置（Vector型）
- `gamepad.IsConnected`<br/>接続状態
- `gamepad.Vibrate(value)`<br/>振動（対応パッドのみ）

## サンプル：複数パッド対応

```csharp
public override void OnUpdate()
{
    for (int i = 0; i < 4; i++)
    {
        var pad = gamepads[i];
        if (pad != null && pad[GamepadButtonType.Start].IsButtonDown)
            JoinPlayer(i);
    }
}
```

## 注意点

- パッドの接続/切断は自動で検出されます。
- ボタン・スティック・トリガーは `GamepadButtonType` で指定します。
- 振動は対応パッドのみ有効です。
