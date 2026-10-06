# IWindow の分割と関連する変更

v1 の `IWindow` はウィンドウ操作・時間管理・イベントのすべてを担っていた。v2 では次のように分割された。

- 画面に関するもの: `IGameView` (シーンからは `View` プロパティ)
- 時間に関するもの: `ITimeProvider` (シーンからは `Time` プロパティ)
- ゲームループのイベントと終了処理: `PrometeApp` (シーンからは `App` プロパティ)

`IGameView` と `ITimeProvider` は `Promete.Backends` 名前空間にある。

## メンバーの対応表

| v1 | v2 |
| --- | --- |
| `window.Size` / `Width` / `Height` | `View.Size` / `Width` / `Height` |
| `window.ActualSize` / `ActualWidth` / `ActualHeight` | `View.ActualSize` / `ActualWidth` / `ActualHeight` |
| `window.Location` / `X` / `Y` | `View.Location` / `X` / `Y` |
| `window.Title` | `View.Title` |
| `window.Scale` | `View.Scale` |
| `window.PixelRatio` | `View.PixelRatio` |
| `window.Mode` | `View.Mode` |
| `window.IsFullScreen` / `IsVisible` / `IsFocused` / `TopMost` | `View.IsFullScreen` / `IsVisible` / `IsFocused` / `TopMost` |
| `window.TakeScreenshot()` / `SaveScreenshotAsync()` | `View.TakeScreenshot()` / `SaveScreenshotAsync()` |
| `window.FileDropped` / `Resize` (イベント) | `View.FileDropped` / `Resize` |
| `window.DeltaTime` / `TotalTime` / `TotalTimeWithoutScale` | `Time.DeltaTime` / `TotalTime` / `TotalTimeWithoutScale` |
| `window.TotalFrame` | `Time.TotalFrame` |
| `window.FramePerSeconds` / `UpdatePerSeconds` | `Time.FramePerSeconds` / `UpdatePerSeconds` |
| `window.TargetFps` / `TargetUps` | `Time.TargetFps` / `TargetUps` |
| `window.TimeScale` | `Time.TimeScale` |
| `window.Start` / `Update` / `Render` / `Destroy` (イベント) | `App.Start` / `Update` / `Render` / `Destroy` |
| `window.PreUpdate` / `PostUpdate` (イベント) | `App.PreUpdate` / `PostUpdate` |
| `window.Exit()` | `App.Exit()` |
| `window.IsVsyncMode` | 起動時の `WindowOptions.IsVsyncMode` のみ。実行中の変更はできない |
| `window.TextureFactory` | `App.TextureFactory` (型は `TextureFactoryBase`) |

v2 では `App` に `PreRender` / `PostRender` イベントも追加されている。

### Render イベントのタイミングの違い

`window.Render` → `App.Render` は名前の上では対応しているが、呼ばれるタイミングが違う。

- v1 の `window.Render`: シーンの描画が終わった後に、画面に対して呼ばれていた
- v2 の `App.Render`: オフスクリーンへの描画中、シーンの描画コマンドを集めた後で、それを実行する前に呼ばれる
- v2 の `App.PostRender`: シーンを画面に転送した後に呼ばれる

v1 の `Render` で GL を直接呼んで画面に重ね描きしていたハンドラは、`App.Render` に移すとシーンの下に描かれるか、画面に出ない。`App.PostRender` に移すか、描画コマンド ([rendering.md](rendering.md)) に置き換える。公式の `ImGuiPlugin` も v2 では `PostRender` で描画している。

## シーン

`IWindow` をコンストラクタで受け取っていたシーンは、その引数を削除し、`View` / `Time` / `App` プロパティを使う。

```csharp
// v1
public class MainScene(IWindow window) : Scene
{
    public override void OnUpdate()
    {
        var dt = window.DeltaTime;
        var size = window.Size;
        window.Title = "My Game";
    }
}

// v2
public class MainScene : Scene
{
    public override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var size = View.Size;
        View.Title = "My Game";
    }
}
```

## シーン以外のクラス (自作プラグインなど)

コンストラクタで `IGameView` / `ITimeProvider` / `PrometeApp` のうち必要なものを受け取る。

```csharp
using Promete.Backends;

public class MyPlugin(IGameView view, ITimeProvider time)
{
    // ...
}
```

`IWindow` のイベントを購読していた場合は `PrometeApp` のイベントに置き換える。

```csharp
// v1
window.Update += OnUpdate;

// v2
app.Update += OnUpdate;
```

v1 の `IWindow._RawInputContext` で Silk.NET の入力コンテキストを取得していた場合は、`InputProvider` (`Promete.Backends.SilkNetCommon` 名前空間) を DI で受け取り、`CreateInput()` を呼ぶ。

```csharp
using Promete.Backends.SilkNetCommon;

public class MyInputPlugin(InputProvider inputProvider) : IInitializable
{
    public void OnStart()
    {
        var input = inputProvider.CreateInput();
    }
}
```

## TextureFactory

`Window.TextureFactory` は `App.TextureFactory` に置き換える。

```csharp
// v1
var texture = Window.TextureFactory.Load("image.png");

// v2
var texture = App.TextureFactory.Load("image.png");
```

コンストラクタで `TextureFactoryBase` を注入して使ってもよい。

```csharp
public class MyScene(TextureFactoryBase textureFactory) : Scene
{
    public override void OnStart()
    {
        var texture = textureFactory.Load("./path/to/texture.png");
    }
}
```

変数やフィールドの型を `TextureFactory` と明示している場合は、`TextureFactoryBase` に変更する。

## WindowOptions の渡し方

`PrometeApp.Run(WindowOptions)` / `Run<TScene>(WindowOptions)` は削除された。`WindowOptions` はバックエンドを構築するときに渡す。

```csharp
// v1
var app = PrometeApp.Create()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>(WindowOptions.Default with
{
    Title = "Promete Demo",
    Mode = WindowMode.Resizable,
    TargetFps = 0,
    TargetUps = 0,
    IsVsyncMode = false,
});

// v2
var app = PrometeApp.Create()
    .BuildWithOpenGLDesktop(WindowOptions.Default with
    {
        Title = "Promete Demo",
        Mode = WindowMode.Resizable,
        TargetFps = 0,
        TargetUps = 0,
        IsVsyncMode = false,
    });

return app.Run<MainScene>();
```

## 同梱プラグインのコンストラクタ

`Promete.ImGui` の `ImGuiPlugin` のコンストラクタが `(PrometeApp, IWindow)` から `(PrometeApp, InputProvider)` に変わった。継承している場合は書き換える。

```csharp
// v1
public class MyImGuiPlugin(PrometeApp app, IWindow window) : ImGuiPlugin(app, window)
{
    protected override void OnConfigure(ImGuiIOPtr io) { /* ... */ }
}

// v2
using Promete.Backends.SilkNetCommon;

public class MyImGuiPlugin(PrometeApp app, InputProvider provider) : ImGuiPlugin(app, provider)
{
    protected override void OnConfigure(ImGuiIOPtr io) { /* ... */ }
}
```

`Keyboard` / `Mouse` / `Gamepads` / `ConsoleLayer` / `CoroutineManager` のコンストラクタも `IWindow` を受け取らなくなった。DI 経由で取得している場合は影響ない。自分で `new` している場合は、DI から取得する形 (コンストラクタ注入や `App.GetPlugin<T>()`) に変更する。

v1 で自分で `new` していたプラグインは、DI に登録されていないことが多い。`PrometeApp.Create()` のチェーンに `.Use<Keyboard>()` などがあるか確認し、なければ追加する。未登録のまま `GetPlugin<T>()` を呼ぶと `ArgumentException` になる。

## IWindow を残した場合の挙動

`IWindow` は後方互換のために残されており、参照箇所には `[Obsolete]` 警告が出る。多くのメンバーは動作するが、次のものはそのままでは動かない。

- `IWindow.Run(WindowOptions)`: `NotSupportedException` をスローする。`app.Run()` を使う
- `IWindow.IsVsyncMode`: 値を設定しても反映されない
- `IWindow._RawInputContext`: `RawInputContext` に名前が変わったため、コンパイルエラーになる。ただし `RawInputContext` に改名するだけでは不十分で、v2 では通常 `null` が返り、使うと `NullReferenceException` になる。上記のとおり `InputProvider.CreateInput()` を使う
- `IWindow.TextureFactory`: 型が `TextureFactoryBase` に変わっている
