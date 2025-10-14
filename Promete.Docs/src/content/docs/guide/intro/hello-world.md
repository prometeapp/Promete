---
title: "Hello, World!"
description: 初めてのPrometeアプリケーションで、ConsoleLayerを使って簡単にテキストを表示する方法
sidebar:
  order: 2
---

まずは、画面にテキストを表示してみましょう。以下のコードを実行します。

```csharp title="Program.cs"
using Promete;
using Promete.Input;
using Promete.Windowing;

var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<ConsoleLayer>()  // ConsoleLayerプラグインを追加
    .BuildWithOpenGLDesktop();

return app.Run<HelloWorldScene>(WindowOptions.Default with
{
    Title = "Hello, World!",
    Size = (800, 600),
});

public class HelloWorldScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        // コンソールに「Hello, World!」を表示
        console.Print("Hello, World!");
        console.Print("ESCキーで終了");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            Window.Close();
        }
    }
}
```

このコードを実行すると、黒いウィンドウの左上に白い文字で「Hello, World!」と表示されます。

## プラグインについて

上記のコードでは、`Use<ConsoleLayer>()`と`Use<Keyboard>()`を使用しています。これらは**プラグイン**と呼ばれる機能で、必要な機能を後から追加できる仕組みです。

- `ConsoleLayer`: 簡単なテキスト表示機能
- `Keyboard`: キーボード入力機能

プラグインの詳細については、後のページで詳しく説明します。

## 実行結果

1. 800×600ピクセルのウィンドウが開きます
2. 左上に「Hello, World!」と「ESCキーで終了」が表示されます
3. Escapeキーを押すとアプリケーションが終了します
