# Promete Vulkan バックエンド移植計画

作成日: 2026-07-23
対象ブランチ: v2

## 1. 現状分析

### 1.1 アーキテクチャの移植適性

v2 のレンダリングアーキテクチャは、バックエンド差し替えを前提とした設計が既に完成している。

- `BackendBase` の 7 つの `Setup*` メソッドによるプロバイダー注入
- `RenderCommandQueue` + `CommandRunner<T>` によるコマンドキュー方式（ノード側は完全にバックエンド非依存）
- `HeadlessBackend` が第2バックエンドとして抽象境界の妥当性を実証済み

OpenGL 依存コードは以下の 16 ファイル・約 2,600 行に限定されている。

| 領域 | ファイル |
|------|----------|
| バックエンド本体 | `Backends/GL/OpenGLDesktopBackend.cs`, `OpenGLDesktopGameView.cs` |
| ビルド拡張・画面系 | `GLDesktop/OpenGLDesktopAppExtension.cs`, `GLScreenBlitter.cs`, `GLRenderTextureProvider.cs` |
| リソース | `Graphics/Rendering/GL/GLTextureFactory.cs`, `GLShaderFactory.cs`, `GLMaterialApplier.cs` |
| ランナー | `Graphics/Rendering/GL/Runners/` 配下 8 ファイル |
| 補助 | `GLHelper.cs`, `GLRenderState.cs`, `GLMaskedContainerHelper.cs` |

### 1.2 描画フロー（Vulkan に有利な点）

`PrometeApp.OnRender()` は全シーン描画を `GLScreenBlitter.ScreenRenderTexture`（オフスクリーン RT）にキャプチャし、ポストプロセス（ピンポンバッファ）を経て最後に画面へブリットする。

つまり **スワップチェーンイメージに触れるのは最終ブリット 1 パスのみ**。シーン描画・RenderTexture・マスク処理はすべて自前管理のオフスクリーンイメージで完結するため、スワップチェーンのフォーマット・イメージ数・リサイズ対応が最終段に隔離される。Vulkan 移植において構造的に非常に有利。

### 1.3 標準シェーダー

`Promete/Resources/shaders/` に GLSL `#version 330 core` が 13 本（texture / texture_instanced / primitive / pie / masked / stencil_mask / blit）。

## 2. 抽象の漏れ・課題一覧

| # | 課題 | 深刻度 | 対応方針 |
|---|------|--------|----------|
| 1 | `RenderCommandQueue.PushTrim()` が GL の左下原点前提で Y 反転している（バックエンド非依存層に GL 固有変換が混入） | 低 | **Phase 0 で修正**。キューは左上原点で保持し、Y 反転は GL ランナー側へ移動 |
| 2 | `Texture2D.Handle` / `ShaderProgram.Handle` が `int` 単一値。Vulkan では VkImage + VkImageView + VkSampler + ディスクリプタの複合 | 中 | バックエンド内のリソーステーブル（int ID → 実体構造体）で吸収。**公開 API 変更不要**。バッチ判定 (`Handle` 比較) もそのまま機能する |
| 3 | シェーダー言語: ユーザーは GL 方言 GLSL 330 ソースを `ShaderProgram.Vertex()/.Fragment()` に渡す。Vulkan は SPIR-V 必須で、GL 方言（ルーズ uniform）は Vulkan GLSL として無効 | 高 | docs は既に「シェーダー言語はバックエンド依存」と明記済み。Vulkan バックエンドでは **Vulkan 方言 GLSL 450 を受け付け、shaderc で実行時コンパイル**する |
| 4 | `Material` の名前ベース uniform 適用（`GLMaterialApplier` が `glGetUniformLocation` 相当を使用） | 高 | SPIR-V リフレクション（SPIRV-Cross / SPIRV-Reflect）で uniform 名 → UBO オフセット / binding を解決し、per-material UBO + ディスクリプタセットに変換 |
| 5 | `IRenderTextureProvider.BeginCapture()` の即時 FBO 切替セマンティクス（`IDisposable` スコープ、ネスト可） | 中 | 単一コマンドバッファに逐次記録する前提なら「現在のレンダーパスを終了 → イメージレイアウト遷移 → 対象 RT へのパス開始」で同じセマンティクスを再現可能 |
| 6 | GL 即時ステート（blend / scissor / stencil の随時切替） | 中 | scissor・viewport・stencil ref は dynamic state。blend・シェーダーの組はパイプラインキャッシュ（キー: シェーダーペア × blend × アタッチメントフォーマット）で対応 |
| 7 | `Promete.ImGui` が `Silk.NET.OpenGL.Extensions.ImGui` にハード依存（`OpenGLDesktopGameView` へキャスト） | 中 | 初期は Vulkan 非対応を明示（現状も例外スロー）。Phase 4 以降で ImGui Vulkan バックエンドを別途実装 |
| 8 | スクリーンショット（`glReadPixels`） | 低 | `vkCmdCopyImageToBuffer` + フェンス待ちで実装（最終ブリット元 RT から読むのが簡単） |
| 9 | インスタンスバッファへの `BufferSubData`（1 フレーム中に複数バッチが同一 VBO を上書き） | 中 | frames-in-flight 対応の per-frame リングバッファ（バッチごとにオフセットを進める）に変更 |

## 3. 技術選定

| 項目 | 選定 | 備考 |
|------|------|------|
| Vulkan バインディング | `Silk.NET.Vulkan` (+ `Extensions.KHR`) | 既存の `Silk.NET` 2.23 メタパッケージに同梱。**追加依存なし** |
| ウィンドウ/サーフェス | 既存 `Silk.NET.Windowing`（`IWindow.VkSurface`） | `GraphicsAPI.DefaultVulkan` を指定するだけ。入力 (`InputProvider`)・時間 (`SilkNetCommonTimeProvider`) はそのまま再利用可 |
| ランタイムシェーダーコンパイル | `Silk.NET.Shaderc` + native パッケージ | カスタムシェーダー（`IShaderFactory.Compile`）用。**標準シェーダー 13 本はビルド時に SPIR-V へ事前コンパイルして埋め込みリソース化**し、shaderc の実行時依存はカスタムシェーダー使用時のみに限定する案を推奨 |
| リフレクション | `Silk.NET.SPIRV.Cross` または `SPIRV.Reflect` | Material の uniform 名 → binding/offset 解決 |
| メモリ管理 | 素朴な専用アロケーション（イメージ/バッファごとに vkAllocateMemory） | 2D エンジンでリソース数は少なく、VMA 相当は初期不要。Phase 5 で必要なら導入 |
| 同期モデル | frames-in-flight = 2、シーン描画は単一グラフィックスキュー | RT キャプチャのネストは単一コマンドバッファへの逐次記録で表現 |
| 最低要求 | Vulkan 1.2 | dynamic rendering (1.3) は使わず、互換性重視で VkRenderPass ベース。macOS は MoltenVK 経由（将来検討、初期スコープ外） |

## 4. フェーズ計画

### Phase 0: 基盤整備（GL バックエンドの回帰なし）✅ 本 PR で実装

- `RenderCommandQueue.PushTrim()` の Y 反転を削除し、トリム座標を左上原点に統一
- `GLBeginTrimCommandRunner` / `GLEndTrimCommandRunner` 側でビューポート高さから Y 反転
- `TrimCommands` の XML ドキュメント修正（下端基準 → 上端基準）
- docs の誤記修正（`CommandRunner.Execute` シグネチャ等）

### Phase 1: Vulkan バックエンド骨格 ✅ 本 PR で実装

- `Backends/Vulkan/VulkanDesktopBackend` / `VulkanDesktopGameView`
- `Graphics/Rendering/Vulkan/VulkanContext`: instance（デバッグ時 validation layers）/ surface / physical device 選択 / device + queue / swapchain / レンダーパス / フレーム同期
- `VulkanDesktop/VulkanDesktopAppExtension.BuildWithVulkanDesktop()`
- 到達点: **クリアカラーの表示とリサイズ対応**。TextureFactory / ShaderFactory / RenderTextureProvider / ScreenBlitter は暫定的に Headless 実装を流用（ランナー未登録のため描画コマンドは無視される）

### Phase 2: リソース基盤 🚧 大部分実装済み

実装済み: `VulkanResourceManager` (int ID テーブル + staging アップロード + ディスクリプタ管理)、`VulkanTextureFactory`、`VulkanRenderTextureProvider` (パス中断/再開・Resize 対応)、`VulkanPipelineProvider` (パイプラインキャッシュ)、shaderc ランタイムコンパイル (`Silk.NET.Shaderc`)。標準シェーダーは Vulkan GLSL 450 版を実行時コンパイル（事前 SPIR-V 化は将来最適化）。
未実装: カスタムシェーダー (`VulkanShaderFactory` は NotSupportedException)。

元の計画（規模目安: 1,500 行）:

- `VulkanTextureFactory`: staging buffer 経由アップロード、Nearest サンプラー、int ID リソーステーブル、スプライトシート（アトラス + UV、ハーフテクセルインセットは既存の共通ロジックを再利用）
- `VulkanShaderFactory`: shaderc による GLSL 450 → SPIR-V コンパイル + リフレクション結果のキャッシュ
- パイプラインキャッシュ基盤
- `VulkanRenderTextureProvider`: オフスクリーンイメージ + レンダーパス、`BeginCapture` のパス中断/再開、`Resize`
- 標準シェーダーの Vulkan GLSL 450 版を作成し、ビルド時 SPIR-V 化（MSBuild ターゲット）

### Phase 3: ランナー移植 🚧 主要部分実装済み

実装済み: `VulkanDrawTextureBatchedCommandRunner` (インスタンシング + per-frame アリーナ)、`VulkanDrawPrimitiveCommandRunner`、`VulkanBeginTrim/EndTrimCommandRunner`、`VulkanScreenBlitter` (単純ブリット)。スクリーンショット (`TakeScreenshot`/`SaveScreenshotAsync`) も実装済みで、Promete.Experimental.Vulkan によるピクセル単位の自動検証がパスしている。
未実装: `DrawPieTextureCommand`、マスク系 (stencil/alpha)、ポストプロセスマテリアル、カスタムマテリアル、線幅 >1 の線 (wideLines)。

元の計画（規模目安: 2,000 行）:

移植順（依存が少なく検証しやすい順）:

1. `VkDrawTextureBatchedCommandRunner`（インスタンシング。per-frame リングバッファ）
2. `VkBeginTrimCommandRunner` / `VkEndTrimCommandRunner`（`vkCmdSetScissor`。Vulkan は左上原点なので Phase 0 の座標がそのまま使える）
3. `VkDrawPrimitiveCommandRunner`
4. `VkDrawPieTextureCommandRunner`
5. `VulkanScreenBlitter`（ピンポンポストプロセス + スワップチェーンへの最終ブリット）
6. マスク系（`VkBeginStencilMaskCommandRunner` / `VkBeginAlphaMaskCommandRunner` / `VkEndMaskCommandRunner` + `VulkanMaskedContainerHelper`）

各ランナーは独立性が高く、**下位モデル・サブエージェントへの委譲に適する**（GL 版という正解実装が並置されているため）。パイプライン/ディスクリプタ基盤（Phase 2）とキュー統合は委譲に不向き。

### Phase 4: 統合・仕上げ（規模目安: 800 行）

- `TakeScreenshot` / `SaveScreenshotAsync`（copy image → buffer → ImageSharp）
- スワップチェーン再構築の網羅（最小化、`Scale` 変更、フルスクリーン切替）
- `Promete.Example` 全デモの動作確認（`--vulkan` 起動フラグ等でバックエンド切替できると検証が楽）
- `Promete.ImGui` の対応方針決定（Vulkan 用 ImGui レンダラー実装 or 明示的非対応の継続）
- docs 更新: `guide/extends/backend.md` にバックエンド一覧追記、`guide/graphics/shader.md` に Vulkan 方言の説明

### Phase 5: 検証・最適化

- RenderDoc での GL / Vulkan 描画結果比較（ピクセル一致確認）
- validation layers クリーン化
- ベンチマーク（`DryRun` 計測基盤が既にあるため活用）
- 必要ならメモリアロケータ改善・ディスクリプタプール戦略見直し

## 5. リスクと判断ポイント

1. **カスタムシェーダーの後方互換**: GL 用に書かれたユーザーシェーダーは Vulkan バックエンドでそのまま動かない（言語方言差）。docs で明記済みの仕様として扱うが、`ShaderProgram` にバックエンド別ソースを与える API（例: `.Vertex(source, ShaderDialect.Vulkan)`）の追加は要検討。
2. **インスタンシング頂点レイアウトとの整合**: カスタム頂点シェーダーは実際にはインスタンシング用レイアウト（location 2〜7 にインスタンス行列等、`uProjection`）を前提とする必要があるが、docs のサンプル（`shader.md` の `aPosition`/`uMvp` 例）はこれと不整合の疑いあり。Vulkan 版の設計前に GL 側で仕様を確定させるべき。
3. **MoltenVK (macOS)**: 初期スコープ外。GL バックエンドが引き続き macOS を担う。
4. **shaderc native 配布サイズ**: 本体 NuGet に含めるか、`Promete.Vulkan` を別パッケージに分離するか。現状は GLDesktop / Headless とも本体アセンブリ内のため、まずは本体内で進め、パッケージ分離はリリース前に判断。

## 6. ドキュメントのファクトチェック結果

| ページ | 判定 | 内容 |
|--------|------|------|
| `guide/extends/backend.md` | ✅ 正確 | `BackendBase` の 7 メソッド構成・`Build<T>()` の説明は実装と一致 |
| `guide/extends/rendercommand.md` | ⚠️ 誤記 | `CommandRunner` 実装例が `protected override void Execute(MyCommand command, RenderContext ctx)` となっているが、実際のシグネチャは `public override void Execute(T command)`（`RenderContext` 引数なし）。本 PR で修正 |
| `guide/graphics/shader.md` | ⚠️ 要確認 | 「シェーダー言語はバックエンド依存」の注記は正確。ただしカスタム頂点シェーダーの例が v2 のインスタンシング頂点レイアウトと不整合の疑い（上記リスク 2） |
| ルート `CLAUDE.md` / `docs-llm.md` | ✅ 概ね正確 | バックエンド構成の記述は最新化済み |
