# Promete

[![Nuget](https://img.shields.io/nuget/vpre/Promete.svg?style=for-the-badge)](https://www.nuget.org/packages/Promete/)

Promete Engine は、.NET 8以降を対象とする、2Dグラフィックに特化したゲームエンジンです。

シンプルなAPI、高速な動作、充実した機能、高い拡張性を持ちます。

その名は、ギリシャ神話に登場する神[「プロメテウス」](https://ja.wikipedia.org/wiki/%E3%83%97%E3%83%AD%E3%83%A1%E3%83%BC%E3%83%86%E3%82%A6%E3%82%B9)に由来します。プロメテウスは、土を捏ねて人間を産み出し、火を盗んで人間に与えたとされます。<br/>
そうしたプロメテウスのように、クリエイターに力を与え、作品に命を吹き込む存在でありたいという願いが、このエンジンには込められています。

## 特徴

### シンプルなAPI

簡潔なエントリポイントからはじめる、シンプルなAPIを提供します。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<ConsoleLayer>()
    .BuildWithOpenGLDesktop();

app.Run<MainScene>();

public class MainScene(IWindow window, Keyboard keyboard)
{
    private ITexture texture1;
    private ITexture texture2;

    public override Container Setup()
    {
        texture1 = window.TextureFactory.Load("./texture1.png");
        texture2 = window.TextureFactory.Load("./texture2.png");

        return new Container
        {
            new Sprite(texture1, location: (16, 16)),
            new Sprite(texture2, location: (16, 32)),
        };
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            window.Close();
        }
    }
}
```

### クロスプラットフォーム

Windows だけでなく、macOSやLinuxでも動作します。 また、将来的にはAndroidやiOS、Webでも動作する予定です。

デフォルトの描画バックエンドはOpenGLを使用していますが、将来的にはVulkanやMetalなどのAPIをサポートし、より高速な描画を実現する予定です。

### 2Dに特化したグラフィックシステム

ピクセルパーフェクトな2Dグラフィックを気軽に実現できるゲームエンジンはそう多くありません。 Prometeは、2Dグラフィックに特化したグラフィックシステムを提供します。

Prometeでは、Elementという描画単位を用いた階層構造で画面を構成します。

Elementの一覧:

- スプライト - 画面上へのテクスチャ表示
- タイルマップ - テクスチャを敷き詰めたマップ表示
- シェイプ - シンプルな図形の描画
- コンテナー - 描画要素を格納できるオブジェクト
- テキスト - 文字列を描画できるオブジェクト
- 9スライススプライト - テクスチャを9分割して、矩形状のテクスチャ−をスムーズに引き伸ばせる特殊なスプライト

各種Elementを描画する上で、エンジンに登録された `ElementRenderer` が用いられます。このレンダラーは特に何もしなくとも標準のものが用いられますが、拡張することも可能です。

### 拡張性

Prometeは、拡張性を重視して設計されています。

標準の描画機能だけでは足りない場合、独自の `ElementRenderer` を実装することで、OpenGLを直接用いた独自のレンダリング機能をPrometeと統合できます。

また、オーディオ機能も同様に、定期的にバッファ配列にデータを書き込む `IAudioSource` の実装を作成することで、標準では足りないオーディオ形式をサポートできます。

### オーディオ機能

BGMから効果音まで、ゲームに欠かせないオーディオ機能を提供します。

BGMは特定のポイントを起点としたループ再生に対応しています（これがないエンジンも多い）

デフォルトではogg vorbisおよびwav形式をサポートしています。オーディオファイルの読み込みはプラグインによって拡張可能です。

### プラグインシステム

Prometeは、DIコンテナを用いたプラグインシステムを採用しています。

アプリ初期化時に `Use<T>` メソッドでプラグインを登録することで、その機能が使えるようになります。

キーボードやマウスの入力は、例えばPCでは必要かもしれませんが、スマートフォン等では不要な機能です。
こうした環境の差異を吸収したり、不要な機能を削減して高速化を図れるのが、Prometeのプラグインシステムです。

また、ゲーム開発時にはアセットやパラメータの管理クラスを作る必要がしばしば発生します。
プラグインシステムには任意のクラスを登録することができるので、そういった管理クラスを登録し、シーンをまたいでアクセスする書き方もできます。

## サポート プラットフォーム

| プラットフォーム | サポート状況                             |
|----------|------------------------------------|
| Windows  | テスト済み。開発者自身がWindows 11で動作を確認しています。 |
| macOS    | 動作可能。未テスト                          |
| Linux    | 動作可能。未テスト                          |
| Android  | まだ対応していません。                        |
| iOS      | まだ対応していません。                        |
| Web      | まだ対応していません。                        |

## ビルドの仕方

```shell
git clone https://github.com/EbiseLutica/Promete
cd Promete
dotnet build
```

## ドキュメント

WIP

## コントリビュート

[コントリビュートの手引き](CONTRIBUTING-ja.md) をご確認ください。

[![GitHub issues](https://img.shields.io/github/issues/ebiselutica/promete.svg?style=for-the-badge)][issues]
[![GitHub pull requests](https://img.shields.io/github/issues-pr/ebiselutica/promete.svg?style=for-the-badge)][pulls]

## ライセンス

[![License](https://img.shields.io/github/license/ebiselutica/promete.svg?style=for-the-badge)](LICENSE)

Promete はいくつかのサードパーティソフトウェアに依存しています。ライセンスをご確認ください [THIRD_PARTIES.md](THIRD_PARTIES.md)

[ci]: https://ci.appveyor.com/project/EbiseLutica/Promete
[issues]: //github.com/EbiseLutica/Promete/issues
[pulls]: //github.com/EbiseLutica/Promete/pulls
[releases]: //github.com/EbiseLutica/Promete/releases
