---
title: InputMap
description: マウス・キーボード・ゲームパッドの入力を抽象化するInputMapの使い方を解説します。
sidebar:
  order: 5
---

`InputMap` は、物理的な入力デバイス（キーボード、マウス、ゲームパッド）を抽象的な「アクション」にマッピングするシステムです。UIライブラリとは独立して使用でき、ゲームの入力管理全般に活用できます。

## 基本的な使い方

```csharp
public class GameScene(Keyboard keyboard, Mouse mouse) : Scene
{
    private readonly InputMap _inputMap = new();

    public override void OnStart()
    {
        // アクションを定義
        _inputMap.AddAction("jump");
        _inputMap.AddAction("attack");

        // キーバインドを設定
        _inputMap.BindKey("jump", KeyCode.Space);
        _inputMap.BindKey("attack", KeyCode.Z);
        _inputMap.BindMouseButton("attack", MouseButtonType.Left);
    }

    public override void OnUpdate()
    {
        // 毎フレーム更新（必須）
        _inputMap.Update(keyboard, mouse);

        if (_inputMap.GetAction("jump").IsJustPressed)
        {
            // ジャンプ開始
        }

        if (_inputMap.GetAction("attack").IsPressed)
        {
            // 攻撃中（押し続けている間）
        }
    }
}
```

## InputAction の状態

`GetAction()` で取得される `InputAction` は、以下の状態を持ちます。

| プロパティ | 説明 |
|---|---|
| `IsPressed` | 現在押されているか（ホールド状態を含む） |
| `IsJustPressed` | このフレームで押された瞬間か |
| `IsJustReleased` | このフレームで離された瞬間か |

## 複数デバイスのバインド

1つのアクションに複数の入力をバインドできます。いずれかの入力が行われればアクションが発火します。

```csharp
_inputMap.AddAction("move_right");
_inputMap.BindKey("move_right", KeyCode.Right);
_inputMap.BindKey("move_right", KeyCode.D);
_inputMap.BindGamepadButton("move_right", GamepadButtonType.DpadRight);
```

## ゲームパッド対応

ゲームパッドのボタンもバインドできます。ゲームパッドを使用する場合は `Update` にゲームパッドのインスタンスを渡します。

```csharp
public class GameScene(Keyboard keyboard, Mouse mouse, Gamepads gamepads) : Scene
{
    private readonly InputMap _inputMap = new();

    public override void OnUpdate()
    {
        var gamepad = gamepads[0];  // 1P のゲームパッド
        _inputMap.Update(keyboard, mouse, gamepad);
    }
}
```

## プログラムからの入力発火

テストやデバッグ、仮想コントローラー実装のために、プログラムからアクションを発火できます。

### Fire — 1フレームだけ押す

```csharp
_inputMap.Fire("jump");
// 次の Update で IsJustPressed = true になり、その次のフレームで自動的にリリースされる
```

### SetPressed — 持続的に押す

```csharp
_inputMap.SetPressed("move_right", true);
// IsPressed = true が維持される

_inputMap.SetPressed("move_right", false);
// 解除
```

### プログラム入力のクリア

```csharp
_inputMap.ClearProgrammaticState("jump");  // 特定のアクション
```

## バインドの解除

```csharp
_inputMap.UnbindAll("jump");  // すべてのバインドを解除
```

## アクションの確認

```csharp
if (_inputMap.HasAction("jump"))
{
    // "jump" アクションが存在する
}
```

## メソッドチェーン

`AddAction`、`BindKey`、`BindMouseButton`、`BindGamepadButton`、`UnbindAll` はメソッドチェーンに対応しています。

```csharp
var inputMap = new InputMap()
    .AddAction("jump")
    .BindKey("jump", KeyCode.Space)
    .BindGamepadButton("jump", GamepadButtonType.A)
    .AddAction("attack")
    .BindKey("attack", KeyCode.Z)
    .BindMouseButton("attack", MouseButtonType.Left);
```
