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
- Promete.Vulkan.Validation
    - Vulkan バックエンド用のバリデーションレイヤーを有効にするプラグインです
    - NuGetにデプロイする対象です
- Promete.MeltySynth
    - Prometeで https://github.com/sinshu/meltysynth/ を利用するデモ
- Promete.Test
    - Promete用のテストプロジェクト
    - xUnit + Fluent Assertionsを使用

これら以外のブランチは開発途上であり、ドキュメント化していません。気になる場合はコードを読んでください。

## ブランチ運用

次のブランチを使います。

- `master`
    - 最新のリリース済みの状態です。直接 push や、後述以外のブランチからの PR はできません
- `develop/<仮バージョン>`（例：`develop/2.4`）
    - 次のリリースに向けた統合ブランチです。リリースのたびに作り、マージしたら捨てます
    - 通常の変更はすべてここに向けて PR を出します
- `hotfix/<バージョン>`（例：`hotfix/2.3.1`）
    - 公開済みバージョンの不具合修正（パッチバージョン）用のブランチです。使い捨てです
- それ以外（`feature/*`、`fix/*` など）
    - タスクごとの作業ブランチです。`develop/<仮バージョン>` から切って、そこに向けて PR を出します
    - 大きな機能改修も、`develop/<仮バージョン>` の下の feature ブランチで行います

ブランチ名のバージョンは識別用のラベルです。csproj の `Version` と一致していなくても構いません。また、パッケージ（Promete / Promete.ImGui / Promete.Web / Promete.Vulkan.Validation）ごとにバージョンは独立しています。

### 通常の変更

1. 最新の `develop/<仮バージョン>` から作業ブランチを切る（無ければメンテナーに相談してください）
2. 変更をコミットして、`develop/<仮バージョン>` に向けて PR を出す
3. マージされるまでレビューに対応する

### 不具合修正（hotfix）

1. `master` から `hotfix/<バージョン>` を切る
2. 修正と、対象パッケージの csproj の `Version` 更新、CHANGELOG の追記をコミットする
3. `master` に向けて PR を出す。マージされると自動でリリースされる（後述）

### master が進んだとき

`hotfix` などで `master` が進んだら、**マージした人が**、生きている `develop/*` へ `master` を取り込んでください（merge でも rebase でも構いません）。

```bash
git switch develop/2.4
git merge master
git push
```

取り込みを忘れると、`develop/*` から `master` への PR で衝突として表に出ます。

### master への PR の検査

`master` への PR には、GitHub Actions（`.github/workflows/master-pr-check.yml`）が次の検査を行います。

- **元ブランチの検査**（`Check source branch`）：`develop/*` か `hotfix/*` 以外はエラーになります
- **Version の検査**（`Check package versions`）
    - パッケージのファイルに差分があるのに csproj の `Version` が変わっていなければ、**警告**を出します（エラーにはなりません）。自動で判定できないため、公開が必要かどうかは人が判断してください
    - `Version` が変更前以下の場合や、nuget.org に公開済みのバージョンと同じ場合は、**エラー**になります

警告の判定は、パッケージごとに次のファイルの差分を見ています。

| パッケージ | 見るディレクトリ |
| --- | --- |
| Promete | `Promete/`、`Promete.SceneGen/` |
| Promete.ImGui | `Promete.ImGui/` |
| Promete.Web | `Promete.Web/` |
| Promete.Vulkan.Validation | `Promete.Vulkan.Validation/` |

- `Promete.Docs`、`Promete.Test`、`Promete.Example` などの他のプロジェクトと、`*.md` は対象外です
- `Directory.Build.props` と `Directory.Build.targets` は全パッケージに影響しうるので、変更があれば全パッケージが対象です
- `stylecop.json` は静的解析の設定だけで成果物を変えないので、対象外です

## 文書化

Promete のドキュメントは、Promete.Docs プロジェクトにて作業しています。

このプロジェクトは、Astroで作成されたドキュメントを含むnode.jsプロジェクトを、MSBuild向けに.esprojファイルを追加して管理しているものです。

masterブランチへのpushをトリガーとして、https://promete.app にデプロイされます。

### Agent Skill との同期

ガイド（`Promete.Docs/src/content/docs/guide/`）は、AI エージェント向けの Agent Skill「promete」（`plugins/promete/skills/promete/`）の references としてもコピーして配布しています。ガイドを編集したら、次のコマンドで同期してください。ページ索引（`SKILL.md` の生成部分）も更新されます。

```bash
dotnet run tools/sync-docs-skill.cs
```

同期漏れは CI で検出されます。また、`.githooks/pre-commit` により、ガイドの変更をコミットするときに自動で同期されます。この git hook は、`Promete` プロジェクトを一度ビルドすると自動で有効になります（`core.hooksPath` を既に設定している場合は変更しません）。ビルドせずに有効にする場合は、次のコマンドを実行してください。

```bash
git config core.hooksPath .githooks
```

同じフックは、ステージされた C# ファイルを CSharpier で整形してコミットに含めます。整形できない場合はコミットが中止されます。

### ブログ

ブログ記事は `Promete.Docs/src/content/blog/` に Markdown（`.md`）または MDX（`.mdx`）で置きます。このフォルダからの相対パスが URL になります（例: `2026/agent-skills.md` → `/blog/2026/agent-skills/`）。

```md
---
title: 記事のタイトル
description: 一覧に表示される概要（省略可）
date: 2026-10-06
authors: [Ebise Lutica]
draft: true
---

本文
```

`date` は公開日で、一覧は新しい順に並びます。`draft: true` の記事は開発サーバー（`pnpm dev`）でのみ表示され、公開サイトには出ません。MDX では、ドキュメントと同じく Starlight のコンポーネント（`Aside` など）を使えます。

## 継続的インテグレーション

Promete では、 GitHub Actions を用いて検査とデプロイの自動化を行っています。設定ファイルは `.github/workflows` にあります。

- `lint.yml`、`test.yml`、`web.yml`、`docs-skill.yml`：PR と、`master`、`develop/**`、`hotfix/**`、`feature/**` への push で実行されます
- `master-pr-check.yml`：`master` への PR の検査（前述）
- `upload-to-nuget.yml`：`master` への push で NuGet に公開します（後述）

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

## リリース手順

リリースはメイン開発者の @EbiseLutica が行います。従ってこの項目はフォークされたプロジェクトの管理者向けの情報となります。

リリースは `master` への push（＝ PR のマージ）をきっかけに、GitHub Actions が自動で行います。タグを手で打つ必要はありません。

### 通常のリリース

1. 最新版の変更がビルドできて、サンプルコードに不具合が発生しないことを確認する
2. `develop/<仮バージョン>` 上で、リリースするパッケージの csproj の `<Version>` を書き換える
3. CHANGELOG を更新する（後述）
4. `develop/<仮バージョン>` から `master` への PR を作る
5. PR の検査が通ったらマージする
6. 自動で、`Version` が変わったパッケージだけが NuGet に公開される。同時に、タグと GitHub Release が作られる

バージョン番号は、これまでどおり手動で決めます。

### hotfix のリリース

「[不具合修正（hotfix）](#不具合修正hotfix)」の手順で `hotfix/<バージョン>` から `master` に PR を出し、マージします。あとは通常のリリースと同じです。マージ後は、生きている `develop/*` に `master` を取り込んでください。

### リリース後に自動で行われること

`master` への push で `upload-to-nuget.yml` が動き、次のことを行います。

- push の前後（`github.event.before..sha`）で csproj の `Version` が変わったプロジェクトだけをビルドして、NuGet に push する（公開済みのバージョンは `--skip-duplicate` で無視される）
- 公開したプロジェクトのタグを打つ（既にあるタグはスキップ）
- GitHub Release を作る。ノートには、`CHANGELOG.md` の該当パッケージ・バージョンの節を使う。節が無ければ自動生成される
- 公開したプロジェクトとスキップしたプロジェクトを、ジョブのサマリに一覧する

手動で実行（`workflow_dispatch`）すると、指定したパッケージを現在の `Version` で公開できます。既定は dry-run（pack まで）です。

必要な設定は次のとおりです。

- Secrets：`NUGET_API_KEY`
- Branch Ruleset（`master`）：PR を必須にし、`Check source branch` を required status check にする

### タグの命名規則

自動で作られるタグは次の形式です。

- Promete 本体（コア）の場合は、 `core-<バージョン>` とする（例：`core-1.0.0`）
- Promete.ImGui の場合は、 `imgui-<バージョン>` とする（例：`imgui-1.0.0`）
- Promete.Web の場合は、 `web-<バージョン>` とする（例：`web-2.1.0`）
- Promete.Vulkan.Validation の場合は、 `vulkan-validation-<バージョン>` とする（例：`vulkan-validation-2.3.0`）

### 変更履歴（CHANGELOG.md）

全パッケージ共通の変更履歴を、リポジトリ直下の `CHANGELOG.md` に書きます（ドキュメントサイトの「更新履歴」にも表示されます）。パッケージごとにバージョンが独立しているため、見出しにパッケージ ID を含めます。

- 変更は `## Unreleased` の `### <パッケージ ID>`（例：`### Promete`、`### Promete.ImGui`）の下に追記しておきます
- リリースする PR（`develop/*` → `master`）の中で、リリースするパッケージの内容を `## <パッケージ ID> <バージョン>`（例：`## Promete 2.3.1`）の節に移します。マージ済みの PR や差分から内容をまとめてください
- 移すときは、見出しを一段上げます（`#### Features` → `### Features`）
- GitHub Release のノートとして、この節がそのまま使われます。同じ名前の節が無い場合は自動生成になります

```md
## Unreleased

### Promete.ImGui

#### Bug Fixes

- 〜

## Promete 2.3.1

〜の不具合を修正しました。

### Bug Fixes

- 〜
```

### AIエージェント

Claude Code用のスキル `/deploy` を使うと、リリース用の PR の準備（バージョン更新、CHANGELOG の下書き、PR 作成）を自動化できます。

`/deploy マイナーバージョンを1つあげて` `/deploy リビジョンを1つあげて` などに対応しているはずです。

## デプロイに問題が起きた場合

TBD
