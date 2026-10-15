---
name: deploy
description: Promete のリリース PR を準備する。バージョン更新・CHANGELOG の下書き・develop/* から master への PR 作成までを CONTRIBUTING.md 準拠の手順で行う。タグ付けと NuGet 公開は master へのマージ後に GitHub Actions が自動で行う。ユーザーが「デプロイして」「リリースして」「バージョンを上げて公開」「リリース PR を作って」などと言った場合、必ずこのスキルを使用すること。
metadata:
  internal: true
---

# Promete リリース

新しいブランチ運用では、リリースは `master` への push（PR のマージ）を起点に GitHub Actions が行う。
**このスキルでタグは打たない。`master` へ直接 push もしない。** リリース PR を用意するところまでが役目。

ブランチ運用の詳細は CONTRIBUTING.md の「ブランチ運用」「リリース手順」を参照すること。

## 引数

`<パッケージ> <バージョン>` の形で受け取る（例: `core 2.4.0`）。パッケージは次のいずれか。

| 接頭辞 | csproj |
| --- | --- |
| `core` | `Promete/Promete.csproj` |
| `imgui` | `Promete.ImGui/Promete.ImGui.csproj` |
| `web` | `Promete.Web/Promete.Web.csproj` |
| `vulkan-validation` | `Promete.Vulkan.Validation/Promete.Vulkan.Validation.csproj` |

一覧は `.github/scripts/packages.tsv` にもある。複数パッケージを同時に上げてもよい。
引数が足りなければユーザーに尋ねる。「マイナーを1つ上げて」のような指定なら、現在の `<Version>` から計算してユーザーに確認する。バージョン番号は必ずユーザーが決める。

## 前提確認

現在のブランチ名と直近10件のコミットログを表示し、ユーザーに確認を求める。

リリース PR の元になるブランチは次のどちらか。それ以外のブランチだった場合は、中止してユーザーに知らせる。

- 通常のリリース: `develop/<仮バージョン>`
- hotfix: `hotfix/<バージョン>`

## 手順

以下、`<version>` は引数のバージョン、`<csproj>` は対象パッケージの csproj を指す。

### 1. master の取り込み確認

`master` が進んでいるのに取り込んでいなければ、リリース PR が衝突する。事前に確認する。

```bash
git fetch origin
git log --oneline HEAD..origin/master
```

出力があれば、`origin/master` を取り込むかユーザーに確認する（マージ方式はどちらでもよい）。

### 2. ビルド確認

```bash
dotnet build Promete.slnx
```

失敗した場合は中止して、ユーザーに報告する。

### 3. バージョンを更新

`<csproj>` の `<Version>` を `<version>` に書き換える。

次を確認する。
- `<version>` が現在の値より大きい
- nuget.org に公開済みのバージョンと重複していない（`https://api.nuget.org/v3-flatcontainer/<小文字のパッケージID>/index.json`）。重複していたら PR の検査でエラーになる

### 4. CHANGELOG を更新

対象パッケージの `CHANGELOG.md`（`core` なら `Promete/CHANGELOG.md`）を更新する。無いパッケージは、リリースノートが自動生成になるので、無理に作らなくてよい。

- `## Unreleased` の中身を、`## <version>` に書き換える。`Unreleased` が空なら、下書きを作る
- 下書きは、前回リリース以降のマージ済み PR とコミットから作る。前回のタグは `git tag --list '<接頭辞>-*' --sort=-v:refname | head -1` で得られる

```bash
git log <前回のタグ>..HEAD --oneline --no-merges -- <パッケージのディレクトリ>
gh pr list --state merged --base develop/<仮バージョン> --limit 100
```

- 既存の書式（冒頭の要約文、`### Features` / `### Enhancements` / `### Bug Fixes`、箇条書き）に合わせ、日本語で書く
- 下書きはユーザーに見せて確認を取る。見出し `## <version>` の節が GitHub Release のノートにそのまま使われる
- 空になった `## Unreleased` は先頭に残しておく

### 5. コミット

関心事ごとに分けてコミットする。

```bash
git add <csproj>
git commit -m "chore: <パッケージ名> のバージョンを <version> に更新"
git add <CHANGELOG>
git commit -m "docs(CHANGELOG): <version> の CHANGELOG を追加する"
```

### 6. push してリリース PR を作る

push と PR 作成の前に、コミット内容を表示してユーザーに確認を取る。OK であれば実行する。

```bash
git push -u origin HEAD
gh pr create --base master --title "<パッケージ名> <version> をリリースする" --body-file <本文ファイル>
```

PR のタイトルと本文は日本語で書く。本文には、リリースするパッケージとバージョン、CHANGELOG の該当節、`master` を取り込み済みであることを載せる。

### 7. 完了報告

PR の URL を報告し、次のことを伝える。

- PR の検査（`Check source branch` / `Check package versions`）が通ってからマージする
- マージ後は GitHub Actions（`upload-to-nuget.yml`）が、NuGet への公開、タグ `<接頭辞>-<version>`、GitHub Release を自動で作る。ジョブのサマリで公開結果を確認できる
- hotfix をマージしたら、生きている `develop/*` に `master` を取り込む必要がある
