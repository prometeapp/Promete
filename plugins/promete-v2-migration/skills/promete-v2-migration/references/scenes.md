# シーン関連の変更

## SceneWillChange イベントの引数

`PrometeApp.SceneWillChange` の型が `Action` から `Action<SceneTransitionEventArgs>` に変わった。引数から遷移の種類 (`Type`) と、遷移前後のシーン (`Previous` / `Next`) を取得できる。

```csharp
// v1
App.SceneWillChange += () => Console.WriteLine("シーンが変わります");

// v2
App.SceneWillChange += e => Console.WriteLine($"{e.Previous} → {e.Next}");
```

## シーンの自動登録

シーンの自動登録が、実行時のリフレクションからコンパイル時のソースジェネレータ (`Promete.SceneGen`) に変わった。次のシーンは自動登録されない。

- `private` / `protected` なネスト型や `file` ローカル型のシーン。`PROMETE0001` の警告が出る。`internal` 以上の可視性にする
- 抽象クラス・ジェネリック型のシーン
- public なコンストラクタがないシーン (`PROMETE0002` の警告が出る)

シーンが登録されていないと、`LoadScene<T>()` などで実行時に失敗する。ビルドが通った後も `PROMETE0001` / `PROMETE0002` が出ていないか確認する。

## Promete を ProjectReference で参照している場合

NuGet パッケージではなく `ProjectReference` で参照している場合 (サブモジュールとして取り込んでいる場合など)、ソースジェネレータが自動では適用されず、シーンが 1 つも登録されない。プロジェクトファイルに次を追加する。パスは実際の配置に合わせる。

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Promete.SceneGen/Promete.SceneGen.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <CompilerVisibleProperty Include="OutputType" />
</ItemGroup>
```

あわせて次の点を確認する。

- シーンを含むライブラリプロジェクトがある場合は、そのプロジェクトにも同じ参照を追加する。参照がないと、そのライブラリ内のシーンは登録されない
- `-r` を付けた publish や NativeAOT を使う場合は、`ProjectReference` に `UndefineProperties="TargetFramework;RuntimeIdentifier;SelfContained;PublishAot;PublishTrimmed;PublishSingleFile"` を付ける。netstandard2.0 の `Promete.SceneGen` にこれらのプロパティが伝播するのを防ぐため。Promete リポジトリ自身も `Directory.Build.props` で同じ指定をしている
- `PublishAot` は `-p:PublishAot=true` で渡さず、csproj に書く。コマンドラインで渡すとグローバルプロパティになって `Promete.SceneGen` に伝播し、`NETSDK1207` になる

## エントリアセンブリ以外にシーンがある場合

`UseScenesFrom` でそのアセンブリを指定する。型引数には、そのアセンブリ内の任意の型を渡す。

```csharp
var app = PrometeApp.Create()
    .UseScenesFrom<SomeTypeInContentAssembly>()
    .BuildWithOpenGLDesktop();
```

コンパイル時に参照していないアセンブリ (実行時に読み込むプラグインなど) のシーンは、原理的に列挙できない。

v1 もエントリアセンブリのシーンしか自動登録しなかった。そのため v1 では、別アセンブリのシーンを `Use<MyScene>()` などで手動登録していることがある。`Use<T>()` はシングルトン登録なので、シーンの「毎回新しいインスタンスを作る」前提と食い違う。こうした手動登録は削除し、`UseScenesFrom` に置き換える。
