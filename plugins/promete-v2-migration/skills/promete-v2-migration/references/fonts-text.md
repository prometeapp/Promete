# フォントとテキスト

フォント機能が刷新され、テキストのレイアウトと描画を Promete 自身が行うようになった。

## そのまま使える API

- `Font.FromFile(path, ...)` / `Font.FromSystem(name, ...)` / `Font.GetDefault()` / `font.With(...)`
- `Text` ノードのコンストラクタ
- `Text` の主要なプロパティ: `Content` / `Color` / `Font` / `BorderColor` / `BorderThickness` / `LineSpacing` / `UseRichText` など

## 変更・削除された API

| v1 | v2 |
| --- | --- |
| `IFont` | 削除。`Font` を使う |
| `Font.FromFile(Stream stream, ...)` | `Font.FromStream(stream, ...)` |
| `Font.FromSystem(name, CultureInfo culture, ...)` | 削除。`Font.FromSystem(name, ...)` を使う |
| `font.GetTextBounds(text, options)` (戻り値 `Rect`) | `TextLayoutEngine.Measure(text, font, options)` (戻り値 `VectorInt`) |
| `font.GenerateTexture(...)` | 削除 (代替なし) |
| `Text.WordWrap` / `TextRenderingOptions.WordWrap` | `WrapMode` (`None` / `Character` / `Word` / `Mixed`) |
| `Text.RenderTexture()` | `Text.UpdateLayout()` |
| `Text.RenderedTexture` | 削除。サイズは `Text.Size` または `Text.Layout.Size` で取得する |

```csharp
// v1
var font = Font.FromFile(stream, 24);
var bounds = font.GetTextBounds("Hello", new TextRenderingOptions());

text.WordWrap = true;
text.RenderTexture();
var size = text.RenderedTexture?.Size;

// v2
var font = Font.FromStream(stream, 24);
var size = TextLayoutEngine.Measure("Hello", font, new TextRenderingOptions());

text.WrapMode = WrapMode.Word; // 和文を含む場合は WrapMode.Mixed
text.UpdateLayout();
var textSize = text.Layout.Size;
```

`GetTextBounds` の戻り値は `Rect`、`TextLayoutEngine.Measure` の戻り値は `VectorInt` (サイズのみ)。

- v1 の `GetTextBounds` は常に位置 (0, 0) の `Rect` を返していた。位置成分を使っていた箇所は 0 (`VectorInt.Zero`) に置き換えればよい
- サイズは `.Width` / `.Height` を `.X` / `.Y` に置き換える
- サイズの値そのものは v1 と一致しない。v1 は描画された文字の外接矩形 + 1px、v2 は文字の送り幅 × 行の高さ。サイズを元にレイアウトしている画面は目視確認が必要

`WordWrap = true` を置き換えるときは、`WrapMode.Mixed` を選べばよい。日本語は文字単位、英語は単語単位で折り返すので、v1 の挙動に近い。英語のみの文字列なら `WrapMode.Word` と同じ結果になる。

`WrapMode` は `Promete.Graphics.Fonts` 名前空間にある。`Text` しか使っていなかったファイルでは `using` の追加が必要になる。

`font.GenerateTexture(...)` に代替はない。使っている場合は、`Text` ノードを `RenderTexture` に描画する方法などを検討し、ユーザーに相談する。

## 動作上の注意

- `Font.FromSystem` は、フォントが見つからない場合に `FontException` をスローする。v1 では SixLabors.Fonts の例外がそのまま出ていたので、それを `catch` している箇所は `FontException` に置き換える
- `Font.FromStream` は呼び出すたびに新しいフォントを読み込む。サイズ違いのフォントが必要なときは、読み込み直さずに `font.With(size)` を使う

見た目に影響する変更 (禁則処理、カーニング、`Text.Size` の計算方法、縁取り、`TextColor` の既定値) は SKILL.md の「目視確認が必要な項目」を参照。

## 独自フォント (IFont の実装) を IGlyphSource に移行する

`IFont` は削除された。v2 ではレイアウトを `TextLayoutEngine` が行い、フォントの実装は文字ごとのグリフを供給するだけになった。独自のフォントは `IGlyphSource` を実装し、`Font.FromGlyphSource` で `Font` に変換して使う。

- 実装するメンバー: `SourceId` / `GetMetrics` / `TryGetGlyph` / `Rasterize` / `GetKerning`、および `IDisposable`
- `SourceId` は `GlyphSourceId.Next()` で採番する
- `Rasterize` が返す `GlyphBitmap` は RGBA8888 形式。`Bearing` はベースラインを原点とした値にする

画像から作るビットマップフォントなら、自分で実装しなくても `BitmapGlyphSource` で置き換えられる。

```csharp
var source = BitmapGlyphSource.FromGrid("font.png", (8, 8), " !\"#$%&'()*+,-./0123456789");
var font = Font.FromGlyphSource(source, 16, FontStyle.Normal, false);
```
