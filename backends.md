# 描画バックエンド

Prometeは、ユーザーがDirectXやOpenGLなどのグラフィクスAPIを意識せずにゲームプログラミングをできるようにAPIを整備しています。

APIによる実際のアプリケーション描画は、描画バックエンドが担当します。描画バックエンドは、Promete APIを実装し、適宜OpenGLなどのグラフィクスAPIを呼び出します。

## 描画バックエンドの実装
描画バックエンドの実装は次の手順で行います。

### IWindow の実装
IWindow は、Prometeのウィンドウを表すインターフェイスです。ウィンドウとしての属性から、描画フレームのイベントの発行など、かなり多くの役割を持ちます。

慣例的に、Promete.Windowing名前空間にバックエンド名を追加したものの中に配置します。例えば、OpenGL デスクトップアプリ用描画バックエンドは、 `Promete.Windowing.GLDesktop` に実装しています。

### Texture Factoryの実装
画像などからテクスチャを生成するための仕組みはグラフィクスAPIに大きく依存するため、Prometeでは「Texture Factory」として抽象化しています。
IWindow 実装から呼び出すために、上で作成した名前空間の中に、 TextureFactory 抽象クラスを継承する形で作成します。

### Node Rendererの実装
Prometeでは、Node という単位で、ゲーム画面の構成要素（スプライト、テキスト、図形など）を管理します。
Node については[README.md](README.md)を参照してください。

Node は、描画バックエンドに対して、描画のための情報を提供しますが、描画ロジックは持ちません。実際のフレームバッファへの描画には、Node Rendererを実装する必要があります。

Node Rendererは、Nodeの描画情報を受け取り、描画バックエンドに対して描画を行います。Node Rendererは、Promete.Nodes.Renderer 名前空間に実装します。例えば、OpenGL用描画バックエンドでは、 `Promete.Nodes.Renderer.GL` に実装しています。

### ヘルパークラスの実装
ユーザーの利便性のために、ヘルパークラスを実装します。

GLであれば `Promete.GLDesktop.OpenGLDesktopAppExtension` のように、描画バックエンド名を追加した名前空間に実装します。

`Promete.App.BuildWithXxx()` というように、描画バックエンドの初期化に必要な処理をワンライナーで行えるようにします。

OpenGLDesktopAppExtension の例です。

```cs
using Promete.Nodes;
using Promete.Nodes.Renderer.GL;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing.GLDesktop;

namespace Promete.GLDesktop;

public static class OpenGLDesktopAppExtension
{
    public static PrometeApp BuildWithOpenGLDesktop(this PrometeApp.PrometeAppBuilder builder)
    {
        return builder
            .UseRenderer<ContainableNode, GLContainbleNodeRenderer>()
            .UseRenderer<Container, GLContainbleNodeRenderer>()
            .UseRenderer<NineSliceSprite, GLNineSliceSpriteRenderer>()
            .UseRenderer<Shape, GLShapeRenderer>()
            .UseRenderer<Sprite, GLSpriteRenderer>()
            .UseRenderer<Text, GLTextRenderer>()
            .UseRenderer<Tilemap, GLTilemapRenderer>()
            .Use<GLTextureRendererHelper>()
            .Use<GLPrimitiveRendererHelper>()
            .Build<OpenGLDesktopWindow>();
    }
}
```

`UseRenderer` は、Node Rendererを登録します。Node Rendererは、描画バックエンドに依存するため、描画バックエンドの初期化時に登録します。もし必要であれば、 `Use` を用いて、独自のクラスを登録してください。GLの例では、レンダリング系をまとめるためにヘルパークラスを登録しています。

-----

これで描画バックエンドの実装が完了します。

OpenGLの実装例が参考になるので、参考にしつつ開発してください。
