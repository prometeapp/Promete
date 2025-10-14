---
title: キーボード入力
description: PrometeのKeyboardプラグインによるキーボード入力の基本・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

Prometeでは、`Keyboard`プラグインを使って簡単にキーボード入力を取得できます。
キーの押下・離上・連打検出や、文字列入力、全キー列挙など幅広い用途に対応しています。

## 基本の使い方

`Keyboard`はプラグインとして登録し、シーンのコンストラクタで依存性注入を利用して受け取ります。

```csharp
// プラグイン登録例（Program.cs）
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

```csharp
// シーンでの利用例
public class MainScene(Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        if (keyboard.Up.IsKeyDown)
        {
            // 上キーが押されたときの処理
        }
        if (keyboard.Enter.IsKeyDown)
        {
            // Enterキーが押されたときの処理
        }
    }
}
```

## 主なAPI

- `keyboard.AllKeyCodes`<br/>すべてのキーコードを列挙します。
- `keyboard.AllPressedKeys`<br/>現在押されているキーを列挙します。
- `keyboard.AllDownKeys`<br/>このフレームで押されたキーを列挙します。
- `keyboard.AllUpKeys`<br/>このフレームで離されたキーを列挙します。
- `keyboard.GetString()`<br/>入力された文字列を取得します（バッファはクリアされます）。
- `keyboard.HasChar()`<br/>入力バッファに文字があるか判定します。

## サンプル：メニュー操作

```csharp
public override void OnUpdate()
{
    if (keyboard.Up.IsKeyDown)
        CurrentIndex--;
    if (keyboard.Down.IsKeyDown)
        CurrentIndex++;
    if (keyboard.Enter.IsKeyDown)
        SelectMenu();
}
```

## ノート

- 入力バッファは `GetString()` や `GetChar()` で消費されます。
- モバイル環境では `OpenVirtualKeyboard()` で仮想キーボードを開けます。
- キーコードは `KeyCode` 列挙体で管理されています。
