---
title: MaskedContainer
description: マスク画像を使用して子要素を切り抜いて描画するMaskedContainerノードの使用方法について解説します。
sidebar:
  order: 10
  badge: v1.3.0～
---
**Promete v1.3.0からサポート**

`MaskedContainer`は、マスク画像を使用して子要素を切り抜いて描画するコンテナです。`Container`を継承しており、複雑な形状のUI要素、スポットライト効果、画像のトリミングなど、様々な視覚効果を実現できます。

## 作成

マスクテクスチャを指定して`MaskedContainer`を作成します。

```csharp title="基本的なMaskedContainerの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // マスク画像を読み込み（明るい部分が表示される）
        var maskTexture = Window.TextureFactory.Load("assets/circle_mask.png");

        // MaskedContainerを作成
        var maskedContainer = new MaskedContainer(maskTexture);

        // 子要素を追加
        var background = new Sprite(Window.TextureFactory.Load("assets/background.png"));
        maskedContainer.Add(background);

        // シーンに追加
        Root.Add(maskedContainer);
    }
}
```

## マスクの仕組み

マスクテクスチャの**RGB値の濃淡（明るさ）**によって、子要素の表示/非表示が決まります。
- **明るい色（白など）**: 子要素がそのまま表示される
- **暗い色（黒など）**: 子要素が非表示になる
- **中間の明るさ（グレーなど）**: アルファマスクモードでのみ、部分的に透明になる

:::note
濃淡の計算は RGB 値の平均 `(R + G + B) / 3` で行われます。
アルファチャンネルは無視され、純粋に色の明るさのみが使用されます。
:::

## マスキング方式

`MaskedContainer`は2つのマスキング方式をサポートしています。

### ステンシルバッファ方式（デフォルト）

デフォルトのマスキング方式です。高速でシンプルですが、マスクは完全表示/非表示の2値のみとなります。
よって、マスクのグレーな値は全て白として認識されます。

```csharp title="ステンシルバッファ方式"
var maskTexture = Window.TextureFactory.Load("assets/mask.png");

// デフォルトでステンシルバッファ方式
var container = new MaskedContainer(maskTexture, useAlphaMask: false);

// または明示的に指定
container.UseAlphaMask = false;

Root.Add(container);
```

### アルファブレンディング方式

`UseAlphaMask`を`true`に設定すると、フレームバッファを使用したアルファブレンディング方式に切り替わります。グラデーションマスクや部分透明に対応しています。

```csharp title="アルファブレンディング方式"
var maskTexture = Window.TextureFactory.Load("assets/gradient_mask.png");

// アルファブレンディング方式を使用
var container = new MaskedContainer(maskTexture, useAlphaMask: true);

Root.Add(container);
```

## 実用例

### 円形のプロフィール画像

```csharp title="円形プロフィール画像"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // 円形のマスク画像を読み込み
        var maskTexture = Window.TextureFactory.Load("assets/circle_mask.png");

        // MaskedContainerを作成
        var profileIcon = new MaskedContainer(maskTexture)
            .Location(100, 100);

        // プロフィール画像を追加
        var profileImage = new Sprite(Window.TextureFactory.Load("assets/profile.png"))
            .Size(100, 100); // マスクと同じサイズ

        profileIcon.Add(profileImage);

        // シーンに追加
        Root.Add(profileIcon);
    }
}
```

### スポットライト効果

```csharp title="スポットライト効果"
public class SpotlightScene(Mouse mouse) : Scene
{
    private MaskedContainer? _spotlightContainer;
    private Sprite? _spotlightMask;

    public override void OnStart()
    {
        // 暗い背景
        var darkBackground = new Sprite(Window.TextureFactory.Load("assets/dark_room.png"));
        Root.Add(darkBackground);

        // スポットライトマスク（グラデーション円）
        var maskTexture = Window.TextureFactory.Load("assets/spotlight_gradient.png");
        _spotlightMask = new Sprite(maskTexture)
            .Pivot(0.5f, 0.5f); // 中心を基準点に

        // アルファブレンディング方式を使用
        _spotlightContainer = new MaskedContainer(maskTexture, useAlphaMask: true);

        // 照らされる部分（明るい画像）
        var litBackground = new Sprite(Window.TextureFactory.Load("assets/lit_room.png"));
        _spotlightContainer.Add(litBackground);

        Root.Add(_spotlightContainer);
    }

    public override void OnUpdate()
    {
        // マウスに追従するスポットライト
        if (_spotlightMask != null)
        {
            _spotlightMask.Location = mouse.Position;
        }
    }
}
```

### 複雑な形状のUI枠

```csharp title="カスタム形状のUIパネル"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // カスタム形状のマスク（例: 六角形）
        var maskTexture = Window.TextureFactory.Load("assets/hexagon_mask.png");

        // MaskedContainerを作成
        var customPanel = new MaskedContainer(maskTexture)
            .Location(200, 150);

        // 背景
        var background = new Sprite(Window.TextureFactory.Load("assets/panel_bg.png"))
            .Size(200, 200)
            .TintColor(Color.FromArgb(180, 100, 150, 200)); // 半透明の色

        customPanel.Add(background);

        // コンテンツ（テキストなど）
        var titleText = new Text("TITLE", Font.GetDefault(), Color.White)
            .Location(100, 50);
        customPanel.Add(titleText);

        var contentText = new Text("Content here...", Font.GetDefault(), Color.LightGray)
            .Location(100, 100);
        customPanel.Add(contentText);

        // シーンに追加
        Root.Add(customPanel);
    }
}
```

### ワイプトランジション

```csharp title="画面ワイプ効果"
public class WipeTransition : Scene
{
    private MaskedContainer? _transitionContainer;
    private float _wipeProgress = 0.0f;
    private Sprite? _wipeMask;

    public override void OnStart()
    {
        // 次のシーンの内容
        var nextSceneContent = new Sprite(Window.TextureFactory.Load("assets/next_scene.png"));

        // ワイプマスク（横に広がる矩形）
        var maskTexture = Window.TextureFactory.Load("assets/wipe_mask.png");
        _wipeMask = new Sprite(maskTexture)
            .Scale(0.0f, 1.0f); // 初期状態: 横幅0

        _transitionContainer = new MaskedContainer(maskTexture);
        _transitionContainer.Add(nextSceneContent);

        Root.Add(_transitionContainer);
    }

    public override void OnUpdate()
    {
        // ワイプアニメーション
        _wipeProgress += 0.02f; // 毎フレーム2%ずつ

        if (_wipeProgress >= 1.0f)
        {
            _wipeProgress = 1.0f;
            // トランジション完了
        }

        // マスクのスケールを更新
        if (_wipeMask != null)
        {
            _wipeMask.Scale = (_wipeProgress, 1.0f);
        }
    }
}
```

## Containerのプロパティ継承

`MaskedContainer`は`Container`を継承しているため、すべてのContainerプロパティが使用できます。

```csharp title="Containerプロパティの使用"
var maskedContainer = new MaskedContainer(maskTexture)
    .Location(100, 100)
    .Scale(1.5f)
    .Angle(45); // 回転も可能

// トリミング機能も使用可能
var trimmableContainer = new MaskedContainer(maskTexture, isTrimmable: true);

Root.Add(maskedContainer);
```

詳細は[Containerのドキュメント](./container)を参照してください。

## マスクの動的変更

マスクテクスチャは実行時に変更できます。

```csharp title="マスクの動的変更"
var container = new MaskedContainer();

// 初期マスク
container.MaskTexture = maskTexture1;

// 後から変更
container.MaskTexture = maskTexture2;

// マスクを無効化（通常のContainerとして動作）
container.MaskTexture = null;

// マスキング方式の切り替え
container.UseAlphaMask = true; // アルファブレンディング方式に変更
```

## 注意事項

- **技術的な問題のため、現状 `MaskedContainer`の入れ子はサポートされていません。**<br/>
  入れ子にして使用した場合、意図せぬ挙動を起こす可能性があります。
- マスクテクスチャが`null`の場合、通常の`Container`として動作します
- ステンシルバッファ方式では、マスクの濃淡（RGB平均値）が0.5より大きい部分のみ表示されます
- アルファブレンディング方式では、フレームバッファを使用するためパフォーマンスに注意が必要です
- マスクテクスチャと子要素のサイズは一致させることを推奨します
