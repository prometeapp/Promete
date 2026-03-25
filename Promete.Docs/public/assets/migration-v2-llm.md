# Promete v1 → v2 移行ガイド（LLM向け）

このドキュメントは、Promete v1 で作られたプロジェクトを v2 に移行するための手順をまとめたものです。

---

## アプリケーション開発者向け手順

### ステップ1: ターゲットフレームワークを更新する

プロジェクトファイル（`.csproj`）の `TargetFramework` を `net10.0` に変更する。

```xml
<!-- Before -->
<TargetFramework>net8.0</TargetFramework>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```

変更後、`dotnet restore` を実行してパッケージを再取得する。

### ステップ2: Promete パッケージを更新する

NuGet パッケージを v2 系の最新版に更新する。

```sh
dotnet add package Promete
```

プラグインパッケージを使用している場合は合わせて更新する（例：`Promete.ImGui`）。

### ステップ3: `IWindow` の参照を `View` / `Time` に置き換える

v1 では `IWindow` がウィンドウ操作と時間管理の両方を担っていたが、v2 ではそれぞれ `IGameView`（`View`）と `ITimeProvider`（`Time`）に分割された。

シーン内の `window.*` の呼び出しを次の表を参考に書き換える。

| v1 | v2 |
|----|----|
| `window.Size` / `Window.Size` | `View.Size` |
| `window.Title` / `Window.Title` | `View.Title` |
| `window.Location` | `View.Location` |
| `window.Scale` | `View.Scale` |
| `window.IsFullScreen` | `View.IsFullScreen` |
| `window.Mode` | `View.Mode` |
| `window.TakeScreenshot()` | `View.TakeScreenshot()` |
| `window.DeltaTime` | `Time.DeltaTime` |
| `window.TotalTime` | `Time.TotalTime` |
| `window.FramePerSeconds` | `Time.FramePerSeconds` |
| `window.TargetFps` | `Time.TargetFps` |
| `window.TimeScale` | `Time.TimeScale` |

コンストラクタで `IWindow` を受け取っていたシーンは、コンストラクタから削除して `View` / `Time` プロパティを直接使うように変更する。

**Before (v1):**
```csharp
public class MainScene(IWindow window) : Scene
{
    public override void OnUpdate()
    {
        var dt = window.DeltaTime;
        var size = window.Size;
        window.Title = "My Game";
    }
}
```

**After (v2):**
```csharp
public class MainScene() : Scene
{
    public override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var size = View.Size;
        View.Title = "My Game";
    }
}
```

### ステップ4: `TextureFactory` の参照先を変更する

テクスチャの読み込みで `Window.TextureFactory` を使っている箇所を `App.TextureFactory` に置き換える。

**Before (v1):**
```csharp
var texture = Window.TextureFactory.Load("image.png");
```

**After (v2):**
```csharp
var texture = App.TextureFactory.Load("image.png");
```

またはコンストラクタ経由で `TextureFactoryBase` をインジェクションして取得することもできる。

```csharp
public class MyScene(TextureFactoryBase textureFactory) : Scene
{
    public override void OnStart()
    {
        var texture = textureFactory.Load("./path/to/texture.png");
    }
}
```

### ステップ5: `Angle` 型の変更に対応する

ノードの回転角度の型が `float` から `Angle` 構造体に変更された。`float` との暗黙的な変換はできないので、`Angle` を受け取るプロパティ・メソッドへの `float` / `int` の直接代入はコンパイルエラーになる。

拡張プロパティ `.Degrees` / `.Radians` を使って `Angle` を生成する。

**Before (v1):**
```csharp
sprite.Angle = 90f;
sprite.Angle += 1f;
```

**After (v2):**
```csharp
sprite.Angle = 90.Degrees;           // int から度数法
sprite.Angle = 90.0f.Degrees;        // float から度数法
sprite.Angle += 1.Degrees;
sprite.Angle = MathF.PI.Radians;     // float からラジアン

// 静的メソッドも引き続き使用可能
sprite.Angle = Angle.FromDegrees(90);
sprite.Angle = Angle.FromRadians(MathF.PI / 2);
```

`float` で角度を管理しているコードは次のように書き換える。

**Before (v1):**
```csharp
float angle = 0;
angle += Window.DeltaTime * 90;
sprite.Angle = angle;
```

**After (v2):**
```csharp
float angle = 0;
angle += Time.DeltaTime * 90;
sprite.Angle = angle.Degrees;
```

`float` として取り出す場合は `ToDegrees()` / `ToRadians()` メソッドを使う。

```csharp
float deg = sprite.Angle.ToDegrees();
float rad = sprite.Angle.ToRadians();
```

`Vector` / `VectorInt` の `.Angle()` メソッドの戻り値も `float`（ラジアン）から `Angle` 型に変わったため、当該箇所の修正を行う。

**Before (v1):**
```csharp
float angle = player.Location.Angle(mouse.Position);
sprite.Angle = angle;
```

**After (v2):**
```csharp
Angle angle = player.Location.Angle(mouse.Position);
sprite.Angle = angle;
```

### ステップ6: ビルドエラーと警告を確認する

ここまでの手順を終えたら一度ビルドする。`IWindow` を参照している箇所に `[Obsolete]` 警告が出るため、一覧を見て手順3・4で対応できていない箇所が残っていないか確認する。

`[Obsolete]` 警告が出ていても動作自体はする。警告が0になるまで順番に対応すればよい。

---

## プラグイン・バックエンド開発者向け追加手順

ゲームロジックのみ書いていて、カスタムノードやバックエンドを実装していない場合はここから先は不要。

### ステップ7: `NodeRenderer` を `Collect()` に移行する

v1 では各ノードタイプに対して `NodeRendererBase` を継承したレンダラーを実装し `UseRenderer<TNode, TRenderer>()` で登録していたが、この仕組みは v2 で廃止された。

代わりに、ノード自身が `Collect()` メソッドをオーバーライドしてレンダリングコマンドをキューに追加する方式に変わった。

**Before (v1):**
```csharp
public class MyNode : Node { ... }

public class MyNodeRenderer : NodeRendererBase
{
    public override void Render(Node node) { ... }
}

app.UseRenderer<MyNode, MyNodeRenderer>();
```

**After (v2):**
```csharp
public class MyNode : Node
{
    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        queue.Enqueue(new DrawTextureCommand
        {
            Texture = _texture,
            ModelMatrix = ModelMatrix,
            TintColor = Color.White,
            Width = Size.X,
            Height = Size.Y,
        });
    }
}
```

登録コード（`UseRenderer<,>()`）はすべて削除する。

### ステップ8: バックエンドを `BackendBase` に移行する

v1 では `IWindow` インターフェースを直接実装してバックエンドを構成していたが、v2 では `BackendBase` 抽象クラスを継承する方式に変わった。

各抽象メソッドを実装することで、ウィンドウ・時間管理・入力・レンダリングなどの責務を個別に提供する。

```csharp
public class MyBackend : BackendBase
{
    public override void OnInitialize(PrometeApp app, WindowOptions windowOptions) { ... }
    public override ITimeProvider SetupTimeProvider() => new MyTimeProvider();
    public override IGameView SetupGameView() => new MyGameView();
    public override InputProvider SetupInputProvider() => new MyInputProvider();
    public override IScreenBlitter SetupScreenBlitter() => new MyScreenBlitter();
    public override TextureFactoryBase SetupTextureFactory() => new MyTextureFactory();
    public override IRenderTextureProvider SetupRenderTextureProvider() => new MyRenderTextureProvider();
    public override IShaderFactory SetupShaderFactory() => new MyShaderFactory();
    public override void OnStart(PrometeApp app) { ... }
    public override void OnExit(PrometeApp app) { ... }
}
```

バックエンドの登録は `Build<T>()` で行う。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Build<MyBackend>(WindowOptions.Default);
```

廃止・リネームされた型：

| v1 の型 | v2 の代替 |
|---------|-----------|
| `IFrameBufferProvider` | `IRenderTextureProvider` |
| `GLFrameBufferProvider` | `GLRenderTextureProvider` |
| `OpenGLDesktopWindow` | `OpenGLDesktopBackend` |
| `TextureFactory`（抽象クラス） | `TextureFactoryBase` |
| `OpenGLTextureFactory` | `GLTextureFactory` |

---

## 名前空間の変更一覧

### 移動された型

| 型名 | v1 の名前空間 | v2 の名前空間 |
|-------|--------------|--------------|
| `TextureFactoryBase` | `Promete.Graphics` | `Promete.Graphics` |
| `GLTextureFactory` | `Promete.Windowing.GLDesktop` | `Promete.Windowing.GLDesktop` |
| `CoordinateExtension` | `Promete.Nodes.Renderer` | `Promete.Graphics.Rendering` |
| `RenderingHelper` | `Promete.Nodes.Renderer` | `Promete.Graphics.Rendering` |
| `GLHelper` | `Promete.Nodes.Renderer.GL.Helper` | `Promete.Graphics.Rendering.GL` |
| `GLMaskedContainerHelper` | `Promete.Nodes.Renderer.GL.Helper` | `Promete.Graphics.Rendering.GL` |
| `GLDrawPieTextureCommandRunner` | `Promete.Nodes.Renderer.GL.Helper` | `Promete.Graphics.Rendering.GL.Runners` |
| `GLDrawPrimitiveCommandRunner` | `Promete.Nodes.Renderer.GL.Helper` | `Promete.Graphics.Rendering.GL.Runners` |

### 削除された型

| v1 の型 | 代替 |
|---------|------|
| `Promete.Windowing.GLDesktop.OpenGLDesktopWindow` | `Promete.Backends.GL.OpenGLDesktopBackend` |
| `Promete.Graphics.IFrameBufferProvider` | `Promete.Graphics.IRenderTextureProvider` |
| `Promete.GLDesktop.GLFrameBufferProvider` | `Promete.GLDesktop.GLRenderTextureProvider` |
| `Promete.Nodes.Renderer.NodeRendererBase` | ノード自身の `Collect()` メソッド |
| `Promete.Nodes.Renderer.GL.GLSpriteRenderer` | `Sprite.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLTextRenderer` | `Text.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLShapeRenderer` | `Shape.Collect()` + `DrawPrimitiveCommand` |
| `Promete.Nodes.Renderer.GL.GLTilemapRenderer` | `Tilemap.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLNineSliceSpriteRenderer` | `NineSliceSprite.Collect()` + `DrawTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLPieSpriteRenderer` | `PieSprite.Collect()` + `DrawPieTextureCommand` |
| `Promete.Nodes.Renderer.GL.GLMaskedContainerRenderer` | `MaskedContainer.Collect()` + マスクコマンド |
| `Promete.Nodes.Renderer.GL.Helper.GLTextureRendererHelper` | `GLDrawTextureBatchedCommandRunner` |
