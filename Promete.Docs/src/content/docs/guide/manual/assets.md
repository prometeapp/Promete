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

まずは、プロジェクトの直下に `assets` フォルダを作り、その中にファイルを置きます。次に、csproj でそのファイルをアセットとして宣言します。

```xml title="MyPrometeGame.csproj"
<ItemGroup>
    <PrometeAsset Include="assets/**" />
</ItemGroup>
```

`PrometeAsset` で宣言したファイルは、ビルドのときに MSBuild が検査して、ゲームから読める形で出力します。`Include` には、読みたいファイルを `*` や `**` を使ったパターンで書きます。

次に、アプリの作成時に、引数なしの `UseAssets` を呼びます。

```csharp title="Program.cs"
using Promete;
using Promete.Assets;

var app = PrometeApp.Create()
    .UseAssets()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

`UseAssets()` は、宣言されたアセットを読めるようにします。ファイルの場所を、コードに書く必要はありません。

:::note
`PrometeAsset` は、ビルドの種類によって次のどちらかの形で出力されます。どちらでも、ゲームのコードは同じです。

- **Folder** (既定は Debug): 元のファイルをそのまま読みます。ファイルの中身を変えたときは、次に起動すると反映されます。
- **Package** (既定は Debug 以外。Release など): ファイルを1つのパッケージにまとめます。配布に向いています。

詳しくは、次の「csproj でアセットを宣言する」を参照してください。
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

## csproj でアセットを宣言する `v2.4~`

`PrometeAsset` は、csproj に書くアセットの宣言です。宣言したファイルは、ビルドのときに検査され、Folder か Package のどちらかの形で出力されます。

### Folder と Package

| モード | 既定の構成 | 出力されるもの | 向いている場面 |
| --- | --- | --- | --- |
| `Folder` | Debug | マニフェスト (`<AssemblyName>.promete-assets.json`)。ファイルはコピーしません | 開発中 |
| `Package` | Debug 以外 (Release など) | パッケージ (`<AssemblyName>.<Package>.prmtpak`)。読み取り専用の1つのファイル | 配布 |

Folder モードでは、マニフェストが元のファイルの場所を指すので、ファイルの中身を変えたら次の起動で反映されます。宣言するファイルを増やしたり減らしたりしたときは、ビルドし直してください。

モードは csproj か、コマンドラインで指定できます。

```xml title="MyPrometeGame.csproj"
<PropertyGroup>
    <PrometeAssetMode>Package</PrometeAssetMode>
</PropertyGroup>
```

```sh
dotnet publish -c Release -p:PrometeAssetMode=Package
```

:::caution
宣言していないファイルは、Folder モードでも読めません。`Exists` は `false` を返し、`OpenRead` は `FileNotFoundException` を発生させます。
:::

### 論理パス (TargetPath)

`Include` のパスは、プロジェクトのフォルダからの相対パスになり、そのまま論理パスになります。たとえば `assets/x.png` を宣言すると、論理パスは `assets/x.png` です。

プロジェクトの外にあるファイルは、`TargetPath` で論理パスを指定します。

```xml title="MyPrometeGame.csproj"
<ItemGroup>
    <PrometeAsset Include="..\Shared\assets\**">
        <TargetPath>assets/%(RecursiveDir)%(Filename)%(Extension)</TargetPath>
    </PrometeAsset>
</ItemGroup>
```

`TargetPath` には `..` を使えません。区切りは `/` で書きます (`\` で書いても、ビルドが `/` に直します)。

### パッケージを分ける

既定では、すべてのアセットは `main` という1つのパッケージに入ります。`Package` メタデータで分けられます。

```xml title="MyPrometeGame.csproj"
<ItemGroup>
    <PrometeAsset Include="assets/**" Exclude="assets/bgm/**" />
    <PrometeAsset Include="assets/bgm/**">
        <Package>bgm</Package>
    </PrometeAsset>
</ItemGroup>
```

- パッケージの名前は、英数字 (ASCII)、`_`、`-` だけが使えます。
- 同じ論理パスのファイルを複数のパッケージに入れると、ビルドが失敗します (PROMETE1003)。`Exclude` で重ならないようにしてください。
- `Package` は Package モードのときだけ効きます。Folder モードでは無視されます。
- 読むときは、すべてのパッケージが自動でマウントされます。

### 圧縮 (Compress)

```xml title="MyPrometeGame.csproj"
<PrometeAsset Include="assets/**">
    <Compress>true</Compress>
</PrometeAsset>
```

- `auto` (既定): 拡張子で判断します。画像や音声など、すでに圧縮された形式は圧縮しません。それ以外は圧縮を試し、小さくならなければ圧縮しません。
- `true`: 常に圧縮します。
- `false`: 圧縮しません。

`Compress` は Package モードのときだけ効きます。大文字・小文字は区別せず、前後の空白は無視されます。

### 埋め込み (PrometeAssetEmbed)

```xml title="MyPrometeGame.csproj"
<PropertyGroup>
    <PrometeAssetEmbed>true</PrometeAssetEmbed>
</PropertyGroup>
```

`true` にすると、パッケージを実行ファイル (アセンブリ) の中に埋め込みます。出力先にパッケージのファイルが残らないので、配るファイルが1つで済みます。Package モードのときだけ効き、すべてのパッケージに同じように効きます。読むときのコードは変わりません。

### 出力されるファイルと publish

- **Folder**: 出力先に `<AssemblyName>.promete-assets.json` が置かれます。中身は元のファイルの絶対パスなので、publish には含まれません。
- **Package**: 出力先に `<AssemblyName>.<Package>.prmtpak` が置かれ、publish にも含まれます。
- **埋め込み**: 出力先にはパッケージのファイルが無く、アセンブリの中に入ります。

配布するときは、Package モードで publish してください。Folder モードのまま publish すると、警告 (PROMETE1011) が出ます。

:::note
Package モードのアプリは、NativeAOT で publish しても、パッケージから読めます (Windows で確認済み)。`PublishAot` は csproj に書いてください。
:::

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
        .MountFolder("dlc/assets", "assets", priority: 10) // DLC: 同じ論理パスを上書きする
    )
    .BuildWithOpenGLDesktop();
```

この例では、`dlc/assets/x.png` があれば、`"assets/x.png"` の読み込みは DLC 側が使われます。DLC にないファイルは、基本のアセットから読まれます。

`PrometeAsset` で宣言したアセットは、どの明示的なマウントよりも低い優先度です。そのため、`UseAssets` で指定したフォルダやパッケージは、宣言したアセットの上に重なります。

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

## パッケージから読む `v2.4~`

複数のファイルを1つにまとめたものを、**パッケージ**と呼びます。パッケージも、フォルダと同じように論理パスで読めます。ファイルの数が多いときや、配布するファイルを1つにまとめたいときに便利です。

ファイルのパッケージは、`MountPackage` でマウントします。

```csharp title="Program.cs"
var app = PrometeApp.Create()
    .UseAssets(o => o.MountPackage("game.main.pak", "assets"))
    .BuildWithOpenGLDesktop();
```

`"game.main.pak"` は、実行ファイルのフォルダにあるパッケージです。相対パスの基準は、フォルダの場合と同じく実行ファイルのフォルダ（`AppContext.BaseDirectory`）になります。

パッケージを実行ファイルに埋め込んだ場合は、`MountEmbeddedPackage` を使います。先に csproj で、パッケージを埋め込みます。

```xml title="Game.csproj"
<ItemGroup>
  <EmbeddedResource Include="Packages\game.main.pak" LogicalName="game.main.pak" />
</ItemGroup>
```

```csharp title="Program.cs"
var app = PrometeApp.Create()
    .UseAssets(o => o.MountEmbeddedPackage(typeof(Program).Assembly, "game.main.pak", "assets"))
    .BuildWithOpenGLDesktop();
```

`MountEmbeddedPackage` の2つ目の引数は、埋め込みリソースの名前（`LogicalName`）です。3つ目の引数は、論理パスの接頭辞です。

マウントした後は、フォルダや埋め込みリソースと同じように読めます。

```csharp
using var stream = assets.FileSystem.OpenRead("assets/x.png");
```

パッケージには、次の性質があります。

- **必要な部分だけを読みます。** 開くときに読むのは、パッケージの目次だけです。ファイルを読むときも、そのファイルの部分だけを読むので、パッケージ全体をメモリに読み込むことはありません。圧縮されたファイルだけは、そのファイル1つ分を展開してメモリに置きます。
- **複数のファイルを同時に読めます。** 別々のファイルを、別々のスレッドから同時に読めます。
- **読み取り専用です。** パッケージの中身は書き換えられません。
- **アプリがパッケージを閉じても、開いたストリームは使えます。** 開いたストリームは、自分で閉じるまで読み続けられます。

パッケージも、ほかの読み込み元と同じように `priority` で重ねられます。たとえば、パッケージの上にフォルダを重ねると、一部のファイルだけを差し替えられます。

```csharp title="Program.cs"
var app = PrometeApp.Create()
    .UseAssets(o => o
        .MountPackage("game.main.pak", "assets")                 // 基本のアセット (priority = 0)
        .MountFolder("patch/assets", "assets", priority: 10)     // パッチ: 同じ論理パスを上書きする
    )
    .BuildWithOpenGLDesktop();
```

:::caution
パッケージのファイルが無い、または壊れている場合は、`UseAssets` の呼び出しではなく、アプリを開始したとき（マウントするとき）に例外が発生します。例外は、次のとおりです。

- ファイルが無い場合: `FileNotFoundException`
- ファイルが壊れている場合: `InvalidDataException`
- 新しい形式のパッケージ（メジャーバージョンが対応していないもの）の場合: `NotSupportedException`

例外のメッセージには、ファイルのパスや埋め込みリソースの名前が入るので、どのパッケージで問題が起きたかが分かります。
:::

:::tip
パッケージは、手で作る必要はありません。csproj で `PrometeAsset` を宣言すると、ビルドがパッケージを作り、`UseAssets()` で自動的に読み込まれます。詳しくは、前の「csproj でアセットを宣言する」を参照してください。
:::

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
// 1. 引数なし: エントリアセンブリの PrometeAsset を読む
PrometeApp.Create().UseAssets();

// 2. アセンブリを指定する (Web など、エントリアセンブリを取れない環境)
PrometeApp.Create().UseAssets(typeof(Program).Assembly);

// 3. 明示的なマウントを重ねる
PrometeApp.Create().UseAssets(o => o.MountFolder("dlc/assets", "assets", priority: 10));
```

`UseAssets` は、アセンブリに付いた `PrometeAsset` の情報を読んで、アセットをマウントします。宣言が無ければ、何もマウントしません。明示的なマウント (`MountFolder` など) は、宣言したアセットの上に重なります。

引数なしの `UseAssets()` は、エントリアセンブリ（実行しているアプリ本体）を見ます。ただし、次の環境ではエントリアセンブリを取得できず、`InvalidOperationException` が発生します。

- **Web（WebAssembly）**: エントリアセンブリが取れません。`UseAssets(typeof(Program).Assembly)` のように、アセンブリを指定してください。

テストでは、ゲームの実行プロジェクトを参照して `UseAssets(typeof(GameScene).Assembly)` を使ってください。テストホストのエントリアセンブリは、ゲームのアセンブリではないためです。

:::note
`UseAssets` は、アプリごとに1回だけ呼べます。2回呼ぶと `InvalidOperationException` が発生します。
:::

## マウントのタイミング `v2.4~`

`UseAssets` で指定したマウントは、`AssetManager` が作られるとき（アプリの開始時）に、指定した順に行われます。`PrometeAsset` によるマウントも、このときに行われ、明示的なマウントより先に行われます。プラグインの `OnStart` より前なので、どのプラグインの `OnStart` からでも、マウント済みのアセットを読めます。

:::note
`MountFolder` で指定したフォルダが存在しない場合は、この時点で `DirectoryNotFoundException` が発生します。
:::

## 画像・フォント・音声を読む `v2.4~`

ここまでは、`OpenRead` でストリームを開いて、自分でテクスチャなどを作る方法でした。`AssetManager` には、画像・フォント・音声をそのまま読む方法もあります。

次の例では、シーンのフィールドで読み込んでいます。

```csharp title="MainScene.cs"
using System.Drawing;
using Promete.Assets;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Nodes;

public class MainScene(AssetManager assets) : Scene
{
    // シーンのフィールドで読む。このシーンが使われている間は、読み込んだものが残る
    private readonly Texture2D _ichigo = assets.LoadTexture("assets/ichigo.png", AssetScope.Scene);
    private readonly Font _font = assets.LoadFont("assets/Koruri.ttf", 24, AssetScope.Global);

    public override void OnStart()
    {
        Root.Add(new Sprite(_ichigo).Location(100, 100));
        Root.Add(new Text("こんにちは", _font, Color.White).Location(100, 200));
    }
}
```

読み込みのメソッドは、次のとおりです。どれも最後の引数に `AssetScope` を指定します。

| メソッド | 読むもの | 返す値 |
| --- | --- | --- |
| `LoadTexture(path, scope)` | 画像 | `Texture2D` |
| `LoadSpriteSheet(path, 横の数, 縦の数, サイズ, scope)` | 格子状に並んだ画像 | `Texture2D[]` |
| `LoadSpriteSheet(path, scope, rects)` | 指定した矩形で切り抜いた画像 | `Texture2D[]` |
| `Load9Sliced(path, left, top, right, bottom, scope)` | 9 スライス用の画像 | `Texture9Sliced` |
| `LoadFont(path, size, scope)` | フォント | `Font` |
| `LoadAudio(path, scope)` | 音声 | `IAudioSource` |

たとえば、32×32 のセルが横に4つ、縦に2つ並んだ画像は、次のように切り分けられます。

```csharp
var frames = assets.LoadSpriteSheet("assets/chara.png", 4, 2, new VectorInt(32, 32), AssetScope.Scene);
```

音声は、`AudioPlayer` で鳴らします。

```csharp
private readonly IAudioSource _sfx = assets.LoadAudio("assets/lineclear.wav", AssetScope.Scene);
private readonly AudioPlayer _player = new();

public override void OnUpdate()
{
    _player.PlayOneShot(_sfx);
}
```

テクスチャのサンプリングの設定 (`TextureOptions`) を変えたいときは、`LoadTexture(path, options, scope)` のように、オプションを追加した書き方を使います。省略した場合は、既定の設定が使われます。

:::note
フォントは、サイズが違っても、同じファイルのデータを 1 つだけ読みます。たとえば、同じ `Koruri.ttf` を 16 と 32 の 2 つのサイズで読んでも、ファイルは 1 回しか読まれません。
:::

:::note
Web (WebAssembly) では、フォントをストリームから読めません。そのため、フォルダにあるフォントは、ファイルのパスから読みます。この場合、フォントは実行環境の側でキャッシュされるため、`AssetScope.Scene` で読んでもシーンが終わっても解放されません（実質的に `Global` と同じです）。`MountEmbeddedResources` で埋め込んだフォントは Web では読めず、`NotSupportedException` が発生します。`PrometeAssetEmbed` で埋め込んだパッケージのフォントは、起動前に読み込まれるので、Web でも読めます。
:::

## 返されたリソースを Dispose しない `v2.4~`

:::caution
`AssetManager` が返したリソースは、**自分で `Dispose` しないでください**。解放は Assets API が、スコープの規則に従って行います。
:::

理由は、`Texture2D.Dispose` が2回呼ばれることを想定していないためです。利用者とスコープの両方が `Dispose` すると、同じテクスチャを別の場所で使っているものまで壊れることがあります。

解放したいときは、`Unload` を使います（「明示的に解放する」の節を参照してください）。

:::note
前の節の `App.TextureFactory.Load(stream)` で自分で作ったテクスチャは、これまでどおり自分で `Dispose` してください。`AssetManager` の読み込みと混ぜないようにしましょう。
:::

## スコープ `v2.4~`

読み込んだアセットは、**スコープ** (`AssetScope`) に所有されます。スコープは、アセットをいつ解放するかを決めます。

| スコープ | 所有者 | 解放されるとき |
| --- | --- | --- |
| `AssetScope.Global` | アプリ全体 | アプリの終了時、または `Unload` で明示的に解放したとき |
| `AssetScope.Scene` | 読んだシーン | そのシーンが使われなくなったとき |

`AssetScope.Scene` の場合、解放のタイミングは次のとおりです。

- **`LoadScene` で別のシーンに移るとき**: 新しいシーンの `OnStart` が終わった後に、前のシーンのものを解放します。新しいシーンが同じものを読んでいれば、解放されずに、そのまま使われます（読み直しはしません）。
- **`PopScene` で前のシーンに戻るとき**: 上にあったシーンのものだけを解放します。
- **`PushScene` で別のシーンを重ねるとき**: 何も解放しません。下にあるシーンのものは、残ります。
- **アプリが終わるとき**: 各シーンの `OnDestroy` の後に解放します。

`OnDestroy` の中では、まだ読み込んだものを使えます。

### どのシーンのものになるか

シーンのコンストラクタ（フィールドの初期化を含みます）で読んだものは、そのシーンのものになります。`OnStart` や `OnUpdate` の中で読んだものは、その時点で動いている、今のシーンのものになります。

```csharp
public class MainScene(AssetManager assets) : Scene
{
    // コンストラクタで読んだので、この MainScene のもの
    private readonly Texture2D _ichigo = assets.LoadTexture("assets/ichigo.png", AssetScope.Scene);

    public override void OnStart()
    {
        // OnStart で読んだので、そのときの今のシーン (この MainScene) のもの
        var icon = assets.LoadTexture("assets/icon.png", AssetScope.Scene);
    }
}
```

:::caution
DI が初めてシーンを解決する途中で作られるプラグイン（シングルトン）のコンストラクタで `AssetScope.Scene` を指定すると、そのものは**そのとき生成中のシーン**のものになります。そのプラグインが後のシーンでも使われるなら、そのシーンが終わったときに解放されてしまいます。プラグインのコンストラクタで読むものは、`AssetScope.Global` を使ってください。
:::

### Scene を指定できない場面

次の場面では、`AssetScope.Scene` を指定すると `InvalidOperationException` が発生します。

- シーンが1つもないとき（アプリの組み立て中や、プラグインの `OnStart` の中など）
- メインスレッド以外（ワーカースレッド）から呼ぶとき

これらの場面では、`AssetScope.Global` を使ってください。

ただし、テクスチャをワーカースレッドで読むときは、同期の `LoadTexture` ではなく `LoadTextureAsync` などの `...Async` を使ってください。GL では、テクスチャの転送をメインスレッドでしか行えないためです。汎用の `Load`、音声、フォントは、ワーカースレッドから `Global` で読めます。

## 同じアセットの扱い `v2.4~`

読み込んだアセットは、**種類・論理パス・オプション**の組をキーにしてキャッシュされます。同じキーで読むと、ファイルを読み直さず、同じものを返します。

- `./assets/ichigo.png` と `assets/ichigo.png` は同じ論理パスなので、同じものになります。先頭の `/` も無視されます。
- 大文字・小文字は区別されます。`assets/Ichigo.png` は別のファイルとして扱われます。
- `TextureOptions` が違えば、同じファイルでも別のテクスチャになります。
- `LoadTexture` と `LoadSpriteSheet` で同じファイルを読んだときも、別のものになります。
- フォントは、サイズや `FontStyle` が違っても、データは共有されます。
- `LoadSpriteSheet` は、呼ぶたびに新しい配列を返します。配列の中身（テクスチャ）は共有されるので、配列の要素を並べ替えても、キャッシュには影響しません。

## 非同期で読む `v2.4~`

大きなファイルを読むと、その間ゲームが止まることがあります。そこで、`...Async` という名前のメソッドを使うと、読み込みをスレッドプールで行い、`Task` を返します。

`Task` の結果は、コルーチンの `WaitForTask` で待つか、`await` で受け取ります。次の例は、コルーチンで待つ方法です。

```csharp title="MainScene.cs"
using System.Collections;
using Promete.Coroutines;
using Promete.Assets;
using Promete.Graphics;
using Promete.Nodes;

public class MainScene(AssetManager assets, CoroutineManager coroutine) : Scene
{
    public override void OnStart()
    {
        coroutine.Start(LoadIconAsync());
    }

    private IEnumerator LoadIconAsync()
    {
        var task = assets.LoadTextureAsync("assets/icons.png", AssetScope.Scene);
        yield return new WaitForTask(task);

        // 読み込みが失敗した (途中で所有が外れた) 場合は、Result を読むと例外になるので、ここで止める
        if (!task.IsCompletedSuccessfully)
            yield break;

        Root.Add(new Sprite(task.Result).Location(256, 96));
    }
}
```

非同期版には、次の性質があります。

- **同じものを同時に読むと、1 回にまとめられます。** 読み込みの途中で同じ `LoadTextureAsync` を呼ぶと、同じ読み込みの結果を共有します。同期版の `LoadTexture` と同じキャッシュを使うので、先に同期版で読んであれば、非同期版はそれを返します。
- **引数やスコープの間違いは、呼んだ瞬間に例外になります。** 読み込みの途中で起きたエラー（ファイルが無いなど）は、`Task` の中の例外になります。
- **`CancellationToken` は、待つのをやめるためだけに使います。** キャンセルしても、読み込み自体は止まりません。読み終わった結果は、同じスコープに残ります。すでにキャンセルされたトークンを渡すと、読み込みは始めずに、キャンセルされた `Task` を返します。
- **読み終わる前にシーンが終わった場合、結果は読み終わった時点で解放され、`Task` は `OperationCanceledException` で失敗します。** (アプリが終わる場合は `ObjectDisposedException` です。) 同じ読み込みを別のスコープも待っていれば、失敗せず、結果を返します。

シーンが終わると、そのシーンで動かしているコルーチンも止まるので、上の例の `Root.Add` は実行されません。ただし、読み込みの途中で所有が外れて `Task` が失敗することもあります。`Task` は失敗しても完了するので、`WaitForTask` の後は `task.Result` を読む前に `task.IsCompletedSuccessfully` を確かめてください。

## 明示的に解放する `v2.4~`

スコープの規則を待たずに、解放したいときは `Unload` を使います。

```csharp
// 同じパスのアセットを、Global の所有から外す (ほかのスコープが持っていれば残る)
assets.Unload("assets/bg.png", AssetScope.Global);

// 同じパスのアセットを、所有しているスコープに関係なく解放する
assets.Unload("assets/bg.png");

// 指定したスコープの所有を、すべてのアセットから外す
assets.Unload(AssetScope.Global);
```

- `Unload(path, scope)` は、そのスコープの所有だけを外します。ほかのスコープがまだ持っていれば、残ります。
- `Unload(path)` は、同じパスのものを、所有者に関係なく解放します。
- `Unload(scope)` は、指定したスコープの所有を、すべてのアセットから外します。スコープ自体は残るので、この後も同じスコープで読めます。
- 解放の途中で例外が出ても、残りのアセットは解放されます。例外はすべて `AggregateException` にまとめて投げられます。

:::caution
解放したテクスチャを使っている `Sprite` は、表示が壊れます。解放する前に、その `Sprite` を `Root` から外すなどして、使わないようにしてください。
:::

## 自前の形式を読む `v2.4~`

画像や音声以外の形式（設定ファイルなど）は、`Load<T>` で読めます。ストリームから `T` を作る処理を渡します。

```csharp
using System.Text.Json;

// キーは「どう読んだか」を表す名前。同じパスでも、キーが違えば別のものになる
var settings = assets.Load(
    "assets/settings.json",
    "settings",
    stream => JsonSerializer.Deserialize<GameSettings>(stream)!,
    AssetScope.Global
);
```

- `key` は、キャッシュのキーの一部です。同じパスを別の方法で読むときは、別のキーを付けてください。大文字・小文字は区別されます。
- 処理に渡されたストリームは、処理が戻ったあとに閉じられます。
- 結果が `IDisposable` を実装していれば、スコープが解放されるときに `Dispose` されます。

ストリームを閉じずに持ち出したいときは、`leaveOpen: true` を指定します。このとき、ストリームを閉じる責任は結果に移ります。

```csharp
// BinaryReader が Dispose されるとき、元のストリームも閉じられる
var reader = assets.Load(
    "assets/data.bin",
    "reader",
    stream => new BinaryReader(stream),
    AssetScope.Scene,
    leaveOpen: true
);
```

非同期版の `LoadAsync` も同じ書き方です。処理は `Func<Stream, Task<T>>` を渡します。

## 音声の形式を増やす `v2.4~`

wav と ogg 以外の音声形式を読めるようにするには、`IAudioDecoder` を実装して登録します。

```csharp title="MyAudioDecoder.cs"
using System;
using System.IO;
using Promete.Audio;

public class MyAudioDecoder : IAudioDecoder
{
    public bool CanDecode(string path, ReadOnlySpan<byte> header)
    {
        // 拡張子ではなく、先頭のマジックで判定する
        return header.Length >= 4 && header[..4].SequenceEqual("MYAU"u8);
    }

    public IAudioSource Decode(Stream stream)
    {
        // stream はデコーダーのものになる。閉じるのはこのデコーダーの責任
        return new MyAudioSource(stream);
    }
}
```

`MyAudioSource` は、自分で実装した `IAudioSource` です。登録は、アプリを作るときに行います。

```csharp title="Program.cs"
var app = PrometeApp.Create()
    .UseAssets(o => o.MountFolder("assets", "assets"))
    .Use<IAudioDecoder, MyAudioDecoder>()
    .BuildWithOpenGLDesktop();
```

- 登録したデコーダーは、標準のデコーダー（wav / ogg）より**先に**、登録した順に判定されます。
- `header` は、ファイルの先頭の最大 64 バイトです。短いファイルでは、それより短くなります。
- どのデコーダーも判定できなければ、`NotSupportedException` が発生します。
- 結果の `IAudioSource` は、キャッシュされて複数の呼び出し元で共有されます。そのため、再生位置などを呼び出し元ごとに持たないでください。`FillSamples` は、複数のスレッドから同時に呼ばれても安全である必要があります。

## 注意点 `v2.4~`

- **ライブラリとテストプロジェクトの `PrometeAsset` は無視されます** (警告 PROMETE1012)。アセットは、ゲームの実行プロジェクトで宣言してください。テストからゲームのアセットを読むには、ゲームの実行プロジェクトを参照して `UseAssets(typeof(GameScene).Assembly)` を使います。
- `GenerateAssemblyInfo` を `false` にすると、宣言の情報がアセンブリに書き込まれないので、`PrometeAsset` のアセットは自動では読めません。
- Package モードのままゲームを起動していても、ビルドすればパッケージを作り直せます。ただし、起動中のゲームは古い内容を読み続けます。
- `InvariantGlobalization=true` の環境では、NFC への正規化が行われません。ビルド時のファイル名は NFC になっているので、ASCII 以外のパスは、**最初から NFC で**渡してください。NFD のまま渡すと、見つかりません。

### エラーと警告

ビルドの出力に出る、Assets のコードの一覧です。

| コード | 種類 | 意味 | 対処 |
| --- | --- | --- | --- |
| PROMETE1001 | エラー | `Include` が絶対パスか `..` を含むので、論理パスが決められない | `TargetPath` で論理パスを指定してください |
| PROMETE1002 | エラー | `TargetPath` が正しくない (`..`、`:`、`//` で始まる、空、長すぎる) | `/` で区切った、正しい論理パスにしてください |
| PROMETE1003 | エラー | 同じ論理パスのファイルが複数ある | どちらかの `TargetPath` を変えるか、`Exclude` で除いてください |
| PROMETE1004 | エラー | `Compress` が `auto` / `true` / `false` のどれでもない | 3 つのどれかにしてください |
| PROMETE1005 | エラー | `Package` の名前に、英数字・`_`・`-` 以外の文字がある | 使える文字だけにしてください |
| PROMETE1006 | エラー | `Obfuscate` が `true` / `false` のどれでもない | `true` か `false` にしてください |
| PROMETE1007 | エラー | 宣言したファイルが見つからない | `Include` のパスと、ファイル名の綴りを確認してください |
| PROMETE1010 | エラー | `PrometeAssetMode` が `Folder` / `Package` のどちらでもない | `Folder` か `Package` にしてください |
| PROMETE1011 | 警告 | Folder モードのまま publish した | `PrometeAssetMode=Package` で publish してください。Folder のマニフェストは publish に含まれません |
| PROMETE1012 | 警告 | ライブラリやテストプロジェクトに `PrometeAsset` がある (無視されます) | ゲームの実行プロジェクトで宣言してください。テストは `UseAssets(typeof(GameScene).Assembly)` を使います |
| PROMETE1013 | 警告 | 古い Promete.Web を使っているので、`PrometeAsset` を処理しなかった | Promete.Web を 2.4 以降に更新してください。更新できないときは、`UseAssets(typeof(Program).Assembly, o => o.MountFolder("assets", "assets"))` で従来どおり読めます |
| PROMETE1014 | エラー | 古い Promete.Web で `PrometeAssetMode=Package` を指定した | Promete.Web を 2.4 以降に更新するか、`Package` の指定を外してください |
| PROMETE1015 | エラー | `PrometeAssetEmbed` が `true` / `false` のどちらでもない | `true` か `false` にしてください |
| PROMETE1020 | エラー | パッケージのファイルを書き出せなかった | 出力先の書き込み権限と、ほかのプロセスがファイルを開いていないかを確認してください |
| PROMETE1021 | エラー | マニフェストを書き出せなかった | 出力先の書き込み権限と、ほかのプロセスがファイルを開いていないかを確認してください |

## まとめ

- csproj で `PrometeAsset` を宣言し、`UseAssets()` を呼ぶ。`MountFolder` などの明示的なマウントは、宣言したアセットの上に重なる。
- シーンやプラグインでは、`AssetManager` をコンストラクタで受け取り、`assets.FileSystem` を使う。
- 読むときは `OpenRead`、確認するときは `Exists`、探すときは `Enumerate` を使う。
- 相対パスの基準は実行ファイルのフォルダで、パスは大文字・小文字を区別する。
- 同じパスは、優先度が高いもの（同じなら後に書いたもの）が勝つ。
- `LoadTexture` / `LoadFont` / `LoadAudio` などで、画像・フォント・音声を直接読める。どれも `AssetScope` を指定する。
- 読んだものは自分で `Dispose` せず、`Unload` かスコープに解放を任せる。
- `AssetScope.Global` はアプリが終わるまで残る。`AssetScope.Scene` はそのシーンが使われなくなったときに解放される。
- `...Async` は読み込みをスレッドプールで行い、`Task` を返す。同じものの同時の読み込みは 1 回にまとまる。
- 形式を増やすには `IAudioDecoder` を実装し、`Use<IAudioDecoder, T>()` で登録する。
