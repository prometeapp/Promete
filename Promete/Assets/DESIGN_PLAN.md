# Assets API 計画書 (#13)

アセット (画像、音声、フォントなど任意のファイル) を論理パスで扱い、開発時はフォルダから、リリース時はアセットパッケージから読めるようにする。本書はその全体をフェーズに分けた計画をまとめる。

- 本書の範囲: Phase 1〜5 (VFS、型付きローダー、パッケージ形式、MSBuild 統合、難読化)。
- 範囲外 (別の計画で扱う): CLI、リモートから取得する Asset Bundle、Android などモバイル向けのソース。§7 に、本書の設計でそれらを塞がないための約束だけ書く。
- 確認できていないことは「未確認」「要スパイク」と明記する。

## 0. 決定事項と、残っている判断

**決定済み**

1. **2 層構成にする。** 下の層は VFS (論理パス → `Stream`)、上の層はキャッシュとスコープつきの型付きローダー。
2. **型付きローダーは、既存の読み込み API を再実装しない。** VFS から `Stream` を開き、`TextureFactoryBase.Load(Stream, TextureOptions)` や `Font.FromStream` などの既存のオーバーロードに引数ごと渡す、薄いアダプタにする。
3. **既存の `TextureFactory.Load(string path)` などの挙動は変えない。** 文字列のパスは今までどおり実ファイルのパスとして扱う。パッケージからの読み込みは Assets API 側で行う。
4. **解放はスコープで管理する。** スコープは `Global` と `Scene` の 2 つから選ぶ。参照カウントは持たない。明示的に Unload する口は残す。
5. **同期と非同期の両方の API を出す。**
6. **マウントは非同期でよいが、マウントした後の読み込みは同期でランダムアクセスできること。** Web (同期の fetch ができない) と Android (AssetManager は同期で読める) の差は、ここで吸収する。
7. **難読化は抑止策であって、DRM ではない。** 目的は「取り出すのに明確な操作が要る」状態にすること。ドキュメントにもそう明記する。
8. **パスはハッシュ化しない。** 代わりに目次 (TOC) ごと暗号化する。これで、フォルダ単位の列挙ができるまま、ファイル名も見えなくなる。
9. **MSBuild の item 名は `PrometeAsset`。** パックとビルド統合はコアに含める。コアの NuGet パッケージは既に props / targets を `build/` と `buildTransitive/` に入れて配っているので、それに足す。
10. **パック処理の本体は netstandard2.0 のライブラリにする。** MSBuild Task (VS 版の MSBuild は .NET Framework で動く) と、将来の CLI の両方から参照するため。この制約から、圧縮は Deflate を使う (Brotli は netstandard2.0 に無い)。
11. **非破壊の追加として v2.x のマイナーリリースに入れる。** ドキュメントには `v2.x~` の表記を付ける (x は入れるリリースで決める)。
12. **Web の既存の `PrometeAsset` と意味を揃える。** Promete.Web は実験的な扱いだが公開済みなので、今の挙動 (ばらのファイルを MEMFS にプリロード) を Folder モードとしてそのまま残す (§4.3)。
13. **音声の形式はデコーダーを DI で差し込めるようにする。** §3.1 の `LoadAudio` が wav / ogg 以外も扱えるようにするため (§3.4)。
14. **API の名前は `AssetManager`。** (D1。Android の `Android.Content.Res.AssetManager` と名前が同じになるが、将来 Android 向けのソースを書くときにエイリアスが要る程度なので構わない)
15. **パスは大文字・小文字を区別する。** Linux / Android / Web と挙動を揃えるため。(D2)
16. **`Scene` スコープは、シーンのインスタンス単位で持つ。** シーンが `PopScene` や `LoadScene` で使われなくなったときに、そのシーンのスコープを破棄する。`PushScene` で下に積まれたシーンのスコープは残す。(D3)
17. **リリース時のパッケージは、既定では実行ファイルの隣に置く。** オプションで埋め込みリソースにもできるようにする。(D4)
18. **ホットリロードは範囲外。** 拡張の余地として、Phase 2 の設計で後から足せる形にしておくだけにする。(D7)
19. **大文字・小文字だけ違うパスは、フォルダから読む場合も例外にする。** Windows / macOS のファイルシステムは既定で区別しないので、開発中は通ってリリースで落ちる。これを Promete 側で防ぐ。例外のメッセージには、実際のファイル名と、パスが大文字・小文字を区別することを書く (§2.2)。(D2')
20. **難読化は ChaCha20 を自前で実装する。** (D6、§5)
21. **鍵は、自動生成と固定のどちらかを選べるようにする。** (D6'、§5)

22. **シーンのコンストラクタ (フィールド初期化子を含む) で読み込んだものは、生成中のそのシーンのスコープに帰属させる。** コアの内部に「生成中のシーン」を設定するフックを足して実現する (§3.3)。(B1)
23. **キャッシュのエントリは「所有するスコープの集合」を持ち、集合が空になったら解放する。** 複数のシーンが同じキーを読んでも、先に破棄されたシーンが、まだ使っている他のシーンの分まで解放しないようにするため。利用者から見えるハンドル単位の参照カウントではないので、決定事項 4 とは矛盾しない。古いシーンのスコープの解放は、新しいシーンの `OnStart` の後まで遅らせる (§3.3)。(B2)
24. **論理パスは `TargetPath` で決める。** 省略時の既定値は、Web と同じくプロジェクトからの相対パス。Folder モードでは「論理パス → 実ファイル」のマニフェストを出力先に置く (§4.2)。(B3)
25. **Web の Package モードでは、`promete.js` のプリロードがパッケージを MEMFS に書き、C# 側は同期でマウントする。** (§4.3)。(B4)
26. **コアの targets は、Web を含むすべての環境で同じ処理をする (属性を書く。マニフェストを書くのはデスクトップだけで、Web は決定事項 32)。** 環境ごとに違うのは、ファイルを届ける方法 (出力先へのコピーか、静的 Web アセットと MEMFS へのプリロードか) とマニフェストの中身だけ。属性には絶対パスを書かない。パックするのは実行プロジェクトだけで、ライブラリのプロジェクトの `PrometeAsset` は警告を出して無視する (§4.2)。(B5 を 2 回目のレビューの C1 で改めた)
27. **引数なしの `UseAssets()` は、Phase 2 の時点から「属性があれば使い、無ければ何もしない」とする。** 明示的なマウントは、その上に高い優先度で重ねる。相対パスの基準は `AppContext.BaseDirectory`。エントリアセンブリが取れない環境 (WASM) では、引数なしの `UseAssets()` は例外にし、`UseAssets(Assembly)` で属性を持つアセンブリを明示させる (§2.1)。(B6、C1)
29. **Web の Folder モードでも、マニフェストに基づくソースを使う。** MEMFS のルートをマウントすると `/dev` などまで見え、宣言していないファイルも読めてしまうため (§4.3)。(C2)
28. **nonce はパスと内容のハッシュから決める。** 再現可能なビルドとインクリメンタルビルドのため (§5)。(B7)

30. **キャッシュで共有する `IAudioSource` は、呼び出し元ごとの状態を持たず、`FillSamples` が並行して呼ばれても安全であること。** これを規則として定める。今の Wave / Vorbis はこれを満たす。共有できないソース (MIDI など) に対応するときに、デコーダーに「共有できるか」を示す既定実装つきのメンバーを足し、共有できないものだけ毎回作る (§3.4)。(C3)

31. **出力先に置くマニフェストとパッケージの名前には、アセンブリ名を含める** (例: `<AssemblyName>.promete-assets.json`、`<AssemblyName>.<Package>.<拡張子>`)。属性にはその相対ファイル名を書く。参照元のプロジェクトの出力先にも伝播させる (publish は Folder モードのマニフェストを除く)。別の実行プロジェクトから参照されたとき (Web Example → Promete.Example、テスト → ゲーム) に名前がぶつからず、`UseAssets(Assembly)` の対応とも揃うため (§4.2)。(D1)
32. **Web 形式のマニフェストの生成は `Promete.Web.targets` が持つ。** コアの targets は「論理パス・ファイル・kind」の item を作るところまで。`promete.js` との契約は Promete.Web の中に閉じる。コアは、Web 向けのビルドで Promete.Web が対応を示していなければ、警告を出して **Promete.Web 2.4 以降への更新を促す** (決定事項 33。§4.2)。(D2)
33. **新しいコアと古い Promete.Web の組み合わせは、警告に留める。** コアは Assets の処理 (属性・パック・item) を一切せず、古い Promete.Web の挙動のまま動かす。`PrometeAssetMode=Package` を明示したときだけエラーにする。決定事項 11 (非破壊) を守りつつ、古い Promete.Web のプロジェクトをデスクトップとして処理させないため (§4.2)。(E1。決定事項 32 の「更新を強制する」を改めた)
34. **実行時に Web 形式のマニフェストを読むのは Promete.Web。** Promete.Web が、マニフェストを読んでソースを作る実装を DI で差し込む。コアはその差し込み口だけを持つ。形式を変えるときに Promete.Web だけを直せばよいようにするため (§4.2)。(E2)

**残っている判断 (方針レベル)**: なし (2026-10-21 時点)。

**実装時に決めるもの** (方針には影響しない)

- §3.2: 同値比較できない引数 (SpriteSheet の矩形の配列など) を持つ読み込みの、キャッシュのキー。
- §3.4: MeltySynth の MIDI を、Phase 2 で `IAudioDecoder` に対応させるか。
- §3.4: `VorbisAudioSource` の逐次読み出しへの置き換え。**本計画とは別のタスク**として扱う。Assets はどちらの実装でも動くように作る (§3.4 の規則を守れば動く)。
- §3.5: Web のフォントの案 1 / 案 2 (要調査)。
- §4.1: パッケージのストリームの共有方式 (要スパイク)。
- §4.2: `IAssetManifestLoader` (と、Web の実装が返すソースが使う `IFileBackedAssetSource` / マニフェストに基づくソース) を public にするか。推奨は public。名前空間を `Promete.Assets.Infrastructure` などに分け、XML コメントで「バックエンド実装者向け」と明記する。internal + `InternalsVisibleTo` にすると、コアと Promete.Web は別々に版が付くので、「新しいコア + 少し古い Promete.Web」でシグネチャがずれたときに実行時に落ちる。public にすれば、`PrometeWebAssetsContract` の版の中で互換を守れる。**Phase 4 の着手前に決める。**
- §2.1: 何もマウントされていないときの例外の文面。属性の無いアセンブリ、テストホスト、新しいコア + 古い Promete.Web で属性が書かれなかったときに、「属性が見つからず、何もマウントされていない」と分かるようにする。最後のケースでは MEMFS にプリロードはされているので、`MountFolder("assets", "assets")` で読めることを警告の文面かガイドで案内する。

---

## 1. 現状 (コードで確認した事実)

**読み込み API**

- 読み込み API の多くは `string path` と `Stream` の両方のオーバーロードを持つ。`TextureFactoryBase` (`Load` / `LoadSpriteSheet` / `Load9Sliced` と、それぞれの `Async`)、`Font.FromStream` / `IFontProvider.FromStream`、`WaveAudioSource(Stream)` / `VorbisAudioSource(Stream)`、`ImageDecoder.Decode(Stream)`。
- `TextureOptions` は `readonly record struct` なので、キャッシュのキーにそのまま使える。
- コアに `LoadAudio` のような「パスから音声を読む」API は無い。`IAudioProvider` は `CreateOutput` / `PlayOneShotAsync` だけ。
- `EmbeddedResource` は Promete 自身のアセンブリの埋め込みリソースしか読めない。利用者のアセンブリから読むヘルパーは無い。

**ストリームを閉じるタイミング** (型ごとにばらばら)

- `WaveAudioSource`: コンストラクタの中で `BinaryReader` ごと閉じる (`Promete/Audio/WaveAudioSource.cs:75`)。
- `VorbisAudioSource`: コンストラクタから戻った**後も**、別スレッドでストリームを読み続ける。NVorbis の `VorbisReader` が、デコードのスレッドの終わりに閉じる (`Promete/Audio/VorbisAudioSource.cs:31,39-66`)。`Dispose` はキャンセルするだけで、閉じるのは後で別スレッドから行われる (`VorbisAudioSource.cs:115-119`)。
- NVorbis は **seek できないストリームを受け付けない** (`CanSeek == false` で `ArgumentException`)。
- フォント: `FreeTypeGlyphSource.FromStream` は中身をコピーするだけで、閉じない。
- テクスチャ: `ImageDecoder.Decode(Stream)` も閉じない。`LoadAsync(Stream)` は、完了するまでストリームを破棄してはいけない (`Promete/Graphics/TextureFactoryBase.cs:440-453`)。

**リソースの破棄**

- `Texture2D` は `readonly struct` で、`Dispose` は毎回 `DestroyTexture(handle)` を呼ぶ。冪等ではない (`Promete/Graphics/Texture2D.cs:75-77`)。GL はハンドルの番号を使い回すので、二重に Dispose すると、後から作られた無関係なテクスチャを消しうる。
- `Font` は `IDisposable` ではない。リソースを持つのは `IGlyphSource` (`IDisposable`)。
- `FreeTypeFontProvider.FromStream` は、呼ぶたびに新しい `IGlyphSource` を作る。サイズ違いで何度も呼ぶと、同じ TTF をその数だけネイティブメモリへ複製する。
- `FreeTypeFontProvider.FromFile` と Web の `CanvasFontProvider.FromFile` は、グリフソースを provider の中に永続的にキャッシュしている。これを外から Dispose すると、次の `FromFile` で破棄済みのものが返る。

**シーンの遷移** (`Promete/PrometeApp.cs`)

- `SceneWillChange` は `SceneTransitionEventArgs(Type, Previous, Next)` を渡す。Load と Pop では Previous が破棄されるシーン。
- イベントは、新しいシーンの**コンストラクタの後**、`OnStart` の前に出る。Load では `OnDestroy` → `GetScene` (コンストラクタ) → `SceneWillChange` → `OnStart` の順 (`PrometeApp.cs:395-414`)。
- 次の経路では、`SceneWillChange` が出ないままシーンが破棄される。
  - `ClearSceneStack` (`PrometeApp.cs:484-490`)
  - アプリの終了時 (`PrometeApp.cs:635-642`)
  - `LoadScene` で `GetScene` が例外を投げたとき。前のシーンは `OnDestroy` 済み (`PrometeApp.cs:403-408`)。
- 今のシーンを取得する公開 API は無い。
- **以上から、`SceneWillChange` だけでは Scene スコープを正しく管理できない。** シーンの生成と破棄を知るフックを、コアの内部 (internal) に足す (§3.3)。
- プラグインのインスタンスが `OnStart` で確実に作られるのは、`IInitializable` / `IUpdatable` / `IDisposable` のどれかを実装したものだけ (`PrometeApp.cs:542-552`)。

**Web**

- `Promete.Web` は、`<PrometeAsset Include="assets/**" />` で宣言したファイルを、起動前に全部 fetch して Emscripten の MEMFS に置いている (`promete.js` の `preloadAssets`、`build/Promete.Web.targets`)。そのため `File.ReadAllBytes` などの同期 API がそのまま動く。MEMFS 上のパスは `TargetPath` で、省略時はプロジェクトからの相対パス。
- **名前の同じ item が既にあるので、意味を揃える必要がある** (§4.3)。
- Web の `CanvasFontProvider.FromStream` は `NotSupportedException` を投げる。`FontFace` の登録が非同期だから。フォントだけは、パスを渡す `FromFile` でしか読めない (MEMFS にプリロードされたファイルを、事前に `FontFace` に登録している)。
- Web では `Run` がすぐに戻る。プラグインの `OnStart` の中では await できない。

**ビルド**

- リポジトリ内のプロジェクトは、`Directory.Build.targets` 経由で `Promete/build/Promete.targets` を Import している。NuGet パッケージから使うときとは、ファイルの場所が違う。`Promete.props` はリポジトリ内では Import されていない (必要な設定は `Directory.Build.props` に直接書いてある)。Phase 4 で `Promete.props` に何か足すなら、`Directory.Build.props` からも Import する。
- Web Example は、`Promete.Web` の props を csproj の先頭で手で Import している (`Promete.Web.Example.csproj:3`)。
- SceneGen (netstandard2.0) は `Directory.Build.props` で `ProjectReference` され、`PublishAot` を `UndefineProperties` で外している。外さないと NETSDK1207 になる。
- `Promete.HeadlessTest` はテストプロジェクトではなく、シーンを動かすだけの Exe。ヘッドレスでのシーンのテストは `Promete.Test` で `BuildWithHeadless()` を使って書かれている。

---

## 2. Phase 1: VFS

型付きローダーやパッケージの土台になる、論理パスから `Stream` を得る層。

### 2.1 作るもの

- `IAssetSource`: 1 つの読み込み元。
  - `bool Exists(string path)`
  - `Stream OpenRead(string path)`。**常に seek できるストリームを返す** (NVorbis が要求するため。§1)。
  - `IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)`
- `FolderAssetSource`: 実際のフォルダを読む。`UseAssets(o => o.MountFolder(...))` で明示的にマウントするときに使う (Web では MEMFS のフォルダを指定できる)。MSBuild 統合の Folder モードでは、デスクトップでも Web でも、これではなくマニフェストに基づくソースを使う (§4.2)。
- `IFileBackedAssetSource`: 実ファイルを持つソースが任意で実装するインターフェース。`bool TryGetFilePath(string path, out string fullPath)` を持つ。`FolderAssetSource` とマニフェストに基づくソースが実装する。Web のフォントを `FromFile` で読むときに使う (§3.5)。
- `EmbeddedResourceAssetSource`: 任意のアセンブリの埋め込みリソースを読む。Phase 4 でパッケージを埋め込むときにも使う。
- VFS 本体 `AssetFileSystem` (名前は仮): 優先度つきで複数のソースをマウントする。`AssetManager` が内部に持ち、単体でも公開する。読むときは優先度の高いソースから探す。後からマウントしたソースで上書きできるので、DLC やパッチも同じ仕組みで扱える。
  - `Mount(IAssetSource, int priority)` / `MountAsync(...)` / `Unmount`
  - `Exists` / `OpenRead` / `Enumerate` (全ソースの結果をマージする)
  - `TryGetFilePath(path, out fullPath)`: 優先度で勝ったソースが `IFileBackedAssetSource` なら、それに問い合わせる。
  - `Unmount` と `AssetFileSystem` の Dispose では、`IDisposable` を実装したソースを Dispose する。ただし、既に開いたストリームは閉じられるまで有効のまま残す (§4.1)。
- DI での登録: `PrometeApp.Create().UseAssets(...)` (決定事項 27)。
  - 引数なしの `UseAssets()` は、エントリアセンブリに Assets の属性 (§4.2) があればそれに従ってマウントし、無ければ何もマウントしない。Phase 2 の時点では属性がまだ無いので、何もマウントしない。こうしておけば、Phase 4 で属性を足しても `UseAssets()` の意味は変わらない。
  - エントリアセンブリが取れない環境 (WASM。`PrometeApp.cs:689-691`) では、引数なしの `UseAssets()` は例外にする (決定事項 27)。メッセージで `UseAssets(Assembly)` を案内する。アセンブリを総当たりで探さないのは、Web Example のように別の実行プロジェクトのアセンブリ (`Promete.Example.dll`) も読み込まれていて、誤った属性を拾うため。
  - 明示的なマウントは、属性によるマウントより高い優先度で重ねる。
  - `MountFolder(physicalPath, mountPoint)` のように、論理パスの接頭辞を指定できるようにする。ガイドの例は、Phase 4 の `TargetPath` の既定 (§4.2) と同じ論理パスになるもの (`MountFolder("assets", "assets")` → `"assets/x.png"`) にする。Phase 2 のガイドに沿って書いたコードを、Phase 4 で書き換えずに済むようにするため。
  - 存在しないフォルダを `MountFolder` したら、マウントする時点で `DirectoryNotFoundException` にする。黙って空のソースにすると、Release でフォルダが無いときに原因が分かりにくくなるため。
  - 相対パスの基準は `AppContext.BaseDirectory`。カレントディレクトリにすると、`dotnet run` を実行した場所で変わってしまうため。
  - オーバーロードは `UseAssets(Action<…>? configure = null)` と `UseAssets(Assembly, Action<…>? configure = null)` の 2 系統にする。前者は、WASM では `configure` の有無に関係なく例外にする。Web で明示的にマウントするときは `UseAssets(typeof(Program).Assembly, o => ...)` を使う (属性が無いアセンブリなら、明示的なマウントだけが効く)。
  - テストホストのように、エントリアセンブリがアプリではない環境では、引数なしの `UseAssets()` は (例外にならず) 何もマウントしない。テストでは `UseAssets(typeof(X).Assembly)` か、明示的なマウントを使う。ガイドにも書く。
  - 属性によるマウントと明示的なマウントは、`AssetManager` のコンストラクタで行う。プラグインの `OnStart` は登録した順に呼ばれる (`PrometeApp.cs:540-558`) ので、`AssetManager` の `OnStart` でマウントすると、先に登録された他のプラグインが `OnStart` で読み込めないため。

### 2.2 パスの規則

- 区切り文字は `/`。`\` を含むパスは例外にする (Windows でだけ通ってしまうのを防ぐ)。
- 先頭の `/` があってもなくても同じ扱い。`.` と `..` はマウントの外に出られないよう正規化する。
- Unicode は NFC に正規化して扱う。パックするときと照合するときの両方で揃える。macOS 由来のファイル名は NFD になっていることがあり、日本語のパスが一致しなくなるため。
  - パッケージとマニフェストは、生成する段階 (ビルド時) で正規化しておく。実行時に正規化が要るのは、利用者が渡したパスと、`FolderAssetSource` がディレクトリを列挙したときの名前だけ。
  - `InvariantGlobalization=true` (NativeAOT や Web でサイズを減らすためによく使われる) のとき、実行時の `string.Normalize` が使えるかどうかは**要スパイク**。使えない場合は、実行時の正規化をせず、ASCII 以外のパスでは利用者が NFC で渡すことをガイドに書く。
- `Enumerate` の pattern は `*` と `?` だけを使え、大文字・小文字を区別する。
- 大文字・小文字は区別する (決定事項 15)。
- `FolderAssetSource` は、ファイルシステムが区別しない環境 (Windows / macOS の既定) でも区別して扱う (決定事項 19)。
  - `File.Exists` は大文字・小文字を無視して通ってしまう。そのため、パスの各要素 (ディレクトリも含む) を、親ディレクトリを列挙して得た実際の名前と序数比較 (`StringComparison.Ordinal`) で照合する。
  - 毎回列挙すると遅いので、ディレクトリごとの名前の一覧をキャッシュする。開発中にファイルが増えることがあるので、見つからなかったときはそのディレクトリを列挙し直してから判断する。
  - 一致しないときは、大文字・小文字を無視すれば一致する名前を探す。見つかればメッセージに含める。例: `'Player.png' が見つかりません。'player.png' はあります。アセットのパスは大文字・小文字を区別します。`
  - ファイルシステムが区別する環境 (Linux など) でも、同じ照合をする。環境ごとに分岐させないため。コストが問題になったら、そのとき見直す。

### 2.3 スレッド安全性

- 非同期の読み込みはスレッドプールで動く (`LoadAsync` は `Task.Run` でデコードする。Vorbis もスレッドプールで読む)。そのため VFS は、どのスレッドから呼ばれても安全であること。
  - `FolderAssetSource` のディレクトリ名のキャッシュは、並行アクセスに耐えること。
  - `Mount` / `Unmount` は、読み込み中に呼ばれても安全であること。マウントの一覧はコピーオンライトのスナップショットにする。

### 2.4 完了条件

- `Promete.Test` に次のテストがある。
  - マウントの優先度、上書き、列挙のマージ
  - パスの正規化 (`\` の拒否、`..` の処理、NFC)
  - 大文字・小文字だけ違うパスで例外になることと、その例外のメッセージ
- デスクトップと Web (MEMFS) で、`FolderAssetSource` から読める。

---

## 3. Phase 2: 型付きローダーとスコープ

VFS の上に、キャッシュとスコープを持つ読み込み API を作る。この時点では、まだフォルダからしか読めない。それでも開発時には使える状態になる。

### 3.1 API の形 (案)

```csharp
// 既存の API の引数をそのまま受ける、薄い型付きメソッド
assets.LoadTexture("player.png", options, AssetScope.Scene);
assets.LoadSpriteSheet("tiles.png", 4, 1, (16, 16), AssetScope.Scene);
assets.Load9Sliced("frame.png", 16, 16, 16, 16, AssetScope.Global);
assets.LoadFont("ui.ttf", 16, FontStyle.Normal, scope: AssetScope.Global);
assets.LoadAudio("bgm.ogg", AssetScope.Scene);

// 汎用の口: 利用者が自前の型を読ませるとき
assets.Load(path, key, stream => MyFormat.Parse(stream), scope);

// それぞれに Async 版がある
await assets.LoadTextureAsync("player.png", options, AssetScope.Scene, ct);
```

- 型付きメソッドは Texture / SpriteSheet / 9Slice / Font / AudioSource に絞る。それ以外は汎用の `Load` で受ける。
- `IAssetLoader<T, TOptions>` のような抽象は、要望が出るまで作らない。
- **AssetManager が返したリソースは、利用者が Dispose してはいけない。** 解放は `Unload` で行う。`Texture2D` の Dispose は冪等ではないので、利用者とスコープの両方が Dispose すると、無関係なテクスチャを消しうる (§1)。ガイドと XML コメントに明記する。

### 3.2 キャッシュ

- キャッシュのキーは (読み込みの種類, 正規化した論理パス, オプション) の組。`TextureOptions` は record struct なのでそのまま使える。
  - 種類を含めるのは、同じ `tiles.png` を `LoadTexture` と `LoadSpriteSheet` の両方から読むと、別のリソース (単体のテクスチャと、ハンドルを共有するセルの配列) になるため。
  - パスは §2.2 で正規化した後のものを使う。`./assets/x.png` と `assets/x.png` が同じエントリを指すようにするため (Example には `"./assets/..."` の書き方が実際にある)。
  - 汎用の `Load` の key は、型付きメソッドのキーとは別の名前空間で扱う。
- フォントは、**グリフソースをパスごとに 1 つキャッシュ**し、`Font` は `Font.FromGlyphSource(source, size, style, isAntialiased)` で作る。サイズごとに `FromStream` を呼ぶと、同じフォントのデータをその数だけ複製するため (§1)。CJK のフォントは数 MB〜数十 MB ある。スコープが解放するのは `IGlyphSource`。
- 引数がそのままでは同値比較できないもの (`LoadSpriteSheet(..., params ReadOnlySpan<RectInt>)` など) のキーは、実装時に決める。
- 各エントリは「所有するスコープの集合」を持つ (決定事項 23)。同じキーを別のスコープ (別のシーンや Global) から読むと、そのスコープを集合に足す。スコープが破棄されると集合から外れ、集合が空になったエントリだけを解放する。`Unload(path, scope)` は、引数で指定したスコープを集合から外す。
- スレッド安全性:
  - キャッシュはロックで守る。
  - 同じキーの非同期の読み込みが重なったときは、実行中の `Task` を共有する。
  - 読み込みの要求を出した時点で、要求したスコープを所有するスコープの集合に足す。完了する前にスコープが破棄されたら、集合から外す。完了した時点で集合が空なら、結果をすぐに Dispose する。実行中の `Task` に別のスコープが合流していれば、そのスコープが持ち続ける。
  - `AssetScope.Scene` がどのシーンを指すかは、呼び出した時点にメインスレッドで決める。メインスレッド以外から `AssetScope.Scene` で呼ばれたら例外にする。「生成中のシーン」や「今のシーン」は、メインスレッドでしか一貫しないため (`PrometeApp` に `IsMainThread` がある)。

### 3.3 スコープ

- `Global`: アプリの終了まで残す。明示的に Unload したときだけ解放する。
- `Scene`: シーンのインスタンス単位で持ち、そのシーンが使われなくなったときにまとめて Dispose する (決定事項 16)。
  - `SceneWillChange` だけでは足りない (§1)。コアの内部 (internal) に、シーンの生成と破棄を知るフックを足す。破棄の通知は、`OnDestroy` を呼ぶすべての経路 (`ClearSceneStack`、アプリの終了、`LoadScene` の例外の経路を含む) で受ける。
  - シーンのコンストラクタ (フィールド初期化子を含む) で読み込んだものは、生成中のそのシーンに帰属させる (決定事項 22)。
    - シーンのインスタンスは、コンストラクタが終わるまで存在しない。しかも派生クラスのフィールド初期化子は、基底の `Scene` のコンストラクタより先に走る。そのため、「生成中のシーン」はインスタンスでは持てない。
    - そこで、コアが `GetScene` を呼ぶ前にスコープのオブジェクトを先に作り、それを「生成中」に設定する。生成に成功したら、そのスコープをシーンのインスタンスに結び付ける。
    - 「生成中」は、遷移が入れ子になる (`OnStart` の中で `LoadScene` を呼ぶなど) 場合に備えてスタックで持ち、`finally` で戻す。
    - 「今のシーン」は別に保持せず、常に `_currentScene` から求める。保持すると、内側の遷移で破棄されたシーンを、外側の遷移が「今のシーン」に戻してしまうため。
    - `AssetScope.Scene` を指定した読み込みは、生成中のスコープがあればそれに、無ければ今のシーンのスコープに帰属する。
    - DI がシーンを解決する途中で、初めて生成されるシングルトンのコンストラクタが `AssetScope.Scene` で読むと、それも生成中のシーンに帰属する。ガイドで注意する。
  - Push では、上に積むシーンの生成中に読んだものは、上のシーンに帰属する (下のシーンではない)。
  - シーンが 1 つも無いとき (`PrometeApp` の組み立て中や、プラグインの `OnStart` の中など) に `AssetScope.Scene` で読むと、例外にする。
  - `GetScene` が例外を投げたら、生成中のスコープもすぐに解放する (Load / Push の両方。コンストラクタの途中までに読んだ分が残らないように)。
  - 解放するタイミング (決定事項 23):
    - 解放は、遷移の最後に行う。Load / Push では新しいシーンの `OnStart` の後、Pop では下のシーンの `OnResume` の後。`finally` で行い、`OnStart` / `OnResume` が例外を投げても解放する。新しいシーンが同じアセットを読めば、所有するスコープの集合に入るので、読み直さずに済む。
    - `LoadScene` が `GetScene` の例外で失敗したときは、遅らせずにすぐ解放する。
    - `ClearSceneStack` とアプリの終了では、各シーンの `OnDestroy` の直後に解放する。`ClearSceneStack` はスタックに積まれたシーンだけを破棄し、今のシーンは破棄しない (`PrometeApp.cs:484-490`)。
    - どの経路でも、解放は `OnDestroy` より後に行う。`OnDestroy` の中ではまだアセットを使えるようにするため。
- 明示的な `Unload(path)` / `Unload(scope)` も残す。
- `AssetManager` は `IDisposable` を実装する。アプリの終了時に Global を解放するためと、`OnStart` で確実にインスタンスを作らせるため (§1)。DI コンテナの破棄でもう一度 Dispose されるので、Dispose は冪等にする。

### 3.4 音声のデコーダーとストリームの寿命

**デコーダーの差し込み**

- コアには、wav / ogg 以外の形式を扱う方法が無い。そこで、デコーダーを DI に登録できるようにする。

```csharp
public interface IAudioDecoder
{
    // 拡張子と、先頭数バイト (RIFF / OggS などのマジック) を見て判定する
    bool CanDecode(string path, ReadOnlySpan<byte> header);

    // stream を閉じる責任はデコーダー (と、返した IAudioSource) が持つ
    IAudioSource Decode(Stream stream);
}
```

- 置き場所は Assets ではなく、コアの Audio 側にする。Assets を使わない場合でも、`Stream` から音声を読めるようにするため。
- 標準で wav と ogg のデコーダーを登録する。利用者が `Use<IAudioDecoder, MyDecoder>()` で足したものは、標準のものより先に判定する。`Use<TPlugin, TImpl>` は `AddSingleton` なので複数登録でき、コアが Build のときに標準のものを足せば、列挙の順で利用者のものが先になる。これは `IEnumerable<IAudioDecoder>` で全部を列挙する場合の話。単一の解決で使うサービス (`IAssetManifestLoader` など。§4.2) に同じ書き方をすると、コアの既定のものが利用者や Promete.Web の登録を上書きするので、そちらは `TryAddSingleton` にする。
- MeltySynth の MIDI は、この形にきれいには乗らない。`MeltySynthAudioSource` はシンセサイザーそのもので、SoundFont という外部の依存を持つから。MIDI ファイルを `IAudioSource` として返すデコーダーにして、SoundFont は DI から受け取る形が考えられる。Phase 2 で MeltySynth まで対応するかは、実装するときに決める。

**キャッシュで共有するソースの規則** (決定事項 30)

- `LoadAudio` の結果はキャッシュされ、複数の呼び出し元 (同時に鳴らす複数の `AudioPlayer` など) で共有される。そのため、キャッシュで共有する `IAudioSource` は次を満たすこと。
  - 呼び出し元ごとの状態 (読み出し位置など) に依存しない。デコードの進み具合のように、すべての呼び出し元で共通の状態に依存するのは構わない (`VorbisAudioSource` の `FillSamples` は、デコード済みの範囲 `LoadedSize` に依存する)。
  - `FillSamples` が複数のスレッドから並行して呼ばれても安全。
- 今の `WaveAudioSource` と `VorbisAudioSource` はこれを満たす (`FillSamples` はデコード済みのバッファを読むだけ)。
- `VorbisAudioSource` を逐次読み出しにする別タスク (後述) では、これを要件にする。1 本のストリームを読み出し位置ごとに共有する作りにはしない。
- MIDI (`MeltySynthAudioSource`) のように状態を持つソースは、この規則を満たせない。対応するときは、`IAudioDecoder` に「共有できるか」を示す既定実装つきのメンバー (例: `bool IsShareable => true`) を足し、共有できないものはキャッシュせず、呼ぶたびに新しいインスタンスを作る (スコープは Dispose のためだけに追う)。

**ストリームの所有権**

型ごとに閉じるタイミングが違う (§1) ので、ローダーの規則を次のように決める。

- テクスチャとフォント: ローダーが閉じる。同期版は呼び出しの直後に、非同期版は await の完了後に閉じる。
- 音声: デコーダーが責任を持って閉じる。閉じる時点は実装による (wav はすぐ、Vorbis はデコードの終わり)。`IAudioSource` は `IDisposable` を継承していないので、ローダーは「`IDisposable` を実装していれば、スコープの解放で Dispose する」という扱いにする。インターフェースに `IDisposable` を足すのは破壊的変更になるので、v3 で検討する。
- 汎用の `Load`: ファクトリが戻った後に、ローダーが閉じる。所有権をファクトリに渡したい場合のために、別のオーバーロードを用意する。

**`VorbisAudioSource` の逐次読み出し**

- `VorbisAudioSource` を、逐次読み出し (再生しながらデコードする形) に置き換える案がある。本計画とは別のタスクとして扱う。
- 逐次読み出しにした場合、パッケージのリーダー (§4.1) に次のことが求められる。
  - 再生している間、ずっとストリームが開いたままになる。BGM と SE を同時に鳴らせば、複数のストリームが同時に開く。
  - オーディオスレッドから読まれる。
  - ループしたり、`FillSamples` の `offsetFrames` が飛んだりしたときには seek が要る。
- これらは、どれもパッケージのリーダーに元から求めていることと同じ。seek は §2.1 の「`OpenRead` は常に seek できるストリームを返す」で満たされる。ogg は `Compress="auto"` で元から無圧縮になり、明示的に圧縮した場合も展開済みの `MemoryStream` で返るので動く (§4.1)。
- 逐次読み出しにすると、Web で `VorbisAudioSource` の読み込みがメインスレッドを止める問題 (Promete.Web/DESIGN_NOTES.md) も解消する見込み。ストリームの所有権もはっきりするので、Assets から見ても逐次読み出しの方が扱いやすい。

### 3.5 Web のフォント (要調査)

- `CanvasFontProvider.FromStream` は対応していない (§1)。このフェーズでは、Web でフォントを読むときだけ、`AssetFileSystem.TryGetFilePath` (§2.1) で MEMFS 上のパスを得て `FromFile` に渡す経路にする。
- `FromFile` で得たグリフソースは provider がキャッシュしているので、**スコープの解放の対象にしない** (§1)。そのため Phase 2 の Web では、フォントは実質的に Global 扱いになる。
- Phase 3 以降、パッケージの中のフォントを Web で読めるようにするには調査が要る。どちらにするかは調査の結果で決める。**この調査は、Phase 4 の Web の Package モードより前に終える** (Web Example がフォントを使うので、決まらないと §4.4 の完了条件を満たせない。§8)。
  - 案 1: バイト列から `FontFace` を登録する非同期の口を足す。`IFontProvider` に既定実装つきのメンバー (DIM) として足せば非破壊で入れられる。ただし、同期の `LoadFont` を Web でどう扱うかが問題になる。
  - 案 2: パッケージをマウントするときに、中にあるフォントを先に全部 `FontFace` に登録しておく。今の MEMFS へのプリロードと同じ考え方。暗号化した目次は JS からは読めないので、登録は C# 側から行う。
- メモ: `IFontProvider.FromStream` には `faceIndex` が無い。TTC に対応するときの拡張点。

### 3.6 完了条件

- `Promete.Test` で、`BuildWithHeadless()` を使って次のテストがある。
  - スコープの解放 (Load / Push / Pop / `ClearSceneStack` / 終了の各経路)。`OnDestroy` の中ではまだ使えること。
  - コンストラクタやフィールド初期化子で読んだものが、そのシーンに帰属すること。
  - Push の上下のシーンが同じキーを持ったとき、上を Pop しても下の分が残ること。
  - 同じシーンへの Load で、同じアセットを読み直さないこと (解放の遅延)。
  - シーンの生成に失敗したとき、生成中のスコープが解放されること。遷移が入れ子になったとき (`OnStart` の中で `LoadScene`)。
  - キャッシュのヒット (種類が違えば別のエントリになること、正規化したパスで同じエントリになること)。非同期の読み込みの重複の共有と、完了前にスコープが破棄されたときの扱い。
  - 同期と非同期の両方。
- `Promete.Example` にデモがある。
- `Promete.Docs` にガイドがあり、Agent Skill も同期してある。

---

## 4. Phase 3: パッケージ形式 / Phase 4: MSBuild 統合

### 4.1 Phase 3: パッケージ形式

- 構成: ヘッダ (マジック、形式のバージョン、フラグ)、目次 (TOC)、データ部。
- 目次の各エントリ: パス (NFC)、オフセット、格納サイズ、元のサイズ、フラグ (圧縮、暗号化)。
- 圧縮はエントリごとに決める。Deflate のみ。png / ogg / wav などは無圧縮で格納する。
- **Phase 5 のための予約**: Phase 3 の時点で、次のものを形式に含めておく。そうすれば Phase 5 で形式のバージョンを上げずに済む。
  - 暗号化のフラグ
  - nonce の置き場所 (目次とエントリ)
  - ヘッダのうち平文で残す範囲 (目次のオフセットとサイズ)
- 形式のバージョンは「メジャー.マイナー」にする。リーダーは、知らないメジャーバージョンを拒否する。
- **リーダー**は .NET 10 のコア側に置く。`PackageAssetSource` として、ファイル・埋め込みリソース・メモリ上のバイト列 (= 任意の `Stream`) から開ける。
  - デスクトップでは全体をメモリに載せず、seek して部分的に読む。
  - `OpenRead` は常に seek できるストリームを返す (§2.1)。無圧縮のエントリは、元の `Stream` の一部を読む seek 可能なストリームで返す。圧縮したエントリは、展開してから `MemoryStream` で返す。
  - 同時に複数のストリームを開いて読めるようにする。開いたストリームは長く開いたままになることがあり、オーディオスレッドからも読まれる (§3.4)。元の `Stream` を共有するならロックをかけて読むか、ファイルのときはエントリごとに `FileStream` を開き直す。どちらが良いかは**要スパイク**。
  - Unmount や Dispose をした後も、既に開いたストリームは、それが閉じられるまで有効にしておく。Vorbis は `Dispose` の後もしばらく別スレッドで読むことがあるため (§1)。元の `Stream` を参照カウントで持つなどの方法を、上のスパイクと一緒に決める。
- **ライター**は netstandard2.0 のライブラリにする (決定事項 10)。単体の NuGet パッケージとしては出さない (`IsPackable=false`)。MSBuild Task と一緒にコアのパッケージに同梱する。CLI を作るときに見直す。
- 完了条件: ライターで書いてリーダーで読むラウンドトリップのテスト。同時に読むテスト。形式の仕様をこの計画書の付録か、別のファイルに書いておく。

### 4.2 Phase 4: MSBuild 統合

```xml
<PrometeAsset Include="Assets/**" Package="main" Compress="auto" Obfuscate="true" />
```

**メタデータ**

- `Package`: どのパッケージにまとめるか。複数のパッケージに分けられる (将来の Asset Bundle にもつながる)。
- `Compress`: `auto` (拡張子で判定) / `true` / `false`。
- `Obfuscate`: Phase 5 で使う。それまでは無視する。
- `TargetPath`: 論理パス (決定事項 24)。省略時は Web と同じく `%(RelativeDir)%(Filename)%(Extension)` (プロジェクトからの相対パス)。デスクトップと Web で同じ `"assets/x.png"` が使える。
  - 複数のフォルダにまたがる Include も、同じ論理パスの空間に並べられる。プロジェクトの外のファイルは、`TargetPath` を明示すれば並べられる (Web Example がそうしている)。
  - パッケージとマニフェストを生成するときに、`TargetPath` を `/` 区切りにし、NFC に正規化する。
  - `%(RelativeDir)` は、`..\` で始まる Include では `..\` を含み、絶対パスの Include では絶対パスになる。そのため、`..` を含むものや絶対パスになったものはビルドエラーにして、`TargetPath` を明示するよう促す。

**モード**

- **属性とマニフェスト** (決定事項 26):
  - 属性には、モード・パッケージ名・鍵と、出力先からの相対ファイル名 (マニフェストかパッケージ。`Embed` のときは `LogicalName`) を書く。絶対パスは書かない。Web は Release でも Folder モードが既定なので、絶対パスを書くと、公開する wasm に開発機のパス (`C:\Users\<名前>\...`) が入ってしまうため。
  - マニフェストは、アセンブリ名を含む名前のファイル (例: `<AssemblyName>.promete-assets.json`) にして、出力先に置く (決定事項 31)。属性には、その相対ファイル名を書く。パッケージも同じく `<AssemblyName>.<Package>.<拡張子>` にする。どちらも `CopyToOutputDirectory` の item にして、参照元のプロジェクトの出力先にも伝播させる。これで、テストのプロジェクトからゲームの Exe を参照して `UseAssets(typeof(GameScene).Assembly)` とすれば、ゲームのアセットを読める。デスクトップでは `AppContext.BaseDirectory`、Web では MEMFS のルート。WASM での `AppContext.BaseDirectory` の値は**要確認** (`/` でなければ、Web では MEMFS のルートを直接見る)。
  - マニフェストの中身は環境ごとに違う。デスクトップでは「論理パス → 実ファイルの絶対パス」(出力先には入るが、publish には載せない。後述)。Web では「論理パス → MEMFS 上のパス」で、今の `promete-assets.json` (`{path, url, kind}` の配列) を拡張して使う。Web 形式は Promete.Web が生成し、実行時に読むのも Promete.Web (決定事項 32 / 34)。
  - コアは、属性を読んでマウントするときに「マニフェストからソースを作る」処理を差し替えられる口 (例: `IAssetManifestLoader`。internal か public かは実装時に決める) を持つ。既定の実装はデスクトップ形式を読む。Promete.Web は、`BuildWithWeb` の中で Web 形式を読む実装を DI に登録する。
  - これで、コアは Web の形式を知らずに済み、形式のバージョンも Promete.Web の中に閉じる。
  - 登録の順序: コアの既定の実装は `TryAddSingleton` で登録する。`AddSingleton` だと、単一の解決では最後に登録したものが勝つので、コアが `Build<T>` の中で既定のものを足すと Web の実装を上書きしてしまう。Promete.Web は `BuildWithWeb` の中で、`BuildWithGLBackend` を呼ぶ前に `Use<IAssetManifestLoader, …>()` で登録する (`PrometeAppBuilder` のサービスは private なので、公開 API で登録するしかない)。プロバイダが組み立てられるのは `RegisterBackend` の中で、`AssetManager` が作られるのは `OnStart` なので、§2.1 のコンストラクタでのマウントに間に合う。
  - 属性には、ビルドした環境 (`Desktop` / `Web`) も書く。Web の実装は `Desktop` の属性を受け取ったら、「このアセンブリはデスクトップ向けにビルドされているので Web では読めない。Web の実行プロジェクトで `PrometeAsset` を宣言する (§4.3)」という例外にする。Web Example の `UseScenesFrom<Promete.Example.MainScene>()` と同じ感覚で `UseAssets(typeof(MainScene).Assembly)` と書かれたときに、原因の分からない「マニフェストが無い」例外にしないため。
  - Web では、属性に書くマニフェストのファイル名を Promete.Web の props が決める (例: `PrometeWebAssetManifestName`)。コアは Web のとき、このプロパティの値を属性に書く。`promete.js` は同じ名前で HTTP で取得し、MEMFS のルートに同じ名前で書く。Web で読むマニフェストは実行プロジェクトの 1 つだけなので、名前はぶつからない (参照先のマニフェストは配信しない。§4.3)。
  - ソースジェネレータは要らない。
- **Folder** (デスクトップの Debug の既定): パッケージを作らない。`UseAssets()` はマニフェストを読んで、マニフェストに載ったファイルだけを読めるソースをマウントする。
  - 宣言していないファイルは Folder モードでも読めない。Folder モードでだけ読めてリリースで落ちる、という事故を防ぐため (決定事項 19 と同じ考え方)。
  - そのため Folder モードのソースは、§2.1 の `FolderAssetSource` (1 つのフォルダの下を読む) ではなく、マニフェストに基づくソース (`ManifestAssetSource`。名前は仮) になる。大文字・小文字の照合 (§2.2) は、マニフェストのキーに対して序数比較で行えばよく、ディレクトリの列挙は要らない。
  - 実行時にマニフェストが無い (ビルドした環境とは別の場所にコピーした) 場合は、分かりやすい例外にする。
- **Package** (Release の既定): ビルド時にパッケージを作り、既定では出力先 (実行ファイルの隣) に置く。`Embed="true"` のようなオプションで埋め込みリソースにもできる (決定事項 17)。`UseAssets()` はそれを見て `PackageAssetSource` をマウントする。
- **どちらの場合も、アプリのコードは `UseAssets()` だけで変わらない。**
- モードは `Configuration` を直接見ず、`PrometeAssetMode` (`Folder` / `Package`) のプロパティで決める。既定値だけ Configuration から決める。Debug でもパッケージの挙動を試せるようにするため。Web での既定は §4.3。

**パッケージの置き場所と publish**

- 実行時は、パッケージを `AppContext.BaseDirectory` から探す。NativeAOT や単一ファイルの publish では `Assembly.Location` が空になるため。
- パッケージは `ResolvedFileToPublish` 相当に登録して、publish の出力にも載せる。
- `Embed` のときは `LogicalName` を固定する。パックは `CoreCompile` より前に行う。
  - 属性には、ファイル名ではなく `LogicalName` を書く。パッケージは `CopyToOutputDirectory` の item にせず、伝播もさせない。参照元からは `UseAssets(typeof(X).Assembly)` で、そのアセンブリのリソースとして読める。
  - Web で `Embed` のときは、マニフェストに `kind: "package"` のエントリを載せず (プリロードしない)、埋め込みリソースから同期でマウントする。
- 属性は `AssemblyAttribute` item で書き込む。`GenerateAssemblyInfo=false` のプロジェクトでは生成されないので、ガイドに注意として書く。
- **Folder モードのマニフェストは publish の対象にしない** (`CopyToPublishDirectory=Never` 相当)。`dotnet publish -c Debug` や、Release で `PrometeAssetMode=Folder` を指定した場合に、開発機の絶対パスを含むマニフェストが publish 物に入るのを防ぐため (決定事項 26 で属性から絶対パスを外したのと同じ理由)。Folder モードで publish したら警告を出し、Package モードにするよう案内する。Web のマニフェストは絶対パスを含まないので、この規則の対象外。
- マニフェストは System.Text.Json の `Utf8JsonReader` で読む (またはソース生成の `JsonSerializerContext` を使う)。リフレクションに基づく `JsonSerializer` は使わない。NativeAOT と、既定で AOT になる Web の Release (`Promete.Web.props:30-33`) で動かすため。

**インクリメンタルビルド**

- パック処理は MSBuild Task で行い、Inputs / Outputs を宣言する。
- ファイルの日付を比べるだけでは、次の変更が検知されない。
  - item の削除、パスの変更
  - メタデータ (`Compress` / `Obfuscate` / `TargetPath`) や鍵の変更
- そこで、item の一覧・メタデータ・鍵の指紋・パックの Task のバージョン (形式のバージョン) を `WriteLinesToFile WriteOnlyWhenDifferent` で `obj/` に書き出し、それも Inputs に含める (`CoreCompileInputs.cache` と同じ手法。`Promete.Web.targets` も既に使っている)。Task のバージョンを含めるのは、Promete を更新して形式が変わったときに、`obj/` の古いパッケージを作り直させるため。
- Folder モードのマニフェストも `obj/` に `WriteOnlyWhenDifferent` で書き、そこから出力先へ `PreserveNewest` でコピーする。glob に一致するファイルが増えたり減ったりしたときに作り直されるようにするため。
- Visual Studio の高速更新チェック (fast up-to-date check) は独自の item 型を追わないので、`<UpToDateCheckInput Include="@(PrometeAsset)" />` と、`obj/` の指紋ファイルを登録する。登録しないと、VS でアセットだけを編集したときにビルドが飛ばされ、古いパッケージやマニフェストのまま動く。
- Web の Package モードでは、パックを `_PrometeDefineAssets` より前に済ませる。`_PrometeDefineAssets` は `ResolveStaticWebAssetsInputsDependsOn` にぶら下がっていて、`DefineStaticWebAssets` は登録する時点でファイルが存在することを前提にするため。
  - そのため `_PrometeDefineAssets` は、コアの「item を作るターゲット」と、Package モードのパックのターゲットを `DependsOnTargets` で指定する。これらのターゲット名は、`PrometeWebAssetsContract` の版に含まれる契約の一部として扱う (名前を変えたら契約の版を上げる)。
- 参照元へ伝播させる item (マニフェストとパッケージ) は、パスが固定 (`$(IntermediateOutputPath)$(AssemblyName)....`) なので評価時に宣言する。パックのターゲットの中で足すと、VS で `BuildProjectReferences=false` のときや、参照先を別の評価で呼んだときに、参照元の `GetCopyToOutputDirectoryItems` に載らないことがあるため。評価時に宣言できない場合は、パックのターゲットを `GetCopyToOutputDirectoryItemsDependsOn` にもぶら下げる。
- 評価時の判定に `@(PrometeAsset)` の有無を使うときは、ItemGroup や item の `Condition` に直接書く。プロパティに落とさない。MSBuild はプロパティを item より先に評価するので、評価時の `<PropertyGroup Condition="'@(PrometeAsset)' != ''">` は常に空と判定され、黙って全部無効になるため (プロジェクト本体の item は、コアの targets より先に評価されるので、item の `Condition` からは見える)。決定事項 33 の警告とエラーは、ターゲットの中で出す。

**Task の配線**

- リポジトリ内とパッケージ内で Task の DLL の場所が違う (§1)。`UsingTask AssemblyFile` を条件で分ける。
- Task のプロジェクト (netstandard2.0) は、SceneGen と同じく次のように扱う。
  - `Directory.Build.props` で `ProjectReference ReferenceOutputAssembly="false"` を足し、`UndefineProperties` で `PublishAot` を外す (NETSDK1207 を避けるため)。
  - `Directory.Build.targets` の Import の対象から外す。
  - 開発中に DLL がロックされるのを避けるため、`TaskFactory="TaskHostFactory"` を検討する。
- パッケージングでは、SceneGen と同じ形で `tasks/netstandard2.0/` などに入れる (`Promete.csproj` の SceneGen の項を参照)。Task が依存する DLL も同梱する (`Microsoft.Build.*` は除く)。`tasks/` に DLL を入れると NuGet の pack で NU5100 の警告が出るので、`NoWarn` で抑止する。
- コアの targets と Web の targets の役割分担 (決定事項 26):
  - コアの targets は `buildTransitive` にも入るので、Web のアプリやライブラリのプロジェクトにも効く。コアの targets は、どの環境でも同じ処理をする。item の解決、`TargetPath` の既定値と正規化、パック (Package モード)、属性の書き込み。
  - **`TargetPath` の既定値と正規化は、コアの targets に一本化する。** 今は `Promete.Web.targets` も自分で既定値を決めている (`Promete.Web.targets:53-58`) ので、両方に定義があるとずれる。Web の targets は、コアが決めた値を使うだけにする。
  - リポジトリ内では、`Promete.Web.targets` は csproj の末尾で Import される (`Promete.Web.Example.csproj:27`)。これは Sdk.targets (= `Directory.Build.targets` 経由のコアの targets) より前になる。そのため、**Web の targets は、コアが決めたメタデータやプロパティをターゲットの実行時にだけ参照する**。評価時の `ItemGroup` / `PropertyGroup` では参照しない。今の `Promete.Web.targets:53-59` の評価時の `Update` は、この一本化で消す。
  - 環境ごとに違う処理 (ファイルを届ける方法とマニフェストの中身) だけを分ける (決定事項 32)。
    - デスクトップ: コアの targets が、デスクトップ形式のマニフェストを生成し、パッケージとともに出力先に置く。
    - Web: コアの targets は「論理パス・ファイル・kind」の item を作るところまで。Web 形式のマニフェストの生成、静的 Web アセットへの登録、プリロードは `Promete.Web.targets` が行う。`promete.js` との契約が Promete.Web の中に閉じるので、形式を変えるときは Promete.Web だけを直せばよい。
  - 切り替えには、`Promete.Web` の props が立てるプロパティ (例: `PrometeWebAssetsContract`。対応している契約のバージョン) を使う。評価の順序に依存しないよう、props の段階で立てる。
  - **古い Promete.Web との組み合わせを防ぐ。** コアと Promete.Web は別々にバージョンが付き (今はコアが 2.3.1、Promete.Web が 2.1.0)、Promete.Web の依存は最低バージョンの指定なので、「新しいコア + 古い Promete.Web」がありうる。古い Promete.Web の props は上のプロパティを立てないので、そのままではコアが Web のプロジェクトをデスクトップとして扱ってしまう。
    - そこでコアの targets は、Web 向けのビルドなのに上のプロパティが無いか古ければ、警告を出し、Assets の処理 (属性・パック・item) を一切しない (決定事項 33)。古い `Promete.Web.targets` は自前でプリロードを完結させている (`Promete.Web.targets:53-150`) ので、これで今までどおり動く。警告のメッセージで、Promete.Web 2.4 以降への更新を案内する。
    - ただし `PrometeAssetMode=Package` を明示しているときはエラーにする。古い Promete.Web では Package モードを実現できず、黙って Folder 相当の挙動になると気付けないため。
    - 判定には `RuntimeIdentifier` を使わず、`Microsoft.NET.Sdk.WebAssembly` が立てるプロパティ (`UsingMicrosoftNETSdkWebAssembly`。名前は**要確認**) を使う。そのうえで、実行プロジェクトで `@(PrometeAsset)` が空でない場合に限る。`dotnet build -r browser-wasm` のように RID をグローバルプロパティで渡すと、ProjectReference 先のデスクトップの実行プロジェクト (Web Example から見た Promete.Example) にも RID が伝わる可能性があり (**要確認**)、RID で判定すると、そちらを誤って止めるため。
    - 逆向き (「古いコア + 新しい Promete.Web」) は、Promete.Web 2.4 の `PackageReference` で、対応するコアのバージョンを最低バージョンに指定して防ぐ。
  - パックするのは実行プロジェクト (`OutputType` が `Exe` / `WinExe`) だけ。ライブラリのプロジェクトに `PrometeAsset` があれば警告を出して無視する。ライブラリからアセットを配りたい場合は、範囲外とする (要望が出たら検討する)。Microsoft.NET.Test.Sdk はテストプロジェクトの `OutputType` を `Exe` にする可能性があり (**要確認**)、その場合テストプロジェクトも実行プロジェクトとして扱われる。テストプロジェクトに `PrometeAsset` を書くことは普通は無いので実害は小さいが、判定に `IsTestProject` も加えるかは実装時に決める。

### 4.3 既存の Web の `PrometeAsset` との関係

今の Web は、`PrometeAsset` のファイルをばらのまま MEMFS にプリロードしている。Promete.Web は公開済みなので、意味を揃えて次のようにする (決定事項 12)。

- Web 形式のマニフェストは `Promete.Web.targets` が生成する (決定事項 32)。
- **マニフェストの受け渡し**: 今の `promete.js` は、`promete-assets.json` を HTTP で取得して読むだけで、MEMFS には書いていない (`promete.js:96-113`。マニフェストが無ければ何もせず戻る)。そこで次のように拡張する。
  - `promete.js` は、取得したマニフェストを MEMFS のルートに、取得したときと同じ名前 (Promete.Web の props が決める名前。§4.2) で書き込む。C# 側はそれを読む。
  - 各エントリに、論理パス (今は `path = "/" + TargetPath` からしか分からない)、`kind: "package"` のエントリのパッケージ名を持たせる。マニフェスト全体には形式のバージョンを持たせる。
- **Folder モード**: 今の挙動のまま、ばらのファイルを MEMFS にプリロードする。`UseAssets(Assembly)` は、MEMFS 上のマニフェストを Promete.Web が差し込んだ実装で読んで (決定事項 34)、マニフェストに基づくソースをマウントする (決定事項 29)。MEMFS のルートを `FolderAssetSource` でマウントしないのは、Emscripten の `/dev`・`/proc`・`/tmp` や実行中に書いたファイルまで見えてしまい、デスクトップの「宣言していないファイルは読めない」と挙動が揃わないため。既存の `TextureFactory.Load("assets/x.png")` も今までどおり動く。`UseAssets` を呼ばないプロジェクトは何も変わらない。
- Web では、エントリアセンブリが取れないので、`UseAssets(Assembly)` で属性を持つアセンブリを明示する (決定事項 27)。
- **参照先の実行プロジェクトのアセットは、Web には届かない。** 参照先 (Web Example から見た Promete.Example) はデスクトップとしてビルドされるので、伝播してくるのはデスクトップ形式のマニフェスト (Debug) かパッケージ (Release) で、静的 Web アセットにも MEMFS へのプリロードにも載らない。Web で参照先のアセットを使うには、Web の実行プロジェクト側で `PrometeAsset` を宣言し直す (Web Example がそうしている)。`Promete.Web.targets` は、参照先から伝播してきた Promete のマニフェストとパッケージを、Web のプロジェクトの出力から外す。
- **Package モード**: 宣言されたパッケージをすべて、`promete.js` のプリロードで取得して MEMFS に書く (決定事項 25)。マニフェスト (`promete-assets.json`) に `kind: "package"` のエントリとして載せる。C# 側の `UseAssets(Assembly)` は、MEMFS 上のパッケージを `PackageAssetSource` として同期でマウントする。ばらのファイルはプリロードしない。
  - これで、プラグインの `OnStart` の中で await できない (§1) 問題を避けられる。決定事項 6 (マウントの後の読み込みは同期) も満たす。
  - パッケージの中のフォントの `FontFace` への登録は、目次を暗号化すると JS からは読めないので、C# 側でマウントするときに行う (§3.5 の案 2 を採る場合)。
- Web では、Configuration に関わらず Folder モードを既定にする (Release でも Package にならない)。これで既存のプロジェクトは何も変わらない。Package モードにするには `PrometeAssetMode` を明示する。
- `Package` / `Compress` / `Obfuscate` メタデータは、Web の Folder モードでは無視する。

### 4.4 完了条件

- `Promete.Example` を移行する。
  - `Promete.Example.csproj` の `None ... CopyToOutputDirectory` を `PrometeAsset` に置き換える。
  - アセットのパスを直接渡している箇所 (`"assets/..."` と `"./assets/..."`、約 45 行) を AssetManager に移す。
  - 同じフォルダにある `.aseprite` などは Exclude する (ガイドにもその例を書く)。
- `Promete.Web.Example` を移行する。パスを直接渡している箇所 (`AudioScene.cs`、`BenchScene.cs`、`FeatureScene.cs`) を AssetManager に移し、`Program.cs` に `UseAssets(typeof(Program).Assembly)` を足す。Package モードではばらのファイルをプリロードしないので、移行しないと Package モードで動かない。
- `Promete.Example` を Debug でも Release でも、コードを変えずに動かせる。
- Release の出力から、アセットのフォルダが消えている。
- Folder モードで、宣言していないファイルが読めないこと。マニフェストが無いときに、分かりやすい例外になること。
- item の削除・メタデータの変更で、パッケージが作り直される。
- Web Example が Folder と Package の両方のモードで動く。
  - Web Example は `UseScenesFrom<Promete.Example.MainScene>()` で Promete.Example のデモをそのまま動かしている (`Promete.Web.Example/Program.cs:42-43`)。そのため、Promete.Example のデモを Web の両モードで開けることも確認する (`ListDemos` / `OpenDemo` のスモークテスト)。Web Example の `PrometeAsset` が、デモの読むファイルを網羅していること。
- Folder モードで publish したとき、絶対パスを含むマニフェストが出力に入らず、警告が出る。
- VS からのビルドでも、アセットの変更・追加が反映される。
- 実行プロジェクトを別の実行プロジェクトから参照しても、マニフェストとパッケージの名前がぶつからない。参照元から `UseAssets(Assembly)` で参照先のアセットを読める。`Embed` の参照先からも読める。
- 新しいコアと古い Promete.Web (2.4 より前) を組み合わせて、`PrometeAsset` を持つ Web のプロジェクトをビルドすると、更新を促す警告が出て、今までどおり (MEMFS へのプリロード) 動く。`PrometeAssetMode=Package` を明示したときはエラーになる。
- `PrometeAsset` を持たない Web のプロジェクトと、デスクトップのプロジェクト (`-r browser-wasm` で参照先に RID が伝わった場合を含む) は、新しいコアでもエラーにならない。
- NativeAOT で publish しても動く (`PublishAot` は csproj に書く)。
- リポジトリ内 (`Directory.Build.targets` 経由) と、NuGet パッケージ経由の両方で動く。
- ガイドに、csproj の書き方を追記してある。

---

## 5. Phase 5: 難読化

- 対象: 目次とデータ部。目次ごと暗号化するので、ファイル名も隠れる (決定事項 8)。ヘッダのうち、目次のオフセットとサイズは平文で残す (§4.1 の予約)。
- 方式: **ChaCha20 を自前で実装する** (決定事項 20)。求めるのは、seek したまま復号できること、Web (WASM) と netstandard2.0 のライターの両方で同じ実装が動くこと、速いこと。暗号としての強さは問わない (鍵をアプリに埋め込む以上、意味がない)。
  - ChaCha20 は、64 バイトのブロックごとのカウンタで seek できる。BCL に依存しないので、Web でも netstandard2.0 でも同じコードが動く。ソフトウェアだけでも速い。
  - BCL の実装は使わない。BCL にあるのは AEAD の `ChaCha20Poly1305` だけで、素の ChaCha20 は公開されていない。`ChaCha20Poly1305` は .NET Core 3.0 以降にしかなく (netstandard2.0 / .NET Framework には無い)、OS の暗号ライブラリに依存するのでブラウザでも使えない。メッセージ全体と認証タグを一度に扱う API なので、seek もできない。
  - 実装は 1 つのソースファイルにして、netstandard2.0 のライターと .NET 10 のリーダーの両方からリンクしてコンパイルする。両者がずれないようにするため。
  - 共有するソースは、配列と `uint` の演算だけで書く。netstandard2.0 では `Span` や `BinaryPrimitives` に System.Memory パッケージが要り、.NET Framework 版の MSBuild の中でバージョンが衝突しうるため。
  - RFC 8439 のテストベクタでテストする。
  - 検討して外した候補: AES-CTR (ブラウザの .NET では AES がおそらく `PlatformNotSupportedException` になり、Web 用に別の実装が要る。CTR モードも BCL に無い)。XOR (要件は満たすが、見た目に弱い)。
- nonce はエントリごとに持たせ、目次に置く。同じ鍵と nonce の組み合わせで別の平文を暗号化すると、平文の XOR が漏れるため。
  - nonce は、パスと内容のハッシュから決める (決定事項 28)。同じ nonce になるのは、パスも内容も同じときだけなので、平文の XOR は漏れない。ランダムにすると、同じ入力でもパッケージが毎回変わり、再現可能なビルドにならない。
  - ただし、同じ入力から同じパッケージになるのは、鍵が同じ間だけ (固定の鍵を使うか、自動生成の鍵が `obj/` に残っている間)。自動生成の鍵は、`dotnet clean` や CI のたびに変わる。
  - 目次自体の nonce は、目次の内容のハッシュから決める。
  - ハッシュは netstandard2.0 にもある SHA-256 を使い、先頭の 12 バイトを nonce にする。
- 改ざんの検知はしない (ChaCha20 単体では検知できない)。破損を検知したくなったら、目次にハッシュを持たせればよい。これは将来の Asset Bundle のダウンロード検証とも共通にできる。
- `Obfuscate` メタデータで、パッケージ単位で有効にする。エントリ単位では切り替えない。
- 鍵: 自動生成と固定のどちらかを選べるようにする (決定事項 21)。どちらの場合も、鍵はアプリのアセンブリに埋め込む。形式は 32 バイトの hex 文字列。
  - **自動生成 (既定)**: 初回のビルドでランダムに生成し、`obj/` に保存して以降のビルドで使い回す。ビルドのたびに生成すると、中身が変わらなくてもパッケージが毎回変わり、インクリメンタルビルドが効かなくなるため。`dotnet clean` で作り直される。
  - **固定**: `PrometeAssetKey` のような MSBuild のプロパティで指定する。複数のプロジェクトで同じパッケージを共有したいとき (将来の Asset Bundle など) に要る。リポジトリにコミットしたくない場合は、環境変数か、コミットしない `Directory.Build.props` で渡すことを勧める。コマンドラインの `-p:` はグローバルプロパティになり、ProjectReference 先 (netstandard2.0 のプロジェクトを含む) にも伝播するので勧めない (`PublishAot` と同じ問題)。
- ドキュメントには、抑止策であって保護ではないこと、強いセキュリティが要るなら独自の `IAssetSource` を書けばよいことを明記する。
- 完了条件: 暗号化したパッケージのラウンドトリップのテスト。バイナリエディタで開いてもパスの文字列が見えないことのテスト。鍵を変えるとパッケージが作り直されること。NativeAOT と Web で動くこと。

---

## 6. 全フェーズに共通する作業

- 公開 API を足したら、同じ PR で `Promete.Docs` に反映し、`dotnet run tools/sync-docs-skill.cs` で Agent Skill も同期する。
- 新しい公開型には XML ドキュメントコメントを付ける。
- 各フェーズは `develop/<仮バージョン>` から作業ブランチを切って PR にする。Phase 1 と Phase 2 は、合わせて 1 つのマイナーリリースにまとめてもよい。

---

## 7. 範囲外のものと、それを塞がないための約束

- **CLI**: Phase 3 のライターを netstandard2.0 のライブラリにしておけば、薄いラッパーを書くだけで済む。
- **Asset Bundle (リモートから取得)**: マニフェスト、ダウンロード、ハッシュの検証、キャッシュの保存先 (デスクトップ / Android / Web の IndexedDB) が要る。本書の設計では「ダウンロードしたパッケージを `PackageAssetSource` で開いて、優先度つきでマウントする」ところまでつながるようにしておく。キャッシュの保存先はセーブデータと同じ永続ストレージの抽象で扱えそうなので、そちらと一緒に設計する。
- **Android などのモバイル**: `IAssetSource` を実装すれば足せるようにしておく。APK の中で圧縮された asset はランダムアクセスできないので、パッケージを無圧縮で入れる (`noCompress`) か、全体をメモリに読み込む必要がある。対応は他のエンジンの前例を調べてから決める。

---

## 8. フェーズの依存関係

```
Phase 1 (VFS)
  └─ Phase 2 (型付きローダー・スコープ)   … ここで開発時に使える状態になる
       └─ Phase 3 (パッケージ形式)
            └─ Phase 4 (MSBuild 統合)    … ここで issue の主な要件がそろう
                 └─ Phase 5 (難読化)
```

Phase 2 と Phase 3 は、Phase 1 さえ終われば並行して進められる。ただし、Phase 3 のリーダーへの要件 (§4.1 の同時読み出し、seek、開いたストリームの寿命) は、Phase 2 の §3.4 の決定を受けて確定させる。

§3.5 の Web のフォントの調査は、Phase 4 の Web の Package モードより前に終える。Web Example がフォントを使っているので、パッケージの中のフォントを Web で読む方法が決まらないと、§4.4 の「Web Example が Package モードで動く」を満たせない。
