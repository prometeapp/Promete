---
title: カスタムグリフソース
description: Prometeで独自のグリフソース（IGlyphSource実装）を作成し、Fontとして利用する方法を解説します。
sidebar:
  order: 5
---

Prometeでは、`IGlyphSource` を実装することで、独自の方法で文字の形（グリフ）を用意し、`Font` として利用できます。
ここではグリフソースの仕組みと、作り方・使い方を解説します。

## グリフソースとは

`Font` クラスは、「グリフの供給元（グリフソース）」と「サイズやスタイルなどの描画設定」を組み合わせたものです。
TTF/OTF ファイルを読み込んだ場合も、内部では FreeType を使ったグリフソースが使われています。

グリフソースが担当するのは、「コードポイント（文字の番号）から、その文字のグリフを取り出す」ことだけです。
行の折り返しや整列、縁取りといった処理はエンジン側が行うため、グリフソースで実装する必要はありません。

## 画像フォントを使う場合

格子状に文字を並べた画像からビットマップフォントを作りたい場合は、自分で `IGlyphSource` を実装する必要はありません。
標準の `BitmapGlyphSource.FromGrid` を使いましょう。

```csharp title="画像からビットマップフォントを作る"
using Promete.Graphics.Fonts;

// 8x8 ピクセルのセルに、左上から順に文字が並んだ画像を読み込む
var source = BitmapGlyphSource.FromGrid("assets/font.png", (8, 8), " !\"#$%&'()*+,-./0123456789");

// グリフソースを Font に変換する（ドット絵なのでアンチエイリアスは無効にする）
var font = Font.FromGlyphSource(source, 16, FontStyle.Normal, false);

var text = new Text("123", font);
```

`BitmapGlyphSource` は、指定したフォントサイズを元の大きさの整数倍に丸めて拡大するため、ドットが崩れません。
上の例では 8 ピクセルの画像を 16 ピクセルで使っているので、ちょうど 2 倍に拡大されます。

## IGlyphSource を実装する

画像以外の方法でグリフを用意したい場合は、`IGlyphSource` インターフェースを実装します。
実装するメンバーは次のとおりです。

| メンバー | 役割 |
|---|---|
| `SourceId` | グリフソースを識別する ID。`GlyphSourceId.Next()` で採番します |
| `GetMetrics(options)` | アセンダー・ディセンダー・行の高さを返します |
| `TryGetGlyph(codepoint, options, out glyph)` | 文字のグリフ情報（送り幅など）を返します。持っていない文字なら `false` を返します |
| `Rasterize(glyph, options)` | グリフを画像（`GlyphBitmap`）に変換します |
| `GetKerning(left, right, options)` | 2 文字間のカーニング量を返します。対応しない場合は `0` を返します |
| `Dispose()` | 保持しているリソースを解放します（`IDisposable`） |

`options`（`GlyphRenderOptions`）には、フォントサイズ（`Size`）などの描画設定が入っています。

### サンプル：英数字を四角形で描くグリフソース

すべての英数字を、白く塗りつぶした四角形として描画する簡単な例です。

```csharp title="BoxGlyphSource.cs"
using System;
using Promete.Graphics.Fonts;

public class BoxGlyphSource : IGlyphSource
{
    // ID は必ず GlyphSourceId.Next() で採番する
    public int SourceId { get; } = GlyphSourceId.Next();

    public FontMetrics GetMetrics(in GlyphRenderOptions options)
    {
        // ベースラインより上に 8 割、下に 2 割の高さを持つ
        return new FontMetrics(options.Size * 0.8f, -options.Size * 0.2f, options.Size);
    }

    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
    {
        // スペースから「~」までの ASCII 文字だけを扱う
        if (codepoint is < 0x20 or > 0x7E)
        {
            glyph = default;
            return false;
        }

        glyph = new GlyphInfo
        {
            Source = this,
            GlyphIndex = (uint)codepoint,
            Codepoint = codepoint,
            Advance = options.Size * 0.6f,
        };
        return true;
    }

    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options)
    {
        // スペースは描画するものがない
        if (glyph.Codepoint == ' ')
            return GlyphBitmap.Empty;

        var width = (int)(options.Size * 0.5f);
        var height = (int)(options.Size * 0.7f);

        // RGBA8888 形式。すべて 255 にすると白い不透明な四角形になる
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, (byte)255);

        // Bearing はベースラインから画像の左上までの位置。上方向はマイナス
        return new GlyphBitmap(pixels, (width, height), (0, -height));
    }

    public float GetKerning(int left, int right, in GlyphRenderOptions options)
    {
        return 0;
    }

    public void Dispose()
    {
    }
}
```

### 実装のポイント

- `SourceId` は、グリフの画像をキャッシュする際の目印になります。自分で数値を決めず、必ず `GlyphSourceId.Next()` で採番してください
- `Rasterize` が返す画像は RGBA8888 形式です。単色の文字は RGB を白にして、アルファ値で濃さを表します。文字色は `Text` の `Color` で着色されます
- `GlyphBitmap` の `Bearing` は、ベースライン上の原点から画像の左上までのずれです。Y 軸は下向きが正なので、ベースラインより上に描く場合はマイナスの値になります
- `Rasterize` は、グリフを初めて描画するときにだけ呼ばれます。結果はエンジン側でキャッシュされます
- 縁取りはエンジン側で `Rasterize` の結果から生成されるため、グリフソースで対応する必要はありません

## Fontとして利用する

作成したグリフソースは、`Font.FromGlyphSource` で `Font` に変換して使います。

```csharp title="カスタムグリフソースでテキストを表示する"
public class GameScene : Scene
{
    private BoxGlyphSource _source;

    public override void OnStart()
    {
        _source = new BoxGlyphSource();
        var font = Font.FromGlyphSource(_source, 24);

        var text = new Text("Hello", font).Location(16, 16);
        Root.Add(text);
    }

    public override void OnDestroy()
    {
        // 使い終わったグリフソースは破棄する
        _source.Dispose();
    }
}
```

### 他のフォントと組み合わせる

グリフソースは、既存のフォントと組み合わせることもできます。

- `font.WithFallback(source)`<br/>フォントが持っていない文字を、`source` で補います
- `font.WithOverride(source)`<br/>`source` を優先して参照します。外字を差し込む場合に使います

```csharp title="外字を重ねる"
// 名前を付けてアイコン画像を登録する
var icons = new BitmapGlyphSource(16);
icons.Register("heart", "assets/heart.png");

// 本文のフォントに外字を重ねる
var text = new Text("", Font.GetDefault(16).WithOverride(icons));
text.UseRichText = true;
text.Content = "ライフ <tex=heart><tex=heart><tex=heart>";
```

名前を付けて登録したグリフは、PTML の `<tex=名前>` で本文に差し込めます。詳しくは[PTML（リッチテキスト）](/guide/text/ptml)を参照してください。

## ノート

- 持っていない文字に対して `TryGetGlyph` が `false` を返した場合、その文字は描画されません
- `Font.FromGlyphSource` に渡したグリフソースは自動では破棄されません。不要になったら `Dispose()` を呼んでください
- 画像から作るビットマップフォントや外字であれば、`BitmapGlyphSource` を使うのが手軽です
