---
title: アセット
description: 画像や設定ファイルなどのアセットを、論理パスで読み込むAssets APIについて解説します。
sidebar:
  order: 3
---

ゲームは画像や音声、設定ファイルなど、多くのファイルを使います。Assets APIを使うと、これらのファイルを**論理パス**（`assets/x.png` のような、ゲーム内でファイルを指す名前）で読めるようになります。

実際のフォルダの場所は、起動時の設定で決めます。そのため、ファイルを置く場所を変えても、ゲームのコードを書き換える必要はありません。ゲームの一部を後から差し替えたり、DLCのように追加のファイルを重ねたりすることもできます。

この機能は、次の3つの部品でできています。

- **`IAssetSource`**: 1つの読み込み元です。実フォルダや埋め込みリソースなどが、これを実装します。
- **`AssetFileSystem`**: 複数の読み込み元を優先度つきで重ねて、論理パスで読み出す仮想ファイルシステムです。
- **`AssetManager`**: `AssetFileSystem` を持つプラグインです。DIで受け取って使います。

## 最初の一歩 `v2.4~`

まずは、プロジェクトの直下に `assets` フォルダを作り、その中にファイルを置きます。そして、csproj に次の設定を足して、ビルド時に `assets` フォルダを出力先へコピーするようにします。

```xml title="MyPrometeGame.csproj"
<ItemGroup>
    <None Update="assets\**" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

この設定が無いと、実行ファイルの隣に `assets` フォルダがないため、アプリの起動時に `DirectoryNotFoundException` が発生します。

:::note
この csproj の設定は、将来の MSBuild 統合 (`PrometeAsset`) で置き換わる予定です。それまでは、上の設定を足してください。
:::

次に、アプリの作成時に `UseAssets` を呼び、`assets` フォルダをマウントします。

```csharp title="Program.cs"
using Promete;
using Promete.Assets;

var app = PrometeApp.Create()
    .UseAssets(o => o.MountFolder("assets", "assets"))
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

`MountFolder("assets", "assets")` は、「`assets` フォルダの中身を、論理パスの `assets/` の下に見せる」という意味です。たとえば、`assets/x.png` というファイルがあれば、論理パス `"assets/x.png"` で読めます。

:::note
`MountFolder` の1つ目の引数は実際のフォルダ、2つ目の引数は論理パスの接頭辞です。両方を `"assets"` にすると、フォルダの場所と論理パスが揃うので分かりやすくなります。
:::

次に、シーンで `AssetManager` を受け取り、ファイルを読みます。`AssetManager` は、コンストラクタ注入で受け取れます。

```csharp title="MainScene.cs"
using Promete;
using Promete.Assets;
using Promete.Graphics;
using Promete.Nodes;

public class MainScene(AssetManager assets) : Scene
{
    private Texture2D? _texture;

    public override void OnStart()
    {
        // ファイルがあるかどうかを確認する
        if (!assets.FileSystem.Exists("assets/x.png"))
        {
            return;
        }

        // ストリームとして開き、テクスチャを作る
        using var stream = assets.FileSystem.OpenRead("assets/x.png");
        _texture = App.TextureFactory.Load(stream);

        Root.Add(new Sprite(_texture).Location(100, 100));
    }

    public override void OnDestroy()
    {
        // 読み込んだテクスチャは、自分で Dispose する
        _texture?.Dispose();
    }
}
```

`OpenRead` が返すストリームは、常に `Seek` できます。そのため、`TextureFactory.Load(Stream)` などの、既存の `Stream` を受け取るAPIにそのまま渡せます。

`using` で囲んでストリームを閉じるのを忘れないでください。ストリームは使い終わったら閉じます。

テクスチャは `IDisposable` です。`Load` で作ったテクスチャは、シーンを離れるとき (`OnDestroy`) に `Dispose` してください。

## ファイルを探す `v2.4~`

`Exists` でファイルがあるかを調べ、`Enumerate` でフォルダの中のファイル一覧を取得できます。

```csharp title="FindAssets.cs"
// assets/images の中の png ファイルを、サブフォルダも含めて列挙する
foreach (var path in assets.FileSystem.Enumerate("assets/images", "*.png", recursive: true))
{
    Console.WriteLine(path);
}
```

`Enumerate` の引数は次のとおりです。

- `directory`: 列挙するフォルダの論理パス。空文字列か `/` でルートを表します。
- `pattern`: ファイル名のパターンです。`*` は0文字以上、`?` はちょうど1文字に一致します。
- `recursive`: `true` なら、サブフォルダの中も探します。

返ってくるのは、フォルダを含まないファイルの論理パスです。複数の読み込み元をマウントしている場合は、すべてをまとめた一覧になります（重複は除かれ、並び順は文字コード順です）。

## 相対パスの基準 `v2.4~`

`MountFolder` に相対パスを渡すと、その基準は**実行ファイルのフォルダ**（`AppContext.BaseDirectory`）になります。カレントディレクトリ（`Directory.GetCurrentDirectory()`）ではありません。

そのため、Visual Studio や `dotnet run` からの起動と、ダブルクリックでの起動でも、同じフォルダを見ます。

指定したフォルダが存在しないと、アプリの起動時（プラグインの `OnStart` より前）に `DirectoryNotFoundException` が発生します。ファイルを読んでから気づくのではなく、起動時に早く気づけるようになっています。

フォルダの物理パス（`MountFolder` の1つ目の引数）は、OS のファイルシステムの規則に従います。そのため、Windows や macOS では、大文字・小文字を区別せずに見つかります。大文字・小文字を厳密に区別するのは、論理パスの側だけです（次の「パスの規則」を参照）。

## パスの規則 `v2.4~`

論理パスには、次の規則があります。

- **区切りは `/`** です。`\` を含むパスは、`ArgumentException` になります。

  ```csharp
  assets.FileSystem.Exists("assets\\x.png"); // ArgumentException
  ```

- **先頭の `/` と `./` は無視されます。** `"/assets/x.png"` と `"assets/x.png"` は同じファイルです。
- **`..` は1つ上のフォルダへ移動します。** ただし、ルートより上には出られません。`"../../secret.txt"` は、ルートの `secret.txt` として扱われます。読み込み元の外にあるファイルは読めません。
- **大文字・小文字を区別します。** Windows や macOS でも区別します。`"assets/X.png"` は、`assets/x.png` があっても見つかりません。（論理パスの話です。フォルダの物理パスは、OS の規則に従います。前の「相対パスの基準」を参照してください。）

  大文字・小文字だけが違う場合は、`AssetPathCaseMismatchException` が発生し、正しい名前を教えてくれます。

  ```text
  'assets/X.png' が見つかりません。'assets/x.png' はあります。アセットのパスは大文字・小文字を区別します。
  ```

  `Exists` は、大文字・小文字だけが違う場合に `false` を返します。例外を使いたくないときは、`Exists` で確認してから読むと安全です。

- **Unicode は NFC に正規化されます。** 濁点や半濁点が別の文字として扱われる問題を防ぐための規則です。

:::caution
`InvariantGlobalization=true` の環境では、NFC への正規化は行われません。その場合は、日本語などのファイル名を**最初から NFC で**書いてください。
:::

## 優先度と上書き `v2.4~`

同じ論理パスのファイルが複数の読み込み元にあるときは、**優先度が高いものが勝ちます**。優先度は `priority` 引数で指定します。大きいほど優先されます。

```csharp title="Program.cs"
var app = PrometeApp.Create()
    .UseAssets(o => o
        .MountFolder("assets", "assets")                  // 基本のアセット (priority = 0)
        .MountFolder("dlc/assets", "assets", priority: 10) // DLC: 同じ論理パスを上書きする
    )
    .BuildWithOpenGLDesktop();
```

この例では、`dlc/assets/x.png` があれば、`"assets/x.png"` の読み込みは DLC 側が使われます。DLC にないファイルは、基本のアセットから読まれます。

優先度が同じ場合は、**後に書いたものが優先されます**。

```csharp
o.MountFolder("mod_a", "assets")  // 先に書く
 .MountFolder("mod_b", "assets")  // 後に書く → "assets/x.png" は mod_b が勝つ
```

:::tip
`Enumerate` は、すべての読み込み元を合わせた結果を返します。上書きされたファイルも、重複を除いて1回だけ一覧に現れます。
:::

## 埋め込みリソースを読む `v2.4~`

ファイルを実行ファイルに埋め込んで配布したいときは、`MountEmbeddedResources` を使います。

まず、csproj でファイルを埋め込み、`LogicalName` で論理パスを付けます。

```xml title="Game.csproj"
<ItemGroup>
  <EmbeddedResource Include="assets\x.png" LogicalName="assets/x.png" />
</ItemGroup>
```

`Include` はファイルの場所なので、`\` のままで構いません。一方、`LogicalName` の区切りは必ず `/` にしてください。

:::caution
`LogicalName` に `%(RelativeDir)` や `%(RecursiveDir)` をそのまま使うと、Windows では区切りが `\` になります。接頭辞に一致する名前に `\` が含まれていると、`MountEmbeddedResources` を使ったときに `ArgumentException` が発生します。接頭辞の直後が `\` の名前（たとえば `assets\x.png`）も、接頭辞に一致するので同じく `ArgumentException` になります。`LogicalName` は、`/` で書いた名前を指定してください。
:::

次に、アプリの作成時にマウントします。

```csharp title="Program.cs"
using System.Reflection;

var app = PrometeApp.Create()
    .UseAssets(o => o.MountEmbeddedResources(
        Assembly.GetExecutingAssembly(),
        resourcePrefix: "assets/",
        mountPoint: "assets"
    ))
    .BuildWithOpenGLDesktop();
```

`resourcePrefix` で始まるリソース名だけを対象にし、その接頭辞を取り除いた残りを、`mountPoint` の下に置きます。上の例では、リソース `assets/x.png` が論理パス `assets/x.png` になります。

:::tip
`resourcePrefix` は指定することをおすすめします。空のままにすると、アセンブリ内のすべてのリソースが対象になります。そのため、無関係なリソースの名前に `\` が1つでも含まれていると、そのリソースも対象になって `ArgumentException` が発生し、マウント全体が失敗します。
:::

接頭辞は、フォルダとして扱います。`"assets"` と `"assets/"` は同じ意味で、`"assets"` は `assets2/x.png` には一致しません。

:::note
同じ論理パスになる名前が複数あると、ソースを作る時点（アプリの開始時）で `ArgumentException` になります。たとえば `assets/x.png` と `assets/./x.png` は、正規化すると同じ `assets/x.png` になるので、どちらか一方だけを埋め込んでください。`resourcePrefix` や `mountPoint` に `\` を書いた場合は、`UseAssets` の中で `ArgumentException` になります。
:::

埋め込みリソースも、フォルダと同じように `OpenRead` や `Exists` で読めます。埋め込みリソースは読み取り専用です。

## 自前の読み込み元を作る `v2.4~`

フォルダや埋め込みリソース以外から読みたいときは、`IAssetSource` を実装して `Mount` します。たとえば、暗号化されたアーカイブから読むような場合です。

```csharp title="MyArchiveSource.cs"
using System;
using System.Collections.Generic;
using System.IO;
using Promete.Assets;

public class MyArchiveSource : IAssetSource
{
    public bool Exists(string path)
    {
        // 論理パスのファイルがあるかを返す
        throw new NotImplementedException();
    }

    public Stream OpenRead(string path)
    {
        // 常に Seek できるストリームを返す
        throw new NotImplementedException();
    }

    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        // directory の下にあるファイルの論理パスを返す
        throw new NotImplementedException();
    }
}
```

作ったソースは、オプションの `Mount` で登録します。

```csharp
o.Mount(new MyArchiveSource(), priority: 5);
```

`IDisposable` を実装したソースを `Mount` すると、そのソースの所有権は `AssetManager` に移ります。アプリが終了するときに、`Dispose` が呼ばれます。

## UseAssets の書き方 `v2.4~`

`UseAssets` には、2つの書き方があります。

```csharp
// 1. 引数なし: エントリアセンブリを使う
PrometeApp.Create().UseAssets(o => o.MountFolder("assets", "assets"));

// 2. アセンブリを指定する
PrometeApp.Create().UseAssets(typeof(Program).Assembly, o => o.MountFolder("assets", "assets"));
```

引数なしの `UseAssets()` は、エントリアセンブリ（実行しているアプリ本体）を見ます。ただし、次の環境ではエントリアセンブリを取得できず、`InvalidOperationException` が発生します。

- **Web（WebAssembly）**: エントリアセンブリが取れません。`UseAssets(typeof(Program).Assembly, ...)` のように、アセンブリを指定してください。

テストでは、`UseAssets(typeof(X).Assembly)` か、明示的なマウントを使ってください。テストホストのエントリアセンブリは、ゲームのアセンブリではないためです。

:::note
`UseAssets` は、アプリごとに1回だけ呼べます。2回呼ぶと `InvalidOperationException` が発生します。
:::

## マウントのタイミング `v2.4~`

`UseAssets` で指定したマウントは、`AssetManager` が作られるとき（アプリの開始時）に、指定した順に行われます。プラグインの `OnStart` より前なので、どのプラグインの `OnStart` からでも、マウント済みのアセットを読めます。

:::note
`MountFolder` で指定したフォルダが存在しない場合は、この時点で `DirectoryNotFoundException` が発生します。
:::

## まとめ

- `UseAssets` で、`MountFolder` や `MountEmbeddedResources` を指定する。
- シーンやプラグインでは、`AssetManager` をコンストラクタで受け取り、`assets.FileSystem` を使う。
- 読むときは `OpenRead`、確認するときは `Exists`、探すときは `Enumerate` を使う。
- 相対パスの基準は実行ファイルのフォルダで、パスは大文字・小文字を区別する。
- 同じパスは、優先度が高いもの（同じなら後に書いたもの）が勝つ。
