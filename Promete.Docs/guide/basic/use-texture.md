# 画像ファイルを画面に表示する

前回、ようやく画面に何かを表示するという偉業を成し遂げました。とはいえ、やはり文字だけというのは寂しいですよね…。今回は、画像ファイルを読み込んで画面に表示する方法を学びます。

好きな画像を用意してください。もしなければ、次の画像をダウンロードしてください。

[![いちご](/assets/ichigo.png)](/assets/ichigo.png)

画像をプロジェクトファイルに追加します。この例では、 `assets` フォルダに `ichigo.png` という名前で保存します。

::: warning
標準では、ビルド時に出力ディレクトリに画像ファイルはコピーされません。画像ファイルを出力ディレクトリにコピーするには、プロジェクトファイルに次のような設定を追加するか、お使いのIDEでファイルのプロパティを適切に設定してください。

```xml
<ItemGroup>
  <None Update="assets\ichigo.png">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```
:::

画像をプロジェクトに追加したら、テクスチャを読み込むコードを書きましょう。 `MainScene.cs` を次のように書き換えてください。


```csharp
using Promete;
using Promete.Graphics;// [!code ++]

public class MainScene(ConsoleLayer console) : Scene
{
    private ITexture texture;// [!code ++]

    public override void OnStart()
    {
        console.Print("Hello, world!");// [!code --]
        Window.Size = (300, 300);
        texture = Window.TextureFactory.Load("assets/ichigo.png");// [!code ++]
    }

    public override void OnUpdate()
    {
        Window.Title = $"{Window.FramePerSeconds} fps";
    }
}
```

`IWindow` インターフェイスには、 `TextureFactory` プロパティがあります。これは[テクスチャファクトリ](/guide/features/texture-factory) への参照です。Prometeでは、このテクスチャファクトリが、画像ファイルを読み込んでテクスチャを生成するための機能を提供しています。

`TextureFactory` インターフェイスには、 `Load` メソッドがあります。このメソッドは、画像ファイルのパスを受け取り、その画像ファイルを読み込んでテクスチャを生成します。このテクスチャを使って、画面に画像を表示することができます。1枚の画像ファイルを格子状に切り抜いて使用できる `LoadSpriteSheet` メソッドなどもあるので、興味があればAPIリファレンスを参照してください。

さて、これだけではまだ画像が表示されません。次に、画像を表示するためのコードを書きましょう。`OnStart` メソッドの中に次のような処理を追加します。

```csharp
var sprite = new Sprite(texture)
  .Location(32, 32);
Root.Add(sprite);
```

`Sprite` （スプライト）は、テクスチャを好きな位置に表示するための Element です。Elementについては次のページで詳しく解説するので、今は「画面に文字を表示するための物体」として理解してください。

これでビルドして実行してみましょう。

![いちごを表示したウィンドウ](/assets/window-with-ichigo.png)

はい、画像が表示されましたね。この例ではいちごが表示されていますが、お手元の好きな画像でいろいろと試してみると楽しいかもしれません。

次のページでは、このSpriteが属するElementについて詳しく解説し、要素を移動させたり、サイズを変更したりする方法を学びます。

