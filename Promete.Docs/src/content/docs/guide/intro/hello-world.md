---
title: '"Hello, world!"'
description: ウィンドウに文字列を表示する方法を解説します。
sidebar:
  order: 3
---

今回は、ウィンドウに簡単な文字列を表示させてみましょう。

Prometeには、簡単なテキスト表示機能「[コンソールレイヤー](/features/console)」があります。この機能を使って、ウィンドウに文字列を表示します。

エントリーポイントの `Program.cs` を次のように書き換えてください。

```diff lang=csharp title="Program.cs"
using Promete;
using Promete.GLDesktop;

var app = PrometeApp.Create()
+    .Use<ConsoleLayer>()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
```

次に、 `MainScene.cs` を次のように書き換えてください。

```diff lang="cs" title="MainScene.cs"
using Promete;
+ using Promete.Windowing;

-public class MainScene : Scene
+public class MainScene(ConsoleLayer console) : Scene
{
+    public override void OnStart()
+    {
+        console.Print("Hello, world!");
+    }
}
```

[プライマリコンストラクタ](https://ufcpp.net/study/csharp/oo_construct.html#primary-constructor) を、 `ConsoleLayer` を受け取るように書き換えます。

:::tip
Prometeでは、このように登録したプラグインのインスタンスを、シーンのコンストラクタで受け取ることができます。[プラグインシステム](/guide/advanced/plugin-system) のページで詳しく解説します。
:::

シーンの `OnStart` メソッドをオーバーライドすると、シーンが開始されたときに呼ばれる処理を記述できます。この例では、シーンが開始されたときにコンソールに文字列を表示しています。

これで、ビルドして実行してみましょう。

![Hello, world!](/assets/hello-world.png)

ウィンドウに文字が表示されましたね。おめでとうございます！

より詳しい手順を学ぶため、以降のページもご覧ください。
