---
title: コルーチン
description: PrometeのCoroutine/CoroutineManagerによる非同期処理・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

Prometeのコルーチンは、Unityのように `yield return` を使って非同期処理や遅延処理を簡単に記述できます。
シーン切り替え時の自動停止や、例外ハンドリング、コールバックなどもサポートしています。

## 基本の使い方

```csharp
// プラグイン登録例（Program.cs）
var app = PrometeApp.Create()
    .Use<CoroutineManager>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

```csharp
// シーンでの利用例
public class MainScene(CoroutineManager coroutines) : Scene
{
    public override void OnStart()
    {
        coroutines.Start(MyCoroutine());
    }

    private IEnumerator MyCoroutine()
    {
        // 1秒待つ
        yield return new WaitForSeconds(1.0f);
        // 続きの処理
    }
}
```

## 主なAPI

- `coroutines.Start(IEnumerator)`<br/>コルーチンを開始
- `coroutines.Stop(Coroutine)`<br/>コルーチンを停止
- `coroutines.Clear()`<br/>全コルーチン停止
- `Coroutine.Then(Action)`<br/>完了時コールバック
- `Coroutine.Error(Action<Exception>)`<br/>例外時コールバック
- `Coroutine.KeepAlive()`<br/>シーン切り替え時も継続

## サンプル：複数コルーチンの管理

```csharp
public override void OnStart()
{
    coroutines.Start(MyCoroutine1());
    coroutines.Start(MyCoroutine2());
}
```

## 主なイールド命令の種類

- `WaitForSeconds(float seconds)`<br/>指定した秒数だけ待機します
- `WaitUntil(Func<bool> condition)`<br/>条件がtrueになるまで待機します
- `WaitWhile(Func<bool> condition)`<br/>条件がtrueの間だけ待機します
- `WaitForTask(Task task)`<br/>指定したTask/ValueTaskが完了するまで待機します
- `WaitUntilNextFrame()`<br/>次のフレームまで待機します（`yield return null` でも同等）

## コールバックの登録（Then, Error）

コルーチンの完了時や例外発生時にコールバックを登録できます。

```csharp
coroutines
    .Start(MyCoroutine())
    .Then(() => Console.WriteLine("完了！"))
    .Error(ex => Console.WriteLine($"例外発生: {ex.Message}"));
```

- `Then(Action)`<br/>コルーチンが正常終了したときに実行されます
- `Error(Action<Exception>)`<br/>コルーチン内で例外が発生したときに実行されます

## 注意点

- コルーチンはシーン切り替え時に自動停止します（KeepAlive指定時を除く）
- 例外時はErrorコールバックでハンドリング可能
- `WaitForSeconds` などのイールド命令も利用できます
