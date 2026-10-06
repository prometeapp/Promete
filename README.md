# <img height="40" src="https://raw.githubusercontent.com/prometeapp/Promete/master/assets/logo.png" /> [Promete](https://promete.app)

[![Nuget](https://img.shields.io/nuget/vpre/Promete.svg?style=for-the-badge)](https://www.nuget.org/packages/Promete/)

Promete は、.NET 10以降を対象とするゲーム開発フレームワーク、ゲームエンジンです。

2Dグラフィクスに特化したシンプルなAPI、高速な動作、充実した機能、高い拡張性を持ちます。

その名は、ギリシャ神話に登場する神[「プロメテウス」](https://ja.wikipedia.org/wiki/%E3%83%97%E3%83%AD%E3%83%A1%E3%83%BC%E3%83%86%E3%82%A6%E3%82%B9)に由来します。プロメテウスは、土を捏ねて人間を産み出し、火を盗んで人間に与えたとされます。<br/>
そうしたプロメテウスのように、クリエイターに力を与え、作品に命を吹き込む存在でありたいという願いが、このエンジンには込められています。

ドキュメントは https://promete.app にて公開中です。

> [!NOTE]
> Promete v1 から移行する場合は、[v1 → v2 移行ガイド](https://promete.app/migration-v2/) をご確認ください。

## 特徴
### シンプルなAPI
簡潔なエントリポイントからはじめる、シンプルなAPIを提供します。

```csharp
// アプリケーションの初期化
var app = PrometeApp.Create()
	.Use<Keyboard>()
	.Use<ConsoleLayer>()
	.BuildWithOpenGLDesktop();

return app.Run<MainScene>();

// ゲームシーンの定義
public class MainScene : Scene
{
	private readonly Keyboard _keyboard;
	private readonly Texture2D _texture1;
	private readonly Texture2D _texture2;

	// 登録したプラグインはコンストラクタで受け取れる
	public MainScene(Keyboard keyboard)
	{
		_keyboard = keyboard;
		_texture1 = App.TextureFactory.Load("./texture1.png");
		_texture2 = App.TextureFactory.Load("./texture2.png");

		Root = [
			new Sprite(_texture1).Location(16, 16),
			new Sprite(_texture2).Location(16, 32),
		];
	}

	public override void OnUpdate()
	{
		if (_keyboard.Escape.IsKeyDown)
		{
			App.Exit();
		}
	}

	public override void OnDestroy()
	{
		_texture1.Dispose();
		_texture2.Dispose();
	}
}
```

シーンはソースジェネレータによってコンパイル時に自動登録されるため、実行時のリフレクションに依存しません。

### クロスプラットフォーム
Windows だけでなく、macOSやLinuxでも動作します。トリミングや NativeAOT でのビルドにも対応しています。

### 2Dに特化したグラフィックシステム
3Dに対応したゲームエンジンは多いですが、ピクセルパーフェクトな2Dグラフィックを気軽に実現できるゲームエンジンはそう多くありません。Prometeは、2Dグラフィックに特化したグラフィックシステムを提供します。

Prometeでは、ノードという描画単位を用いた階層構造で画面を構成します。

ノードの一覧:

- Sprite - 画面上へのテクスチャ表示
- Tilemap - テクスチャを敷き詰めたマップ表示
- Shape - シンプルな図形の描画
- Container - 描画要素を格納できるオブジェクト
- MaskedContainer - 子ノードをマスクで切り抜いて描画できるコンテナー
- Text - 文字列を描画できるオブジェクト。禁則処理・カーニング・リッチテキストに対応
- NineSliceSprite - テクスチャを9分割して、矩形状のテクスチャ−をスムーズに引き伸ばせる特殊なスプライト
- PieSprite - テクスチャを扇形に切り抜いて表示するスプライト

描画はコマンドキュー方式で行われます。各ノードが描画コマンドを発行し、同じテクスチャ・同じマテリアルが連続する描画は自動的にまとめられ、インスタンシング描画されます。

また、シェーダーを用いたマテリアルや、画面全体に適用するポストプロセスにも対応しています。

### 拡張性
Prometeは、拡張性を重視して設計されています。

標準のノードだけでは足りない場合、ノードの `Collect()` メソッドをオーバーライドして描画コマンドを発行することで、独自のノードを作成できます。標準のコマンドで表現できない描画も、独自の描画コマンドとランナーを追加することでPrometeと統合できます。

オーディオ機能も同様に、バッファにデータを書き込む `IAudioSource` の実装を作成することで、標準では足りないオーディオ形式をサポートできます。

フォントについても、`IGlyphSource` を実装することで独自のフォント形式を扱えます。

### オーディオ機能
BGMから効果音まで、ゲームに欠かせないオーディオ機能を提供します。

BGMは特定のポイントを起点としたイントロ付きループ再生に標準対応。ローパスやディレイなどのDSPフィルターを適用することもできます。

デフォルトではogg vorbisおよびwav形式をサポートしています。オーディオファイルの読み込みはプラグインによって拡張可能です。

### プラグインシステム
Prometeは、DIコンテナを用いたプラグインシステムを採用しています。

アプリ初期化時に `Use<T>` メソッドでプラグインを登録することで、その機能が使えるようになります。

キーボードやマウスの入力は、例えばPCでは必要かもしれませんが、スマートフォン等では不要な機能です。
こうした環境の差異を吸収したり、不要な機能を削減して高速化を図れるのが、Prometeのプラグインシステムです。

また、ゲーム開発時にはアセットやパラメータの管理クラスを作る必要がしばしば発生します。
プラグインシステムには任意のクラスを登録することができるので、そういった管理クラスを登録し、シーンをまたいでアクセスする書き方もできます。

### バックエンド
Prometeは、描画やウィンドウ管理を担うバックエンドを差し替えられる設計になっています。

| バックエンド | 初期化メソッド | 用途 |
|----------|----------|------|
| OpenGL | `BuildWithOpenGLDesktop()` | 標準のバックエンド。製品向け |
| Vulkan（実験的） | `BuildWithVulkanDesktop()` | Vulkan 1.2 を用いた描画。動作が不安定な場合があります |
| ヘッドレス | `BuildWithHeadless()` | グラフィックスを持たないバックエンド。テストなどに |

## サポート プラットフォーム
| プラットフォーム | サポート状況								|
|----------|---------------------------------------|
| Windows  | テスト済み。開発者自身がWindows 11で動作を確認しています。	|
| macOS	| テスト済み。開発者自身がmacOS Golden Gateで動作を確認しています。 |
| Linux	| おそらく動作可能。開発者はWSL2を用いてUbuntu環境で動作を確認しています。 |
| Android  | まだ対応していません。						   |
| iOS	  | まだ対応していません。						   |
| Web	  | まだ対応していません。						   |

## ビルド方法
ビルドには .NET 10 SDK が必要です。

```shell
git clone https://github.com/prometeapp/Promete
cd Promete
dotnet build
```

## コントリビュート
[コントリビューションの手引き](CONTRIBUTING.md) をご確認ください。

[![GitHub issues](https://img.shields.io/github/issues/prometeapp/promete.svg?style=for-the-badge)][issues]
[![GitHub pull requests](https://img.shields.io/github/issues-pr/prometeapp/promete.svg?style=for-the-badge)][pulls]

## サポート中のバージョンとポリシー
[サポート状況とポリシー](SUPPORT.md) をご確認ください。

## LLM向けの情報
AIエージェントを用いて v1 のプロジェクトを v2 へ移行する場合は、[LLM向けの移行ガイド](https://promete.app/assets/migration-v2-llm.md) を利用できます。

## ライセンス
[![License](https://img.shields.io/github/license/prometeapp/promete.svg?style=for-the-badge)](LICENSE)

Promete はいくつかのサードパーティソフトウェアに依存しています。ライセンスをご確認ください [THIRD_PARTIES.md](THIRD_PARTIES.md)

[issues]: //github.com/prometeapp/Promete/issues
[pulls]: //github.com/prometeapp/Promete/pulls
[releases]: //github.com/prometeapp/Promete/releases
