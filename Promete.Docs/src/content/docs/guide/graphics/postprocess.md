---
title: ポストプロセス
description: App.PostProcessMaterialsを使って画面全体にシェーダーエフェクトを適用する方法を解説します。
sidebar:
  order: 12
---

ポストプロセスは、シーン全体のレンダリング結果に対してシェーダーエフェクトを適用する仕組みです。グレースケール・ブルーム・色収差など、画面全体を加工するエフェクトに使用します。

## 基本的な使い方

`App.PostProcessMaterials` リストに `Material` を追加するだけで、そのフレームの最終出力に適用されます。

```csharp
public class PostProcessScene : Scene
{
    private ShaderProgram _shader;
    private Material _material;

    private const string VertGlsl = """
        #version 330 core
        layout(location = 0) in vec2 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        out vec2 vTexCoord;
        void main()
        {
            gl_Position = vec4(aPosition, 0.0, 1.0);
            vTexCoord = aTexCoord;
        }
        """;

    private const string FragGlsl = """
        #version 330 core
        in vec2 vTexCoord;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        void main()
        {
            vec4 color = texture(uTexture, vTexCoord);
            // グレースケール変換
            float gray = dot(color.rgb, vec3(0.299, 0.587, 0.114));
            FragColor = vec4(gray, gray, gray, color.a);
        }
        """;

    public override void OnStart()
    {
        _shader = ShaderProgram.Create()
            .Vertex(VertGlsl)
            .Fragment(FragGlsl)
            .Compile();

        _material = new Material(_shader);

        // 画面全体にエフェクトを適用
        App.PostProcessMaterials.Add(_material);
    }

    public override void OnDestroy()
    {
        App.PostProcessMaterials.Remove(_material);
        _shader.Dispose();
    }
}
```

## 複数のエフェクトをチェーン適用

複数の `Material` をリストに追加すると、追加した順番にチェーン適用されます。前のエフェクトの出力が次のエフェクトの入力になります。

```csharp
// グレースケール → ビネット の順で適用
App.PostProcessMaterials.Add(grayscaleMaterial);
App.PostProcessMaterials.Add(vignetteMaterial);
```

## エフェクトの切り替え

リストへの追加・削除でエフェクトの有効/無効を動的に切り替えられます。

```csharp
public class GameScene(Keyboard keyboard) : Scene
{
    private bool _effectEnabled = false;

    public override void OnUpdate()
    {
        if (keyboard.F.IsKeyDown)
        {
            if (_effectEnabled)
            {
                App.PostProcessMaterials.Remove(_material);
            }
            else
            {
                App.PostProcessMaterials.Add(_material);
            }
            _effectEnabled = !_effectEnabled;
        }
    }
}
```

## ポストプロセスシェーダーの書き方

ポストプロセスシェーダーでは、画面全体のテクスチャ（`uTexture`）を入力として受け取り、加工した色を出力します。

```glsl
#version 330 core
in vec2 vTexCoord;
out vec4 FragColor;
uniform sampler2D uTexture;
uniform float uIntensity;

void main()
{
    vec4 color = texture(uTexture, vTexCoord);
    // ここで色を加工
    FragColor = color;
}
```

ノード用シェーダーとは異なり、ポストプロセスシェーダーの頂点シェーダーは画面全体を覆うクワッドを描画するシンプルなものです。`uMvp` 行列は不要です。

## 関連項目

- [シェーダーとマテリアル](/guide/graphics/shader) - ノードへのシェーダー適用
