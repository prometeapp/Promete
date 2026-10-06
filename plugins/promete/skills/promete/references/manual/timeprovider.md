---
title: 時間管理
description: フレーム時間・FPS・タイムスケールを管理するITimeProviderについて解説します。
sidebar:
  order: 2
---

`ITimeProvider` は、フレーム時間・FPS・タイムスケールなどの時間に関する情報を管理するインターフェースです。シーン内では `Time` プロパティを通じてアクセスします。

```csharp
public class MyScene : Scene
{
    public override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        // dt を使った位置の更新など
        x += speed * dt;
    }
}
```

## プロパティ

### 時間情報

```csharp
// 前フレームからの経過時間（秒）。フレームレートに依存しない移動量の計算に使用する
float dt = Time.DeltaTime;

// ゲーム開始からの累積時間（秒）
float total = Time.TotalTime;

// 現在のFPS
long fps = Time.FramePerSeconds;
```

### フレームレート制御

```csharp
// FPS目標値の設定（0で無制限）
Time.TargetFps = 60;
```

### タイムスケール

`TimeScale` を変更することで、ゲーム全体の時間の流れを変化させられます。`DeltaTime` と `TotalTime` の値に影響します。

```csharp
Time.TimeScale = 1.0f;  // 通常速度（デフォルト）
Time.TimeScale = 0.5f;  // スローモーション（半分の速度）
Time.TimeScale = 2.0f;  // 早送り（2倍速）
Time.TimeScale = 0.0f;  // 一時停止
```

:::note
`TimeScale` は `DeltaTime` に乗算されます。物理演算やアニメーションのようにタイムスケールの影響を受けたくない処理がある場合は、独自に実時間を計測してください。
:::

## 使用例

### フレームレート非依存の移動

```csharp
public class PlayerScene(Keyboard keyboard) : Scene
{
    private Vector _position;
    private const float Speed = 200f; // ピクセル/秒

    public override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        if (keyboard.Right.IsPressed) _position.X += Speed * dt;
        if (keyboard.Left.IsPressed)  _position.X -= Speed * dt;
        if (keyboard.Down.IsPressed)  _position.Y += Speed * dt;
        if (keyboard.Up.IsPressed)    _position.Y -= Speed * dt;
    }
}
```

### ポーズ機能

```csharp
public class GameScene(Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        if (keyboard.P.IsKeyDown)
        {
            // TimeScaleを0にしてゲームを一時停止
            Time.TimeScale = Time.TimeScale == 0f ? 1f : 0f;
        }
    }
}
```

## 関連項目

- [ゲームビュー](/guide/manual/gameview) - ウィンドウの表示・サイズ管理
- [PrometeApp](/guide/manual/app) - アプリケーションの初期化
