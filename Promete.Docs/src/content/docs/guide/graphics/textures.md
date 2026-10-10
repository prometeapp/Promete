---
title: テクスチャ
description: Prometeにおけるテクスチャの読み込みと管理について解説します。
sidebar:
  order: 2
---

テクスチャは、2Dゲームにおいて画像を表現するためのリソースです。キャラクター、背景、UI要素など、画面に表示される視覚的な要素はすべてテクスチャとして管理されます。

Prometeでは、`Texture2D`構造体がテクスチャを表現し、`TextureFactoryBase`クラスがテクスチャの生成と読み込みを担当します。

`TextureFactoryBase` は、シーン内では`App.TextureFactory`でアクセスできます。また、コンストラクタ経由でDI注入して利用することもできます。

## 基本的なテクスチャ読み込み
画像ファイルをテクスチャとして読み込む場合、パスを指定するか、`Stream` インスタンスを渡します。

### ファイルパス

画像ファイルのパスを指定してテクスチャを読み込みます。PNG、JPEG、GIFなどの一般的なフォーマットに対応しています。

```csharp title="TextureLoadExample.cs"
using Promete;
using Promete.Graphics;
using Promete.Nodes;

public class TextureLoadExample : Scene
{
    private Texture2D _playerTexture;
    private Sprite _playerSprite;

    public override void OnStart()
    {
        // PNG、JPEG、BMPなどの画像ファイルを読み込み
        _playerTexture = App.TextureFactory.Load("assets/player.png");

        // スプライトを作成してテクスチャを設定
        _playerSprite = new Sprite(_playerTexture)
            .Location(100, 100)
            .Scale(2.0f);

        Root.Add(_playerSprite);
    }

    public override void OnDestroy()
    {
        // リソースを適切に解放
        _playerTexture.Dispose();
    }
}
```

### ストリーム

埋め込みリソースやメモリ上のデータなど、.NETの `Stream` 型からテクスチャを生成できます。

```csharp title="StreamLoadExample.cs"
using System.IO;
using System.Reflection;
using Promete.Graphics;

public class StreamLoadExample : Scene
{
    public override void OnStart()
    {
        // 埋め込みリソースから読み込み
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream("MyGame.Assets.embedded_texture.png");
        if (resourceStream == null) return;

        var texture = App.TextureFactory.Load(resourceStream);
        var sprite = new Sprite(texture).Location(50, 50);
        Root.Add(sprite);
    }
}
```

## スプライトシート

複数の画像を一つのファイルにまとめたスプライトシートを読み込むことができます。

```csharp title="SpriteSheetExample.cs"
using Promete;
using Promete.Graphics;
using Promete.Nodes;

public class SpriteSheetExample : Scene
{
    private Texture2D[] _iconTextures;

    public override void OnStart()
    {
        // 3x1のスプライトシートを読み込み（各アイコンは32x32ピクセル）
        _iconTextures = App.TextureFactory.LoadSpriteSheet(
            "assets/icons.png",
            horizontalCount: 3,
            verticalCount: 1,
            size: (32, 32)
        );

        // 各テクスチャを表示
        for (int i = 0; i < _iconTextures.Length; i++)
        {
            var sprite = new Sprite(_iconTextures[i])
                .Location(64 * i + 16, 16)
                .Scale(2, 2);
            Root.Add(sprite);
        }
    }

    public override void OnDestroy()
    {
        // スプライトシートのテクスチャを解放
        foreach (var texture in _iconTextures)
        {
            texture.Dispose();
        }
    }
}
```

:::note
スプライトシートから得られたテクスチャは、すべて1枚の画像を共有しています。そのため、いずれか1つを `Dispose` した時点で画像が解放され、他のテクスチャも使えなくなります。

v2.2以降では、すでに解放された後の `Dispose` は何も行わないので、上のように全要素に対して `Dispose` を呼んでも問題ありません。それ以前のバージョンでは未定義動作となるので注意してください。
:::

### 任意の矩形で切り抜く `v2.4~`

サイズがバラバラの画像を詰め込んだアトラスのように、格子状ではない画像は、切り抜く範囲を `RectInt` で直接指定できます。

```csharp
// (左, 上, 幅, 高さ) の順。戻り値は渡した順に並ぶ
var textures = App.TextureFactory.LoadSpriteSheet(
    "assets/atlas.png",
    (0, 0, 32, 32),
    (32, 0, 16, 48),
    (48, 0, 64, 24)
);
```

`TextureOptions` を渡す場合は、矩形より前に置きます。

```csharp
var textures = App.TextureFactory.LoadSpriteSheet("assets/atlas.png", TextureOptions.Default, rects);
```

非同期版の `LoadSpriteSheetAsync` は、矩形を `IReadOnlyList<RectInt>` で受け取ります。`[(0, 0, 32, 32), (32, 0, 16, 48)]` のようにコレクション式で渡せます。

画像の範囲外にはみ出す矩形や、幅・高さが 0 以下の矩形を渡すと `ArgumentException` になります。得られたテクスチャが1枚の画像を共有する点は、格子版と同じです。

## プログラムによるテクスチャ生成
ファイルを用いずに、動的にテクスチャを生成する方法もあります。

### 単色テクスチャ

`CreateSolid`メソッドを使って、指定した色とサイズの単色テクスチャを生成できます。

```csharp title="SolidTextureExample.cs"
using System.Drawing;
using Promete.Graphics;
using Promete.Nodes;

public class SolidTextureExample : Scene
{
    public override void OnStart()
    {
        // 赤色の64x64ピクセルテクスチャを生成
        var redTexture = App.TextureFactory.CreateSolid(
            Color.Red,
            size: (64, 64)
        );

        var redSprite = new Sprite(redTexture)
            .Location(100, 100);
        Root.Add(redSprite);
    }
}
```

### ビットマップデータからの生成

byte型の3次元配列（横、縦、RGBA）から直接テクスチャを生成することも可能です。配列は `[x, y, チャンネル]` の順でアクセスします。

```csharp title="BitmapTextureExample.cs"
using System.Drawing;
using Promete.Graphics;

public class BitmapTextureExample : Scene
{
    public override void OnStart()
    {
        // 2x2のチェッカーパターンを作成
        var bitmap = new byte[2, 2, 4]; // RGBA形式

        // 白いピクセル (0,0)
        bitmap[0, 0, 0] = 255; // R
        bitmap[0, 0, 1] = 255; // G
        bitmap[0, 0, 2] = 255; // B
        bitmap[0, 0, 3] = 255; // A

        // 黒いピクセル (1,1)
        bitmap[1, 1, 0] = 0;   // R
        bitmap[1, 1, 1] = 0;   // G
        bitmap[1, 1, 2] = 0;   // B
        bitmap[1, 1, 3] = 255; // A

        var texture = App.TextureFactory.Create(bitmap);
        var sprite = new Sprite(texture)
            .Location(50, 50)
            .Scale(32, 32); // 拡大して見やすくする

        Root.Add(sprite);
    }
}
```

## 9スライステクスチャ

UIパネルなどで使用される伸縮可能な9スライステクスチャを読み込めます。詳しくは [NineSliceSprite](/guide/graphics/nine-slice-sprite/) を参照してください。

この場合、`Texture2D` 構造体ではなく、 `Texture9Sliced` 構造体が返されます。

```csharp title="NineSliceExample.cs"
using Promete.Graphics;
using Promete.Nodes;

public class NineSliceExample : Scene
{
    public override void OnStart()
    {
        // 9スライステクスチャを読み込み
        // left=8, top=8, right=8, bottom=8 の境界を指定
        var nineSlice = App.TextureFactory.Load9Sliced(
            "assets/panel.png",
            left: 8, top: 8, right: 8, bottom: 8
        );

        var panel = new NineSliceSprite(nineSlice)
            .Location(50, 50)
            .Size(200, 150);

        Root.Add(panel);
    }
}
```

## テクスチャの設定 `v2.2~`

テクスチャの生成時に `TextureOptions` を渡すと、描画時のサンプリング方法を指定できます。`Load`、`LoadSpriteSheet`、`Load9Sliced`、`Create`、`CreateSolid` が受け取ります。

指定しない場合は、ピクセルパーフェクトな描画に適した既定値（`Nearest` と `Clamp`）が使われます。

### 補間方法

`TextureFilterMode` で、拡大・縮小したときのピクセルの補間方法を指定します。

- `Nearest`（既定）: 最も近いピクセルの色をそのまま使います。拡大しても境界がくっきりするので、ドット絵に向いています
- `Linear`: 周囲のピクセルを混ぜ合わせます。境界が滑らかになるので、高解像度のイラストなどに向いています

```csharp title="FilterModeExample.cs"
// ドット絵を、くっきりと拡大して表示する（既定）
var pixelArt = App.TextureFactory.Load("assets/icon.png");

// 高解像度のイラストを、滑らかに表示する
var illustration = App.TextureFactory.Load(
    "assets/illustration.png",
    new TextureOptions(TextureFilterMode.Linear)
);
```

:::caution
`Linear` をスプライトシートや `Tilemap` で使うテクスチャに指定すると、隣り合うセルのピクセルが混ざって、境界が滲んで見えることがあります。ピクセルパーフェクトな描画が必要な場合は、既定の `Nearest` を使ってください。
:::

### アドレスモード

`TextureAddressMode` で、UV 座標が 0〜1 の範囲を超えたときのサンプリング方法を指定します。

- `Clamp`（既定）: 範囲外では、端のピクセルを引き伸ばします
- `Repeat`: 範囲外では、テクスチャを繰り返します
- `Mirror`: 範囲外では、テクスチャを反転しながら繰り返します

通常の `Sprite` では UV 座標が範囲内に収まるため、違いは現れません。UV 座標を範囲外へ広げるカスタムノードや、シェーダーでタイル状に敷き詰めるときなどに使います。

```csharp title="AddressModeExample.cs"
var tile = App.TextureFactory.Load(
    "assets/tile.png",
    new TextureOptions(Address: TextureAddressMode.Repeat)
);
```

## 非同期読み込み `v2.2~`

大きな画像を `Load` で読み込むと、デコードが終わるまでゲームが止まってしまいます。`LoadAsync`、`LoadSpriteSheetAsync`、`Load9SlicedAsync` を使うと、ゲームを止めずに読み込めます。

画像のデコードはバックグラウンドで行われ、GPU への転送は次のフレームの開始時に、メインスレッドで行われます。

### コルーチンで待つ

シーンの中では、コルーチンの `WaitForTask` で待つ方法が簡単です。完了後の処理はメインスレッドで実行されるので、そのままノードを操作できます。コルーチンについては [コルーチン](/guide/other/coroutine/) を参照してください。

```csharp title="AsyncLoadExample.cs"
using System.Collections;
using Promete;
using Promete.Coroutines;
using Promete.Graphics;
using Promete.Nodes;

public class AsyncLoadExample(CoroutineManager coroutines) : Scene
{
    private Texture2D _texture;

    public override void OnStart()
    {
        coroutines.Start(LoadTexture());
    }

    private IEnumerator LoadTexture()
    {
        var task = App.TextureFactory.LoadAsync("assets/background.png");
        yield return new WaitForTask(task);

        _texture = task.Result;
        Root.Add(new Sprite(_texture));
    }

    public override void OnDestroy()
    {
        _texture.Dispose();
    }
}
```

### async / await で待つ

`async` メソッドで `await` することもできます。ただし、`await` の後のコードは、メインスレッドで実行されるとは限りません。

:::caution
`await` の後に、ノードの追加などシーンの操作を行う場合は、`App.NextFrame` を使ってメインスレッドで実行してください。
:::

```csharp title="AwaitLoadExample.cs"
private async Task LoadBackgroundAsync()
{
    var texture = await App.TextureFactory.LoadAsync("assets/background.png");

    // await の後はメインスレッドとは限らないので、NextFrame で戻す
    App.NextFrame(() => Root.Add(new Sprite(texture)));
}
```

### キャンセルと注意点

- `CancellationToken` を渡すと、GPU への転送が始まる前までキャンセルできます。転送が始まった後にキャンセルしても無視され、テクスチャが返されます
- 完了にはメインスレッドの処理が必要なので、ゲームのメインループが動作している間に使ってください
- 返されたテクスチャの `Dispose` は、同期版と同じく、メインスレッドで行ってください

## テクスチャのプロパティ

`Texture2D`構造体は以下のプロパティを持ちます：

```csharp
// テクスチャのサイズを取得
VectorInt size = texture.Size;
Console.WriteLine($"サイズ: {size.X} x {size.Y}");

// 画像のどの範囲を使うか（UV座標。左上と右下）
Vector uvStart = texture.UvStart;
Vector uvEnd = texture.UvEnd;

// (v2.2~) スプライトシートの要素など、画像の一部の範囲のみを指しているか
bool isSubTexture = texture.IsSubTexture;

// 描画バックエンドが使用する画像のハンドル（通常は直接使用しない）
int handle = texture.Handle;
```

`IsSubTexture` が `true` のテクスチャは、画像の一部を指しているだけなので、`TextureFactoryBase.Update` で内容を書き換えることはできません。

## リソース管理のベストプラクティス

### 適切な解放

メモリリークを防ぐために、テクスチャは必ず適切に解放してください。

```csharp title="ResourceManagement.cs"
public class ResourceManagement : Scene
{
    private readonly List<Texture2D> _textures = new();

    public override void OnStart()
    {
        // 複数のテクスチャを読み込み
        _textures.Add(App.TextureFactory.Load("assets/bg.png"));
        _textures.Add(App.TextureFactory.Load("assets/player.png"));
        _textures.Add(App.TextureFactory.Load("assets/enemy.png"));
    }

    public override void OnDestroy()
    {
        // 全てのテクスチャを解放
        foreach (var texture in _textures)
        {
            texture.Dispose();
        }
        _textures.Clear();
    }
}
```

## パフォーマンスの考慮事項

### テクスチャの再利用

同じ画像を複数回使用する場合は、テクスチャを再利用しましょう。

```csharp title="TextureReuse.cs"
public class TextureReuse : Scene
{
    private Texture2D _coinTexture;

    public override void OnStart()
    {
        // 一度だけテクスチャを読み込み
        _coinTexture = App.TextureFactory.Load("assets/coin.png");

        // 複数のスプライトで同じテクスチャを使用
        for (int i = 0; i < 10; i++)
        {
            var coin = new Sprite(_coinTexture)
                .Location(i * 50, 100);
            Root.Add(coin);
        }
    }

    public override void OnDestroy()
    {
        // テクスチャは一度だけ解放
        _coinTexture.Dispose();
    }
}
```
