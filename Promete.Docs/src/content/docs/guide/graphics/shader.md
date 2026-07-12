---
title: シェーダーとマテリアル
description: ShaderProgramとMaterialを使ってノードにカスタムシェーダーを適用する方法を解説します。
sidebar:
  order: 11
---

`ShaderProgram` と `Material` を使って、個々のノードにカスタムシェーダーを適用できます。

シェーダーの記述方法については本ドキュメントでは解説しません。別途、GLSLのリファレンスやチュートリアルを参照してください。

:::note[バックエンド依存]
使用できるシェーダー言語はバックエンドによって異なります。現在提供されている **OpenGLDesktop バックエンドでは GLSL 3.30+** が使用できます。将来的に他のバックエンド（Vulkan、Metal など）がサポートされた場合、そのバックエンド固有のシェーダー言語が必要になります。
:::

## ShaderProgram の作成

`ShaderProgram.Create()` でビルダーを生成し、頂点シェーダーとフラグメントシェーダーのソースコードを設定した後、`Compile()` でコンパイルします。

```csharp
var shader = ShaderProgram.Create()
    .Vertex(vertexGlsl)
    .Fragment(fragmentGlsl)
    .Compile();
```

:::caution
`Compile()` はシーンの `OnStart()` 以降（GLコンテキストが有効な状態）で呼び出してください。フィールド初期化子では呼び出せません。
:::

:::caution
`ShaderProgram` は `IDisposable` です。不要になったら `Dispose()` を呼んでGPUリソースを解放してください。
:::

## Material の作成

`Material` は `ShaderProgram` と uniform 変数の値をまとめたオブジェクトです。

```csharp
var material = new Material(shader);

// uniform 変数を設定（インデクサー形式）
material["uTime"] = 1.0f;
material["uColor"] = new Vector4(1f, 0f, 0f, 1f);
```

## ノードへの適用

`Node.Material` プロパティに `Material` を設定すると、そのノードの描画に適用されます。

```csharp
var sprite = new Sprite(texture);
sprite.Material = material;
Root.Add(sprite);
```

## 使用例：時間で揺れるシェーダー

```csharp
public class ShaderScene : Scene
{
    private ShaderProgram _shader;
    private Material _material;
    private Sprite _sprite;

    private const string VertexGlsl = """
        #version 330 core
        layout(location = 0) in vec2 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        out vec2 vTexCoord;
        uniform mat4 uMvp;
        uniform float uTime;
        void main()
        {
            vec2 pos = aPosition;
            pos.x += sin(pos.y * 0.05 + uTime * 3.0) * 4.0;
            gl_Position = uMvp * vec4(pos, 0.0, 1.0);
            vTexCoord = aTexCoord;
        }
        """;

    private const string FragmentGlsl = """
        #version 330 core
        in vec2 vTexCoord;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        void main()
        {
            FragColor = texture(uTexture, vTexCoord);
        }
        """;

    public override void OnStart()
    {
        var texture = App.TextureFactory.Load("assets/player.png");

        _shader = ShaderProgram.Create()
            .Vertex(VertexGlsl)
            .Fragment(FragmentGlsl)
            .Compile();

        _material = new Material(_shader);

        _sprite = new Sprite(texture).Location(100, 100);
        _sprite.Material = _material;
        Root.Add(_sprite);
    }

    public override void OnUpdate()
    {
        // 毎フレーム uniform を更新
        _material["uTime"] = Time.TotalTime;
    }

    public override void OnDestroy()
    {
        _shader.Dispose();
    }
}
```

## サンプルプログラム

Promete のサンプルプロジェクト（`Promete.Example`）には、様々なシェーダーの実装例が含まれています。

| デモ                            | 内容                                                        |
| ------------------------------- | ----------------------------------------------------------- |
| `/graphics/materialEffect.demo` | Material とカスタムシェーダーによるノードへのエフェクト適用 |
| `/graphics/postProcess.demo`    | 画面全体へのポストプロセスエフェクト                        |

サンプルを起動してデモを選択するか、`Promete.Example/examples/graphics/` 以下のソースコードを参照してください。

## 関連項目

- [ポストプロセス](/guide/graphics/postprocess) - 画面全体へのエフェクト適用
