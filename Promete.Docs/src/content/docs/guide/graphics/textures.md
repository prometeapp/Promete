---
title: テクスチャ
description: Prometeにおけるテクスチャの読み込みと管理について解説します。
sidebar:
  order: 2
---

テクスチャは、2Dゲームにおいて画像を表現するためのリソースです。キャラクター、背景、UI要素など、画面に表示される視覚的な要素はすべてテクスチャとして管理されます。

Prometeでは、`Texture2D`構造体がテクスチャを表現し、`TextureFactory`クラスがテクスチャの生成と読み込みを担当します。

## 基本的なテクスチャ読み込み

### ファイルからの読み込み

最も一般的なテクスチャの読み込み方法は、画像ファイルから読み込むことです：

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
        _playerTexture = Window.TextureFactory.Load("assets/player.png");

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

### ストリームからの読み込み

埋め込みリソースやメモリ上のデータからテクスチャを読み込むこともできます：

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

        var texture = Window.TextureFactory.Load(resourceStream);
        var sprite = new Sprite(texture).Location(50, 50);
        Root.Add(sprite);
    }
}
```

## スプライトシート

複数の画像を一つのファイルにまとめたスプライトシートを読み込むことができます：

```csharp title="SpriteSheetExample.cs"
using Promete;
using Promete.Graphics;
using Promete.Nodes;

[Demo("/graphics/sprite_sheet.demo", "スプライトシートの読み込み")]
public class SpriteSheetExample : Scene
{
    private Texture2D[] _iconTextures;

    public override void OnStart()
    {
        // 3x1のスプライトシートを読み込み（各アイコンは32x32ピクセル）
        _iconTextures = Window.TextureFactory.LoadSpriteSheet(
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

## プログラムによるテクスチャ生成

### 単色テクスチャ

プログラムで単色のテクスチャを生成できます：

```csharp title="SolidTextureExample.cs"
using System.Drawing;
using Promete.Graphics;
using Promete.Nodes;

public class SolidTextureExample : Scene
{
    public override void OnStart()
    {
        // 赤色の64x64ピクセルテクスチャを生成
        var redTexture = Window.TextureFactory.CreateSolid(
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

ピクセルデータから直接テクスチャを生成することも可能です：

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

        var texture = Window.TextureFactory.Create(bitmap);
        var sprite = new Sprite(texture)
            .Location(50, 50)
            .Scale(32, 32); // 拡大して見やすくする

        Root.Add(sprite);
    }
}
```

## 9スライステクスチャ

UIパネルなどで使用される伸縮可能な9スライステクスチャを読み込めます：

```csharp title="NineSliceExample.cs"
using Promete.Graphics;
using Promete.Nodes;

public class NineSliceExample : Scene
{
    public override void OnStart()
    {
        // 9スライステクスチャを読み込み
        // left=8, top=8, right=8, bottom=8 の境界を指定
        var nineSlice = Window.TextureFactory.Load9Sliced(
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

## テクスチャのプロパティ

`Texture2D`構造体は以下のプロパティを持ちます：

```csharp
// テクスチャのサイズを取得
VectorInt size = texture.Size;
Console.WriteLine($"サイズ: {size.X} x {size.Y}");

// 描画バックエンドが使用する画像のハンドル（通常は直接使用しない）
int handle = texture.Handle;
```

## リソース管理のベストプラクティス

### 適切な解放

テクスチャは必ず適切に解放してください：

```csharp title="ResourceManagement.cs"
public class ResourceManagement : Scene
{
    private readonly List<Texture2D> _textures = new();

    public override void OnStart()
    {
        // 複数のテクスチャを読み込み
        _textures.Add(Window.TextureFactory.Load("assets/bg.png"));
        _textures.Add(Window.TextureFactory.Load("assets/player.png"));
        _textures.Add(Window.TextureFactory.Load("assets/enemy.png"));
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

### using文の活用

一時的にテクスチャを使用する場合は`using`文を活用しましょう：

```csharp
public void CreateTemporarySprite()
{
    using var tempTexture = Window.TextureFactory.CreateSolid(Color.Blue, (32, 32));
    var sprite = new Sprite(tempTexture).Location(200, 200);
    Root.Add(sprite);

    // using文を抜けるときに自動的にtempTexture.Dispose()が呼ばれる
}
```

## パフォーマンスの考慮事項

### テクスチャの再利用

同じ画像を複数回使用する場合は、テクスチャを再利用しましょう：

```csharp title="TextureReuse.cs"
public class TextureReuse : Scene
{
    private Texture2D _coinTexture;

    public override void OnStart()
    {
        // 一度だけテクスチャを読み込み
        _coinTexture = Window.TextureFactory.Load("assets/coin.png");

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
