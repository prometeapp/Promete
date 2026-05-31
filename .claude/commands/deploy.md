引数 `$ARGUMENTS` をデプロイするバージョンとして使用する。引数がない場合はユーザーにバージョンを尋ねること。

以下の手順でデプロイ作業を行う。

## 前提確認

まず現在のブランチ名と直近10件のコミットログを表示し、ユーザーに確認を求める。
確認が取れたら以下の手順を実行する。

## デプロイ手順（CONTRIBUTING.md 準拠）

**バージョン:** `$ARGUMENTS`

### 1. ビルド確認
```bash
dotnet build Promete/Promete.csproj
```
ビルドが失敗した場合はデプロイを中止してユーザーに報告する。

### 2. Promete.csproj のバージョンを更新
`Promete/Promete.csproj` 内の `<Version>` タグを `$ARGUMENTS` に書き換える。

### 3. バージョン更新をコミット
```bash
git add Promete/Promete.csproj
git commit -m "chore: バージョンを $ARGUMENTS に更新"
```

### 4. タグを作成
CONTRIBUTING.md のタグ命名規則に従い `core-$ARGUMENTS` というタグを作成する。
```bash
git tag core-$ARGUMENTS
```

### 5. push する前にユーザーに確認
コミットとタグの内容を表示してから、push してよいか確認を取る。
OKであれば以下を実行する。
```bash
git push origin HEAD
git push origin core-$ARGUMENTS
```

### 6. 完了報告
push 完了後、タグ名と対象コミットを報告する。
