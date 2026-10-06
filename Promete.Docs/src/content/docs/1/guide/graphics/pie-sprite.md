---
title: PieSprite
description: テクスチャを扇状（円グラフ状）に描画するPieSpriteノードの使用方法について解説します。
sidebar:
  order: 5
  badge: v1.3.0～
---
**Promete v1.3.0からサポート**

`PieSprite`は、テクスチャを扇状（円グラフ状）に描画するためのノードです。`Sprite`を継承しており、ローディングゲージ、クールダウンタイマー、円グラフなど、進行状況を視覚的に表現する際に便利です。

## 作成

`PieSprite`の基本的な作成方法は`Sprite`と同じです。テクスチャを読み込み、コンストラクタに渡します。

```csharp title="基本的なPieSpriteの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // テクスチャを読み込み
        var texture = Window.TextureFactory.Load("assets/gauge.png");

        // PieSpriteを作成
        var pieSprite = new PieSprite(texture);

        // シーンに追加
        Root.Add(pieSprite);
    }
}
```

## 扇形の制御

### 描画範囲の指定

`StartPercent`と`Percent`プロパティで、テクスチャの描画範囲を制御できます。

```csharp title="描画範囲の設定"
var pieSprite = new PieSprite(texture);

// 開始位置: 真上（12時方向）
pieSprite.StartPercent = 0.0f;

// 終了位置: 25%（3時方向）
pieSprite.Percent = 25.0f;

Root.Add(pieSprite);
```

:::note
**座標系の基準:**
- `0%`: 真上（12時方向）
- `25%`: 右（3時方向）
- `50%`: 真下（6時方向）
- `75%`: 左（9時方向）
- `100%`: 真上に戻る（1周）

両方のプロパティとも、0.0 ～ 100.0 の範囲で指定します。
:::

### 時計回りアニメーション

パーセントを変更することで、アニメーション効果を実現できます。

```csharp title="ローディングゲージの実装"
public class LoadingScene : Scene
{
    private PieSprite? _loadingGauge;
    private float _progress = 0.0f;

    public override void OnStart()
    {
        var texture = Window.TextureFactory.Load("assets/loading.png");

        _loadingGauge = new PieSprite(texture)
            .Location(Window.Width / 2, Window.Height / 2)
            .Pivot(0.5f, 0.5f); // 中心を基準点に

        Root.Add(_loadingGauge);
    }

    public override void OnUpdate()
    {
        if (_loadingGauge == null) return;

        // 進行状況を更新
        _progress += 0.5f; // 毎フレーム0.5%ずつ増加

        if (_progress >= 100.0f)
        {
            _progress = 100.0f;
            // ローディング完了
            App.LoadScene<GameScene>();
        }

        // PieSpriteに反映
        _loadingGauge.Percent = _progress;
    }
}
```

### 部分的な扇形表示

`StartPercent`と`Percent`を両方使用することで、円の一部だけを描画できます。

```csharp title="円の一部を表示"
// 右半分のみ表示（3時 ～ 9時）
var rightHalf = new PieSprite(texture)
    .Location(100, 100);
rightHalf.StartPercent = 25.0f;  // 3時から開始
rightHalf.Percent = 75.0f;       // 9時まで

// 上半分のみ表示（9時 ～ 3時）
var topHalf = new PieSprite(texture)
    .Location(250, 100);
topHalf.StartPercent = 75.0f;  // 9時から開始
topHalf.Percent = 25.0f;       // 3時まで（次の周）

Root.AddRange(rightHalf, topHalf);
```

:::caution
`Percent`が`StartPercent`より小さい値の場合、0%（真上）を跨いで描画されます。
:::

## Spriteのプロパティ継承

`PieSprite`は`Sprite`を継承しているため、すべてのSpriteプロパティが使用できます。

```csharp title="Spriteプロパティの使用"
var pieSprite = new PieSprite(texture)
    .Location(200, 150)
    .Scale(2.0f)
    .Pivot(0.5f, 0.5f)
    .TintColor(Color.FromArgb(200, 255, 200, 100))
    .ZIndex(10);

pieSprite.StartPercent = 0.0f;
pieSprite.Percent = 50.0f;

Root.Add(pieSprite);
```

詳細は[Spriteのドキュメント](./sprite)を参照してください。

## 実用例

### クールダウンタイマー

スキルのクールダウン表示に使用できます。

```csharp title="クールダウンタイマー"
public class SkillButton(Keyboard keyboard) : Container
{
    private PieSprite? _cooldownOverlay;
    private float _cooldownTime = 0.0f;
    private const float MaxCooldown = 5.0f; // 5秒

    public override void OnStart()
    {
        // スキルアイコン
        var iconTexture = Window!.TextureFactory.Load("assets/skill_icon.png");
        var icon = new Sprite(iconTexture);
        Add(icon);

        // クールダウンオーバーレイ（半透明の黒）
        var overlayTexture = Window.TextureFactory.Load("assets/black_circle.png");
        _cooldownOverlay = new PieSprite(overlayTexture)
            .TintColor(Color.FromArgb(128, 0, 0, 0)); // 半透明

        _cooldownOverlay.StartPercent = 0.0f;
        _cooldownOverlay.Percent = 0.0f;
        _cooldownOverlay.IsVisible = false;

        Add(_cooldownOverlay);
    }

    public override void OnUpdate()
    {
        // スキル使用
        if (keyboard.Space.IsKeyDown && _cooldownTime <= 0.0f)
        {
            UseSkill();
            _cooldownTime = MaxCooldown;
            _cooldownOverlay!.IsVisible = true;
        }

        // クールダウン更新
        if (_cooldownTime > 0.0f)
        {
            _cooldownTime -= App.DeltaTime;

            // 進行状況を計算（100% → 0%に減少）
            var progress = (_cooldownTime / MaxCooldown) * 100.0f;
            _cooldownOverlay!.Percent = progress;

            if (_cooldownTime <= 0.0f)
            {
                _cooldownTime = 0.0f;
                _cooldownOverlay.IsVisible = false;
            }
        }
    }

    private void UseSkill()
    {
        // スキル発動処理
    }
}
```

### 円形のHPゲージ

```csharp title="円形HPゲージ"
public class CircularHealthBar : Container
{
    private PieSprite? _hpGauge;
    private int _currentHp = 100;
    private int _maxHp = 100;

    public override void OnStart()
    {
        var gaugeTexture = Window!.TextureFactory.Load("assets/hp_gauge.png");

        _hpGauge = new PieSprite(gaugeTexture)
            .Pivot(0.5f, 0.5f);

        _hpGauge.StartPercent = 0.0f;
        _hpGauge.Percent = 100.0f; // 初期状態: 満タン

        Add(_hpGauge);
    }

    public void TakeDamage(int damage)
    {
        _currentHp = Math.Max(0, _currentHp - damage);
        UpdateGauge();
    }

    public void Heal(int amount)
    {
        _currentHp = Math.Min(_maxHp, _currentHp + amount);
        UpdateGauge();
    }

    private void UpdateGauge()
    {
        var hpPercent = (_currentHp / (float)_maxHp) * 100.0f;
        _hpGauge!.Percent = hpPercent;

        // HPに応じて色を変更
        if (hpPercent > 50.0f)
            _hpGauge.TintColor = Color.Green;
        else if (hpPercent > 25.0f)
            _hpGauge.TintColor = Color.Yellow;
        else
            _hpGauge.TintColor = Color.Red;
    }
}
```

## 注意事項

- `StartPercent`と`Percent`は自動的に0.0 ～ 100.0の範囲にクランプされます
- テクスチャの中心を基準に扇形が描画されるため、通常は`Pivot`を`(0.5f, 0.5f)`に設定することを推奨します
- `Percent`が`StartPercent`より小さい場合、0%（真上）を跨いで描画されます
