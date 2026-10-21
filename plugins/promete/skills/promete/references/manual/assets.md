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

<!-- TODO(Assets Phase 4): PrometeAsset (MSBuild) でパッケージを作る方法へのリンクを足す -->

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
Web (WebAssembly) では、フォントをストリームから読めません。そのため、フォルダにあるフォントは、ファイルのパスから読みます。この場合、フォントは実行環境の側でキャッシュされるため、`AssetScope.Scene` で読んでもシーンが終わっても解放されません（実質的に `Global` と同じです）。埋め込みリソースのフォントは Web では読めず、`NotSupportedException` が発生します。
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

## まとめ

- `UseAssets` で、`MountFolder` や `MountEmbeddedResources`、`MountPackage` を指定する。
- シーンやプラグインでは、`AssetManager` をコンストラクタで受け取り、`assets.FileSystem` を使う。
- 読むときは `OpenRead`、確認するときは `Exists`、探すときは `Enumerate` を使う。
- 相対パスの基準は実行ファイルのフォルダで、パスは大文字・小文字を区別する。
- 同じパスは、優先度が高いもの（同じなら後に書いたもの）が勝つ。
- `LoadTexture` / `LoadFont` / `LoadAudio` などで、画像・フォント・音声を直接読める。どれも `AssetScope` を指定する。
- 読んだものは自分で `Dispose` せず、`Unload` かスコープに解放を任せる。
- `AssetScope.Global` はアプリが終わるまで残る。`AssetScope.Scene` はそのシーンが使われなくなったときに解放される。
- `...Async` は読み込みをスレッドプールで行い、`Task` を返す。同じものの同時の読み込みは 1 回にまとまる。
- 形式を増やすには `IAudioDecoder` を実装し、`Use<IAudioDecoder, T>()` で登録する。
