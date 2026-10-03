# 技術的負債・棚卸しリスト

このセッション（2026-05-30）のAIコードレビューで発見した未対応事項。
完了したものは末尾の「対応済み」セクションを参照。

---

## 🔴 高優先度

### BA-001: `Keyboard.OnUpdate` の `Parallel.ForEach` を `foreach` に置き換える

**ファイル:** `Promete/Input/Keyboard.cs:132-158`

`OnUpdate` と `OnPostUpdate` で `~120 個の KeyCode` に対して `Parallel.ForEach` を使っているが、スレッドプール起動のオーバーヘッドが反復処理コストを上回る（逆効果）。また Silk.NET の入力 API はメインスレッド前提でスレッドセーフ保証なし。

**対応:** 両メソッドを通常の `foreach` に戻す。

---

### BA-002: `IWindow` 廃止移行パスの明確化

**ファイル:** `Promete/Windowing/IWindow.cs`, `Promete/Scene.cs`, `Promete.Example/` 各シーン

`IWindow` は `[Obsolete]` だが、サンプル（`sample5.cs`, `sample9.cs`, `container.cs` 等）が `Window.TextureFactory`, `Window.DeltaTime`, `Window.FramePerSeconds` を現役で使っている。「廃止先が何か」が不明のため初学者が困る。

**対応:**
- `Scene` に `TextureFactory` / `FramePerSeconds` / `DeltaTime` のショートカットを追加
- サンプルを `App.View.*` / `App.Time.*` / `App.TextureFactory` へ書き換え
- または v2 の間は `[Obsolete]` を外して v3 まで警告を出さない

---

### BA-003: `Keyboard.keys.cs` のスイッチ文を辞書化

**ファイル:** `Promete/Input/Keyboard.keys.cs`

130 個のキープロパティ定義 + 130 分岐の `switch` 文を手書き管理。新キー追加時や順序ミスに気づきにくい（`KeypadPlus` と `KeypadMinus` の登録順が実際に逆転している）。

**対応:** `Dictionary<KeyCode, Key>` + プロパティは `KeyOf(KeyCode.A)` のショートカット化、または Source Generator。

---

## 🟡 中優先度

### BA-004: SA1401（フィールド非 private）の根本対応

**ファイル:** `Promete/Nodes/ContainableNode.cs:18-22`, `Promete/Coroutines/Coroutine.cs:16`

現状は `#pragma warning disable SA1401` で局所抑制中。本来は:
- `ContainableNode.children / isTrimmable / sortedChildren` → `private` 化してアクセス用 protected メソッドを提供
- `Coroutine.IsKeepAlive` → プロパティ化（内部 set のみ）

---

### BA-005: `Rect.Intersect` の境界規約と座標系ドキュメント

**ファイル:** `Promete/Rect.cs`, `Promete/RectInt.cs`, `Promete/Vector.cs:213-216`

`Right = Left + Width - 1` という「ピクセル空間の末端」前提が暗黙。`Vector.In` でも `-One` を使っているが float で -1 する意味が不明瞭。`Rect.Intersect` と `RectInt.Intersect` の記述が別スタイルで統一されてない。

**対応:** `Rect` クラスの summary に座標系規約（ピクセル空間か数学空間か）を明文化。可能なら半開区間 `[Left, Right)` への変更も検討。

---

### BA-006: `MathHelper.ToRadian / ToDegree` の廃止誘導

**ファイル:** `Promete/MathHelper.cs:110-123`

`Angle` 構造体と `AngleExtension`（`45f.Degrees.ToRadians()` 等）で同等機能が提供済み。`MathHelper` の関数は重複APIとして `[Obsolete]` を付けて廃止誘導すべき。

---

### BA-007: `ContainableNode.Update` のイテレーション問題

**ファイル:** `Promete/Nodes/ContainableNode.cs:38-54`

```csharp
for (var i = 0; i < sortedChildren.Length; i++)
{
    if (children.Count <= i) break;
    children[i].Update();  // sortedChildren の順序を無視してる
}
```

`sortedChildren` でループしながら `children[i]` にアクセスしておりソート順が実際には反映されない。また `OnUpdate` 中に子が追加されるとインデックスがズレる可能性。意図的な動作なら コメントで理由を記載、バグなら `foreach (var child in sortedChildren)` に修正。

---

### BA-008: `ContainableNode.children` の `[Obsolete]` フィールド露出

**ファイル:** `Promete/Nodes/ContainableNode.cs:15-18`

`protected` フィールドに `[Obsolete("直接このフィールドは操作しないでください...")` を付けた設計は「触れるが触るな」の矛盾。`Container.cs` が毎回 `#pragma warning disable CS0618` を必要としている。`BA-004` と合わせて対応。

---

### BA-009: `AbsoluteAngle` の合成モデルのドキュメント化

**ファイル:** `Promete/Nodes/Node.cs:171`

`AbsoluteAngle = Angle + Parent.AbsoluteAngle`（単純加算）と、`UpdateModelMatrix` が行列積で正確に回転・スケール・位置を合成する実装が並存。単純加算が「親の回転に乗った子のローカル回転を考慮しているか」が不明。API の保証を `<remarks>` に明記すること。

---

### BA-010: `AudioPlayer.Stop(time)` の状態矛盾

**ファイル:** `Promete/Audio/AudioPlayer.cs:172-203`

`Stop(time > 0)` の場合、バックグラウンドタスクでフェードアウト中なのに即座に `IsPlaying = false` にしてしまう。フェード中は「再生中なのに `IsPlaying = false`」という矛盾した状態になる。フェード完了後に `IsPlaying = false` にするよう修正が必要。

---

## 🟢 低優先度

### BA-011: `IInputContext` のシングルトン化

**ファイル:** `Promete/Input/Keyboard.cs:112`, `Promete/Input/Mouse.cs:46`

`Keyboard.OnStart` と `Mouse.OnStart` がそれぞれ `inputProvider.CreateInput()` を呼び、同じバックエンドで2つの `IInputContext` を生成している可能性がある。`IInputContext` を DI でシングルトン登録するよう変更すること。

---

### BA-012: `New<T>` / `LogHelper` の未使用確認

**ファイル:** `Promete/Internal/New.cs`, `Promete/Internal/LogHelper.cs`

`New<T>` は `FormatterServices.GetUninitializedObject`（.NET 5+ で非推奨）を使用。両ファイルが現在コードベースで実際に呼ばれているか確認し、未使用なら削除、使用中なら `RuntimeHelpers.GetUninitializedObject` へ移行。

---

### BA-013: `Vector` / `Vector2` 相互変換の乱立整理

**ファイル:** `Promete/Vector.cs`, `Promete/VectorExtension.cs`, `Promete/Input/Mouse.cs`

同じ変換を `Vector.From(vec2)`, `(Vector)explicit cast`, `.ToPromete()`, `.ToNumerics()` の4方法で行えてしまう。利用側 (`Mouse.cs`) でも混在。1種に統一するか、 `Vector` ⇔ `Vector2` を implicit にして変換メソッドを全廃するか方針を決める。

---

### BA-014: CI lint の対象ブランチ設定

**ファイル:** `.github/workflows/lint.yml:5-6`

現在の lint ワークフローは `main / master / develop` ブランチのみ対象。現行の開発ブランチ `v2` が対象外のため、今回のような StyleCop 違反が CI で気づかれなかった。`v2` またはすべての PR を対象にする変更を検討。

---

### BA-015: テスト失敗: `Run(WindowOptions)` 未実装

**ファイル:** `Promete.Test/ScenelessExampleTests.cs:39-47`

`Example_ScenelessHelloWorld_ShouldHaveRequiredMethods` が `PrometeApp.Run(WindowOptions)` の存在を `Assert.Single` で期待しているが、`PrometeApp` には当該シグネチャが存在しない。

**選択肢:**
1. テストの期待を削除（現実に合わせる）
2. `PrometeApp.Run(WindowOptions opts)` を新規実装する

---

### BA-016: `Scene.Root` の `protected init` の妥当性

**ファイル:** `Promete/Scene.cs:16`

```csharp
public Container Root { get; protected init; } = new Container().Name("Root");
```

派生クラスのコンストラクタで `Root` を差し替えられるが、そのユースケースが実際に存在するか不明。なければ `private set` または readonly フィールドに変更してシグナルを明確化すること。

---

## ✅ 対応済み（このセッション）

| コミット | 内容 |
|---|---|
| `0423e18` | `Vector.Dot` / `VectorInt.Dot` の内積実装バグを修正。テスト値も誤検出を防ぐ値に変更 |
| `2ee2e5f` | `Directory.Build.props` の `<AdditionalFiles>` 誤配置修正、SA1401 #pragma 抑制、IDE0040 自動修正 |
| `4713aea` | SA1201 メンバー順序を 27 ファイルで修正（並べ替えのみ、動作変更なし）|
