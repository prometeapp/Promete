# 名前空間と型の変更一覧

## 移動された型

| 型名 | v1 の名前空間 | v2 の名前空間 |
| --- | --- | --- |
| `CoordinateExtension` | `Promete.Nodes.Renderer` | `Promete.Graphics.Rendering` |
| `RenderingHelper` | `Promete.Nodes.Renderer` | `Promete.Graphics.Rendering` |
| `GLHelper` | `Promete.Nodes.Renderer.GL.Helper` | `Promete.Graphics.Rendering.GL` |
| `GLMaskedContainerHelper` | `Promete.Nodes.Renderer.GL.Helper` | `Promete.Graphics.Rendering.GL` |

`GLMaskedContainerHelper` は名前空間に加えて、コンストラクタ (`(PrometeApp, RenderCommandQueue, IRenderTextureProvider)`) と `RenderToTexture(MaskedContainer, RenderContext)` のシグネチャも変わっている。

## 名前が変わった型

| v1 の型 | v2 の型 |
| --- | --- |
| `Promete.Graphics.TextureFactory` | `Promete.Graphics.TextureFactoryBase` |
| `Promete.Windowing.GLDesktop.OpenGLTextureFactory` | `Promete.Windowing.GLDesktop.GLTextureFactory` |
| `Promete.Windowing.GLDesktop.OpenGLDesktopWindow` | `Promete.Backends.GL.OpenGLDesktopBackend` |
| `Promete.Graphics.IFrameBufferProvider` | `Promete.Graphics.IRenderTextureProvider` (メンバーは別物) |
| `Promete.Headless.HeadlessAppExtesion` | `Promete.Headless.HeadlessAppExtension` |

## 削除された型

| v1 の型 | 代替 |
| --- | --- |
| `Promete.Graphics.Fonts.IFont` | `Promete.Graphics.Fonts.Font` / `IGlyphSource` |
| `Promete.GLDesktop.GLFrameBufferProvider` | なし (内部実装)。`IRenderTextureProvider` を使う |
| `Promete.Nodes.Renderer.NodeRendererBase` | ノード自身の `Collect()` メソッド |
| `Promete.Nodes.Renderer.GL.GLSpriteRenderer` | `Sprite.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLTextRenderer` | `Text.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLShapeRenderer` | `Shape.Collect()` + `DrawPrimitiveCommand` |
| `Promete.Nodes.Renderer.GL.GLTilemapRenderer` | `Tilemap.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLNineSliceSpriteRenderer` | `NineSliceSprite.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLPieSpriteRenderer` | `PieSprite.Collect()` + `DrawPieTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLMaskedContainerRenderer` | `MaskedContainer.Collect()` + マスクコマンド |
| `Promete.Nodes.Renderer.GL.GLContainbleNodeRenderer` | `ContainableNode.Collect()` + `BeginTrimCommand` / `EndTrimCommand` |
| `Promete.Nodes.Renderer.GL.Helper.GLTextureRendererHelper` | `DrawTextureCommand` (キューが自動でバッチ化する) |
| `Promete.Nodes.Renderer.GL.Helper.GLPrimitiveRendererHelper` | `DrawPrimitiveCommand` + `Promete.Graphics.Rendering.GL.Runners.GLDrawPrimitiveCommandRunner` |
| `Promete.Nodes.Renderer.GL.Helper.GLPieSpriteRendererHelper` | `DrawPieTextureCommand` + `Promete.Graphics.Rendering.GL.Runners.GLDrawPieTextureCommandRunner` |

削除された型を継承・利用しているコードの書き換え方は [rendering.md](rendering.md) を参照。
