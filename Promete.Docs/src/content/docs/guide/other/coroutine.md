---
title: コルーチン
description: PrometeのCoroutine/CoroutineManagerによる非同期処理・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

Unityライクなコルーチン機能を利用できます。コルーチンとは、複数フレームにまたがる非同期処理を簡潔に記述するための仕組みです。

## 基本の使い方
まず、プラグインとして `CoroutineManager` をアプリケーションに登録します。
```csharp
// プラグイン登録例（Program.cs）
var app = PrometeApp.Create()
    .Use<CoroutineManager>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

`CoroutineManager` を受け取り、`Start`メソッドに `IEnumerator` を渡します。この `IEnumerator` を返すメソッド内の `yield return` にイールド命令（後述）を渡すことで、一定時間や条件で処理を遅延させることができます。


```csharp
// シーンでの利用例
public class MainScene(CoroutineManager coroutines, ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        coroutines.Start(MyCoroutine());
        console.Print("コルーチンを開始しました");
    }

    private IEnumerator MyCoroutine()
    {
        // 1秒待つ
        yield return new WaitForSeconds(1.0f);
        Console.WriteLine("1秒経過");
    }
}
```

## 主なAPI

- `coroutines.Start(IEnumerator)`<br/>コルーチンを開始
- `coroutines.Stop(Coroutine)`<br/>コルーチンを停止
- `coroutines.Clear()`<br/>全コルーチン停止

## コルーチンの終了条件
コルーチンは、以下の条件で終了・中断されます。
- 処理が終了したとき
- `Stop`メソッドが呼ばれたとき
- ハンドルされていない例外が発生したとき
  - 例外が発生すると即座に中断され、Errorコールバックが呼び出されます
  - Errorコールバックが登録されていない場合、アプリケーションはクラッシュします
- `yield break` が実行されたとき
- シーンが切り替わったとき
  - ただし、`Coroutine.KeepAlive()` メソッドを呼び出すことで、シーンが切り替わっても中断されなくなります

## イールド命令
コルーチンの中では、 `yield return` で以下のようなオブジェクト（イールド命令）を返すことで、処理を遅延できます。
- `WaitForSeconds(float seconds)`<br/>指定した秒数だけ待機します
- `WaitUntil(Func<bool> condition)`<br/>条件がtrueになるまで待機します
- `WaitWhile(Func<bool> condition)`<br/>条件がtrueの間だけ待機します
- `WaitForTask(Task task)`<br/>指定したTask/ValueTaskが完了するまで待機します
- `WaitUntilNextFrame()`<br/>次のフレームまで待機します（`yield return null` でも同等）

## コールバックの登録（Then, Error）

コルーチンが正常に完了したときや、例外で中断したときに呼び出されるコールバックを登録できます。

```csharp
coroutines
    .Start(MyCoroutine())
    .Then(() => Console.WriteLine("完了！"))
    .Error(ex => Console.WriteLine($"例外発生: {ex.Message}"));
```

- `Then(Action)`<br/>コルーチンが正常終了したときに実行されます
- `Error(Action<Exception>)`<br/>コルーチン内で例外が発生したときに実行されます
