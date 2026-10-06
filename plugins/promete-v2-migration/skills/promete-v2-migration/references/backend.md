# 独自バックエンドの移行

v1 では `IWindow` を直接実装してバックエンドを構成していた。v2 では `BackendBase` 抽象クラスを継承し、責務ごとの抽象メソッドを実装する。

バックエンドの移行は作り直しに近い規模になる。着手前に、移行の範囲と方針をユーザーに確認する。

```csharp
public class MyBackend : BackendBase
{
    public override void OnInitialize(PrometeApp app, WindowOptions windowOptions) { /* ... */ }
    public override ITimeProvider SetupTimeProvider() => new MyTimeProvider();
    public override IGameView SetupGameView() => new MyGameView();
    public override InputProvider SetupInputProvider() => new MyInputProvider();
    public override IScreenBlitter SetupScreenBlitter() => new MyScreenBlitter();
    public override TextureFactoryBase SetupTextureFactory() => new MyTextureFactory();
    public override IRenderTextureProvider SetupRenderTextureProvider() => new MyRenderTextureProvider();
    public override IShaderFactory SetupShaderFactory() => new MyShaderFactory();
    public override void OnStart(PrometeApp app) { /* ... */ }
    public override void OnExit(PrometeApp app) { /* ... */ }
}
```

登録は `Build<T>()` で行う。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Build<MyBackend>(WindowOptions.Default);
```

## 実装上の注意

- **呼び出し順**: `OnInitialize` → 各 `Setup*` の順で呼ばれ、その後に DI コンテナが構築される。`OnInitialize` の時点で取得できるプラグインは、`Use<T>(instance)` でインスタンスとして登録されたものだけ
- **ゲームループ**: `OnStart` では、`app.OnStart()` を 1 度呼んだ後、ゲームループの中で `app.OnUpdate()` / `app.OnRender()` を呼ぶ。終了時には `app.OnDestroy()` を呼ぶ
- **コマンドランナー**: 描画はコマンドキューで行われる ([rendering.md](rendering.md) を参照)。`RenderCommandQueue` は `Build<T>()` が自動で登録するので、`app.GetPlugin<RenderCommandQueue>()` で取得し、標準のコマンドに対応するランナーを `RegisterRunner` で登録する。テクスチャの描画は、連続する `DrawTextureCommand` をまとめた `DrawTextureBatchedCommand` として届く
- **`TextureFactoryBase`**: v1 の `TextureFactory` から名前が変わった。テクスチャの一部を書き換える抽象メソッド `Update(Texture2D, VectorInt, VectorInt, byte[])` が追加されたほか、`LoadFromImageSharpImage(Image)` も `protected override` で実装する必要がある。テクスチャは `new Texture2D(handle, size, onDispose)` で生成する
- **`IRenderTextureProvider`**: v1 の `IFrameBufferProvider` とはメンバーがまったく異なるため、作り直しになる。`Create` では `new RenderTexture(size, texture, this)` を返す。`Resize` でテクスチャを作り直した場合は `RenderTexture.Texture` を差し替える
- **`IShaderFactory`**: `Compile` でシェーダーをコンパイルし、`ShaderProgram.SetCompiledData(handle, onDispose)` で結果を設定する。マテリアルの Uniform 値は `Material.Uniforms` で取得できる

## 廃止・リネームされた型

| v1 の型 | v2 の代替 |
| --- | --- |
| `IWindow` (バックエンドとして) | `BackendBase` |
| `IFrameBufferProvider` | `IRenderTextureProvider` (メンバーは別物) |
| `OpenGLDesktopWindow` | `OpenGLDesktopBackend` |
| `TextureFactory` (抽象クラス) | `TextureFactoryBase` |
| `OpenGLTextureFactory` | `GLTextureFactory` |
| `GLFrameBufferProvider` | 内部実装になった。`IRenderTextureProvider` を使う |
| `HeadlessAppExtesion` | `HeadlessAppExtension` (綴りの修正) |
