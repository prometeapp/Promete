---
title: Webブラウザ対応
description: Prometeの実験的なWebバックエンド（Promete.Web）を使って、ゲームをブラウザで動かす方法と注意点を解説します。
sidebar:
  order: 4
---

Promete.Web パッケージを使うと、Prometeで作ったゲームを、.NET の WebAssembly と WebGL2 でWebブラウザ上に動かせます。
シーンやノードのコードは、デスクトップ版とほとんど同じように書けます。

:::caution
Webバックエンドは**実験的な機能**です。デスクトップ版と挙動が異なる場合や、対応していない機能があります（[制約](#制約)）。また、今後のバージョンでAPIや挙動が予告なく変更される可能性があります。

Promete.Web パッケージは現在プレビュー版です。
:::

## 動作要件

- .NET 10 SDK
- `wasm-tools` ワークロード（後述）
- WebGL2 に対応したWebブラウザ

Promete.Web は、ビルド時にネイティブのコードを Emscripten でリンクします。そのため、`wasm-tools` ワークロードが必要です。

```sh
dotnet workload install wasm-tools
```

## プロジェクトを作る

Web版のプロジェクトは、デスクトップ版とは別のプロジェクトとして作ります。
SDKに `Microsoft.NET.Sdk.WebAssembly` を指定し、`Promete.Web` パッケージを参照します。

```xml title="MyGame.Web.csproj"
<Project Sdk="Microsoft.NET.Sdk.WebAssembly">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Promete.Web" Version="2.1.0-preview.1" />
  </ItemGroup>

  <ItemGroup>
    <!-- ゲームで使うアセット。起動前にすべて読み込まれる -->
    <PrometeAsset Include="assets/**" />
  </ItemGroup>
</Project>
```

`Promete` 本体のパッケージは、`Promete.Web` が参照しているので、別に追加する必要はありません。
HTMLやJavaScriptのファイルも、パッケージが既定のものを用意するので、書く必要はありません。

### エントリーポイント

`Main` は `async` にして、最初に `PrometeWeb.InitializeAsync()` を呼びます。
そのあと、`BuildWithOpenGLDesktop()` の代わりに `BuildWithWeb()` でアプリケーションを構築します。

```csharp title="Program.cs"
using Promete;
using Promete.Input;
using Promete.Web;
using Promete.Windowing;

public static class Program
{
    public static async Task Main()
    {
        await PrometeWeb.InitializeAsync();

        var app = PrometeApp.Create()
            .Use<Keyboard>()
            .Use<Mouse>()
            .BuildWithWeb(WindowOptions.Default with
            {
                Size = (320, 240),
                Scale = 2,
                Title = "My Game",
            });

        app.Run<MainScene>();
    }
}
```

`WindowOptions` の `Size` と `Scale` はページ上の描画領域（canvas）の大きさに、`Title` はページのタイトルに反映されます。
`Scale` で拡大するときは、ドットがぼやけないように表示されます。

:::note
ブラウザでは、`Run` はゲームの終了を待たずにすぐ戻ります。以降は、ブラウザの描画ループがゲームを進めます。
`Run` の後ろに、ゲームの終了後に実行したい処理を書いても、その時点では実行されないことに注意してください。
:::

### 実行する

```sh
dotnet run
```

ビルドが終わると、`App url: http://localhost:xxxx/` のようにURLが表示されます。このURLをブラウザで開くと、ゲームが起動します。
実行中のエラーは、ブラウザの開発者ツールのコンソールに表示されます。

ポートを固定したい場合は、`Properties/launchSettings.json` に指定します。

```json title="Properties/launchSettings.json"
{
  "profiles": {
    "MyGame.Web": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000"
    }
  }
}
```

`dotnet watch` を使うと、コードの変更を、ゲームを再起動せずに反映できます（ホットリロード）。
ただし、`OnStart` のように一度しか呼ばれないメソッドの変更は、シーンを開き直すまで反映されません。

## アセット

ブラウザのゲームは、ディスク上のファイルを直接読むことができません。
そこで Promete.Web は、`<PrometeAsset>` で指定したファイルを、ゲームの起動前にすべてダウンロードして、メモリ上のファイルシステムに置きます。

```xml
<ItemGroup>
  <PrometeAsset Include="assets/**" />
</ItemGroup>
```

これにより、デスクトップ版と同じコードでアセットを読み込めます。

```csharp
var texture = App.TextureFactory.Load("assets/player.png");
var font = Font.FromFile("assets/fonts/MyFont.ttf", 16);
```

ファイルのパスは、プロジェクトのフォルダーからの相対パスになります。
プロジェクトの外にあるファイル（デスクトップ版と共有しているアセットなど）を使う場合は、`TargetPath` でゲームから見えるパスを指定します。

```xml
<ItemGroup>
  <PrometeAsset Include="../MyGame/assets/**">
    <TargetPath>assets/%(RecursiveDir)%(Filename)%(Extension)</TargetPath>
  </PrometeAsset>
</ItemGroup>
```

:::caution
`<PrometeAsset>` で指定していないファイルは、ゲームから読み込めません。
また、ファイルへの書き込みはメモリ上で行われるため、ページを再読み込みすると消えます。
:::

## 公開する

```sh
dotnet publish -c Release -o publish
```

`publish/wwwroot` フォルダーに、公開に必要なファイル一式（HTML、JavaScript、アセンブリ、アセット）が出力されます。
このフォルダーを、静的ファイルを配信できる任意のWebサーバーに置けば、ゲームを公開できます。

Webサーバーには、次の設定が必要です。

- `.wasm` ファイルを `application/wasm` として配信する
- 圧縮済みのファイル（`.br`、`.gz`）を配信できると、ダウンロードのサイズを大きく減らせる

### AOTコンパイル

Releaseビルドでは、C# のコードを事前に WebAssembly へコンパイルする**AOTコンパイル**が既定で有効になります。
AOTを使わない場合（インタプリタ）と比べて、描画性能が大きく向上します（スプライトを大量に表示する計測で、10倍以上）。

その代わり、公開にかかる時間が数分に延び、配信するファイルのサイズも増えます。
目安として、配信サイズ（brotli圧縮後）は、インタプリタで約2.6MB、AOTで約4.2MBです。

AOTを無効にしたい場合は、プロジェクトファイルに次のように指定します。

```xml
<PropertyGroup>
  <RunAOTCompilation>false</RunAOTCompilation>
</PropertyGroup>
```

`dotnet run`（Debugビルド）は、デバッガーを使えるように、常にインタプリタで動きます。
そのため、`dotnet run` での動作は、公開したゲームより遅くなります。

:::tip
ゲームが多言語の書式（日付や数値の地域ごとの表記など）を必要としない場合は、`InvariantGlobalization` を有効にすると、配信サイズを減らせます。

```xml
<PropertyGroup>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```
:::

## ページをカスタマイズする

Promete.Web は、既定のHTML（`index.html`）とJavaScript（`main.js`）を用意しています。
プロジェクトの `wwwroot` フォルダーに同じ名前のファイルを置くと、そちらが使われます。片方だけを置くこともできます。

### HTML

自分の `index.html` を使う場合は、ゲームを描画する `<canvas>` と、`main.js` の読み込みを書きます。

```html title="wwwroot/index.html"
<!doctype html>
<html lang="ja">
<head>
    <meta charset="utf-8" />
    <title>My Game</title>
    <script type="module" src="./main.js"></script>
</head>
<body>
    <canvas id="canvas"></canvas>
</body>
</html>
```

canvas の大きさは、ゲームの `WindowOptions` から実行時に設定されるので、HTML に書く必要はありません。

canvas の `id` を変えたい場合は、`BuildWithWeb()` の `WebOptions` で、canvas を指す CSS セレクタを指定します。

```csharp
var app = PrometeApp.Create()
    .BuildWithWeb(web: new WebOptions { CanvasSelector = "#game" });
```

### JavaScript

自分の `main.js` を使う場合は、`startPromete()` を呼んでゲームを起動します。
読み込みの進み具合や、エラーを受け取ることができます。

```js title="wwwroot/main.js"
import { startPromete } from './_content/Promete.Web/promete.js';

await startPromete({
    onProgress: (loaded, total) => {
        // アセットの読み込みの進み具合
        console.log(`読み込み中... ${loaded}/${total}`);
    },
    onError: error => {
        // 起動時、または実行中に起きたエラー
        console.error(error);
    },
});
```

## ブラウザで動いているかを調べる

ブラウザで動いているかどうかは、`OperatingSystem.IsBrowser()` で調べられます。
デスクトップ版とWeb版でシーンのコードを共有していて、一部の処理だけを切り替えたい場合に使えます。

```csharp
if (!OperatingSystem.IsBrowser())
{
    // デスクトップ版だけで行う処理
    App.View.IsFullScreen = true;
}
```

## 制約

Webバックエンドでは、次の機能に対応していません。
対応していないプロパティを設定しても、エラーにはならず、無視されます。

### ゲーム画面

- ウィンドウの位置（`Location`、`X`、`Y`）、`TopMost`、`IsFullScreen`、`Mode`、`IsVisible` は、設定しても画面に反映されません
- `IsFocused` は常に `true` を返します
- ファイルのドロップ（`FileDropped`）は発生しません
- スクリーンショット（`TakeScreenshot`、`SaveScreenshotAsync`）は、`NotSupportedException` をスローします
- ブラウザのタブが非表示になっている間は、ゲームの進行が止まるか、大きく遅くなります

### 入力

- キーボードとマウスに対応しています。ゲームパッドには対応していません
- マウスカーソルの見た目の変更や、カーソルの非表示・固定には対応していません
- クリップボードには対応していません

### フォント

- `Font.FromFile` で読み込むフォントファイルは、`<PrometeAsset>` で指定しておく必要があります
- `Font.FromStream` には対応していません（`NotSupportedException` をスローします）
- 文字はブラウザの機能で描画するため、アンチエイリアスの無効化やカーニングの設定は効きません。デスクトップ版と見た目が少し異なる場合があります
- `Font.GetDefault` は、ブラウザの既定のゴシック体（`sans-serif`）を使います。実際のフォントは、閲覧する環境によって異なります

### オーディオ

- ブラウザの制限により、ユーザーがページをクリックするかキーを押すまで、音は鳴りません
- Ogg Vorbis ファイルの読み込み中は、ゲームが一時的に止まります（長い曲では数秒）

### その他

- [ImGui連携](/guide/plugins/imgui)プラグインには対応していません
- カスタムシェーダー（GLSL 3.30）は、WebGL2 向け（GLSL ES 3.00）に自動で変換されます。ただし、GLSL ES 3.00 にない機能は使えません

## デバッグ

`dotnet run` で起動したゲームは、ブラウザの開発者ツールでエラーやログを確認できます。

Visual Studio Code でブレークポイントやステップ実行を使う場合は、[Prometeのリポジトリ](https://github.com/prometeapp/Promete)の `.vscode/launch.json` と `.vscode/tasks.json`、`Promete.Web.Example/Properties/launchSettings.json` を参考にしてください。

## ノート

- Webバックエンドは実験的な機能です。不具合を見つけた場合は、デスクトップ版で同じ問題が起きるかどうかを確認してみてください
- Prometeのリポジトリにある `Promete.Web.Example` は、Web版のサンプルです。`dotnet run --project Promete.Web.Example` で動かせます
