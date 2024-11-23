# "Hello, world!"

前回、黒いウィンドウを表示することには成功しましたが、これでは物足りないですよね。やはりウィンドウに何かを表示しないと始まった気がしないと思います。

今回は、ウィンドウに文字列を表示させてみましょう。

まず、 `Program.cs` を次のように書き換えてください。

```csharp
using Promete;
using Promete.GLDesktop;

var app = PrometeApp.Create()
    .Use<ConsoleLayer>()// [!code ++]
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
```

`ConsoleLayer` プラグインを登録しています。Prometeでは、このように必要な機能をプラグインとして登録して使います。

---

次に、 `MainScene.cs` を次のように書き換えてください。

```csharp
using Promete;
using Promete.Windowing;// [!code ++]

public class MainScene : Scene// [!code --]
public class MainScene(ConsoleLayer console) : Scene// [!code ++]
{
    public override void OnStart()// [!code ++]
    {// [!code ++]
        console.Print("Hello, world!");// [!code ++]
    }// [!code ++]
}
```

[プライマリコンストラクタ](https://ufcpp.net/study/csharp/oo_construct.html#primary-constructor) を、 `ConsoleLayer` を受け取るように書き換えます。

::: info
Prometeでは、このように登録したプラグインのインスタンスを、シーンのコンストラクタで受け取ることができます。[プラグインシステム](/guide/advanced/plugin-system) のページで詳しく解説します。
:::

シーンの `OnStart` メソッドをオーバーライドすると、シーンが開始されたときに呼ばれる処理を記述できます。この例では、シーンが開始されたときにコンソールに文字列を表示しています。

これで、ビルドして実行してみましょう。

![Hello, world!](/assets/hello-world.png)

ウィンドウに文字が表示されましたね。おめでとうございます！

次のページでは、ウィンドウを細かくカスタマイズする方法を学びます。<br/>
画像を表示できるようになるのはもうちょっと先です…。
