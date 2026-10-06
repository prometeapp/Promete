# コントリビューションの手引き

**コントリビュートにご協力いただきありがとうございます！その前に、このガイドをお読みいただければと思います。**

## 貢献者へ

あなたがこのプロジェクトに提供したソースコードには、他のコードと同様に LICENSE ファイルに記載されるライセンスが付与されることに同意してください。

## Issue

新機能の要望やバグ報告などは [GitHub Issues](https://github.com/EbiseLutica/Promete/Issues) にてお願いします。 Issue を作成する前に、重複を避けるため既に存在している同じような Issue が存在しないかどうか検索をしてください。もし存在するならば、リアクションやコメントを用いて upvote してください。

## 各プロジェクト

本リポジトリは、MSBuildのソリューションを用いており、複数のプロジェクトを持ちます。

- Promete
    - Prometeのメインプロジェクトです
    - ゲームエンジンとしての基本機能は全てこのプロジェクトにあります
    - NuGetにデプロイする対象です
- Promete.Docs
    - ドキュメント（後述）
- Promete.Example
    - Prometeの機能を試せるデモ プロジェクトです。
- Promete.ImGui
    - PrometeでImGUIを利用できるプラグインのプロジェクトです
    - NuGetにデプロイする対象です
- Promete.MeltySynth
    - Prometeで https://github.com/sinshu/meltysynth/ を利用するデモ
- Promete.Test
    - Promete用のテストプロジェクト
    - xUnit + Fluent Assertionsを使用

これら以外のブランチは開発途上であり、ドキュメント化していません。気になる場合はコードを読んでください。

## 文書化

Promete のドキュメントは、Promete.Docs プロジェクトにて作業しています。

このプロジェクトは、Astroで作成されたドキュメントを含むnode.jsプロジェクトを、MSBuild向けに.esprojファイルを追加して管理しているものです。

masterブランチへのpushをトリガーとして、https://promete.app にデプロイされます。

### Agent Skill との同期

ガイド（`Promete.Docs/src/content/docs/guide/`）は、AI エージェント向けの Agent Skill「promete」（`plugins/promete/skills/promete/`）の references としてもコピーして配布しています。ガイドを編集したら、次のコマンドで同期してください。ページ索引（`SKILL.md` の生成部分）も更新されます。

```bash
dotnet run tools/sync-docs-skill.cs
```

同期漏れは CI で検出されます。次のコマンドで git hook を有効にしておくと、ガイドの変更をコミットするときに自動で同期されます。

```bash
git config core.hooksPath .githooks
```

## 継続的インテグレーション

Promete では、 GitHub Actions を用いてデプロイの自動化を行っています。設定ファイルは `.github/workflow` にあります。

## コーディング規則

コードフォーマットは [CSharpier](https://csharpier.com/) で管理しています。手動でスタイルを調整する必要はありません。

```bash
dotnet tool restore
dotnet csharpier format .
```

## 設計上の規則

### 公開APIをSilk.NET等の外部ライブラリに依存させないこと

Prometeでは、Silk.NET等のバックエンドに依存しないようAPIを設計しています。

新しくAPIを追加する場合、引数や戻り値の型として、.NET 標準ライブラリおよびPrometeが提供する型のみを使用するようにし、バックエンドが提供する型は基本的に使用しないでください。<br/>
ただ、内部的・プラグイン向けと明記している場合や、private、internalなメンバーの場合は使用しても良いです。

### Silk.NET はフォークを使っています

Promete が参照する Silk.NET は net10.0 専用のフォーク (`prometeapp/Silk.NET`) です。
パッケージ ID は `Promete.Silk.*` ですが、アセンブリ名と名前空間は `Silk.NET.*` のままなので、
コード上の `using` は upstream と同じです。

バージョンを上げる場合はフォーク側でタグを打って publish してから、
`Promete/Promete.csproj` と `Promete.ImGui/Promete.ImGui.csproj` の参照を更新してください。

### Promete.SceneGen は netstandard2.0 を維持すること

ソースジェネレータは Roslyn がコンパイラプロセスにロードするため、
`Promete.SceneGen` のターゲットフレームワークは `netstandard2.0` 固定です。
`record` などが要求する型は `IsExternalInit.cs` で補っています。

生成コードは利用者のコンパイルで動きます。配布はパッケージの
`analyzers/dotnet/cs` 経由で、設定は `build/Promete.props` の
`CompilerVisibleProperty` で渡しています。

このリポジトリ内では、ジェネレータは `Directory.Build.props` の
`ProjectReference` で全 C# プロジェクトに配っています。そのため
`PublishAot` や `PublishTrimmed` をコマンドラインで渡すと、グローバル
プロパティとして `Promete.SceneGen` にも波及し `NETSDK1207` で失敗します。

```
# NG
dotnet publish Promete.Example -p:PublishAot=true
```

csproj の `PropertyGroup` に書いてください。利用者にはパッケージ経由で
届くので、この制約はリポジトリ内のビルドだけの話です。

### .NET のアップデート PR を作成しないでください

.NET のアップデートは、[SUPPORT.md](SUPPORT.md) に従ってメンテナーが行います。
そのため、.NET のアップデートに関する PR は作成しないでください。

## デプロイ手順

デプロイはメイン開発者の @EbiseLutica が行います。従ってこの項目はフォークされたプロジェクトの管理者向けの情報となります。

1. 最新版の変更がビルドできて、サンプルコードに不具合が発生しないことを確認する
2. master に最新版をコミットする
3. Promete/Promete.csproj 内のバージョン表記を書き換える
4. 上記の変更をコミットする
5. タグを作成する（タグの命名規則は下記）
6. pushする
7. :pray:

### タグの命名規則

- Promete 本体（コア）の場合は、 `core-<バージョン>` とする（例：`core-1.0.0`）
- Promete.ImGui の場合は、 `imgui-<バージョン>` とする（例：`imgui-1.0.0`）

### AIエージェント

Claude Code用のスキル `/deploy` を使うことでデプロイを自動化できます。

`/deploy マイナーバージョンを1つあげて` `/deploy リビジョンを1つあげて` などに対応しているはずです。

## デプロイに問題が起きた場合

TBD
