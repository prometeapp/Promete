using System.Drawing;
using System.Numerics;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

/// <summary>
/// Material とカスタムシェーダーによる画像エフェクトのデモ。
/// セピア・グレースケール・モザイク・縁取り（白1px）・ラスタースクロールの5種類を表示します。
/// </summary>
[Demo("/graphics/materialEffect.demo", "Materialとカスタムシェーダーによる画像エフェクトの例")]
public class MaterialEffectDemo(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private Texture2D _texture;
    private ShaderProgram _sepiaShader = null!;
    private ShaderProgram _grayscaleShader = null!;
    private ShaderProgram _mosaicShader = null!;
    private ShaderProgram _outlineShader = null!;
    private ShaderProgram _rasterScrollShader = null!;
    private Material _rasterScrollMat = null!;

    // バッチドレンダラーのVAOレイアウトに合わせた頂点シェーダー
    private const string VertSrc = """
        #version 330 core
        layout(location = 0) in vec2 vPos;
        layout(location = 1) in vec2 vUv;
        layout(location = 2) in vec4 iModel0;
        layout(location = 3) in vec4 iModel1;
        layout(location = 4) in vec4 iModel2;
        layout(location = 5) in vec4 iModel3;
        layout(location = 6) in vec4 iTintColor;
        layout(location = 7) in vec4 iUvRect;

        out vec2 fUv;
        out vec4 fTintColor;

        uniform mat4 uProjection;

        void main()
        {
            mat4 model = mat4(iModel0, iModel1, iModel2, iModel3);
            gl_Position = uProjection * model * vec4(vPos, 0.0, 1.0);
            fUv = mix(iUvRect.xy, iUvRect.zw, vUv);
            fTintColor = iTintColor;
        }
        """;

    // セピア変換
    private const string SepiaFragSrc = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uTexture0, fUv) * fTintColor;
            float r = dot(c.rgb, vec3(0.393, 0.769, 0.189));
            float g = dot(c.rgb, vec3(0.349, 0.686, 0.168));
            float b = dot(c.rgb, vec3(0.272, 0.534, 0.131));
            FragColor = vec4(r, g, b, c.a);
        }
        """;

    // グレースケール変換（輝度係数: BT.601）
    private const string GrayscaleFragSrc = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uTexture0, fUv) * fTintColor;
            float gray = dot(c.rgb, vec3(0.299, 0.587, 0.114));
            FragColor = vec4(gray, gray, gray, c.a);
        }
        """;

    // モザイク（uBlockSize px 単位でスナップ）
    private const string MosaicFragSrc = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        uniform vec2 uTextureSize;
        uniform float uBlockSize;
        out vec4 FragColor;

        void main()
        {
            vec2 pixel   = fUv * uTextureSize;
            vec2 snapped = floor(pixel / uBlockSize) * uBlockSize + uBlockSize * 0.5;
            vec2 uv      = snapped / uTextureSize;
            FragColor = texture(uTexture0, uv) * fTintColor;
        }
        """;

    // ラスタースクロール: 走査線ごとに sin でX方向オフセット → うにょうにょ
    private const string RasterScrollFragSrc = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        uniform float uTime;
        out vec4 FragColor;

        void main()
        {
            // 走査線（Y座標）ごとにサイン波でX方向へオフセット
            float offset = sin(fUv.y * 18.0 + uTime * 4.0) * 0.06;
            vec2 uv = vec2(fUv.x + offset, fUv.y);
            FragColor = texture(uTexture0, uv) * fTintColor;
        }
        """;

    // 1px縁取り（白）: 透明ピクセルの隣に不透明ピクセルがあれば白を描画
    private const string OutlineFragSrc = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        uniform vec2 uTextureSize;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uTexture0, fUv) * fTintColor;
            if (c.a > 0.5) {
                FragColor = c;
                return;
            }
            vec2 texel = 1.0 / uTextureSize;
            float n = texture(uTexture0, fUv + vec2( 0.0,      texel.y)).a;
            float s = texture(uTexture0, fUv + vec2( 0.0,     -texel.y)).a;
            float e = texture(uTexture0, fUv + vec2( texel.x,  0.0    )).a;
            float w = texture(uTexture0, fUv + vec2(-texel.x,  0.0    )).a;
            FragColor = (n + s + e + w > 0.5)
                ? vec4(1.0, 1.0, 1.0, 1.0)
                : vec4(0.0);
        }
        """;

    public override void OnStart()
    {
        console.Print("Press [ESC] to exit");

        _texture = Window.TextureFactory.Load("assets/ichigo.png");

        // シェーダーのコンパイル（OnStart以降・メインスレッドで呼ぶ必要あり）
        _sepiaShader        = ShaderProgram.Create().Vertex(VertSrc).Fragment(SepiaFragSrc).Compile();
        _grayscaleShader    = ShaderProgram.Create().Vertex(VertSrc).Fragment(GrayscaleFragSrc).Compile();
        _mosaicShader       = ShaderProgram.Create().Vertex(VertSrc).Fragment(MosaicFragSrc).Compile();
        _outlineShader      = ShaderProgram.Create().Vertex(VertSrc).Fragment(OutlineFragSrc).Compile();
        _rasterScrollShader = ShaderProgram.Create().Vertex(VertSrc).Fragment(RasterScrollFragSrc).Compile();

        var texSize = new Vector2(_texture.Size.X, _texture.Size.Y);

        // マテリアルの作成とUniform設定
        var sepiaMat     = new Material(_sepiaShader);

        var grayscaleMat = new Material(_grayscaleShader);

        var mosaicMat    = new Material(_mosaicShader);
        mosaicMat["uTextureSize"] = texSize;
        mosaicMat["uBlockSize"]   = 4.0f;

        var outlineMat   = new Material(_outlineShader);
        outlineMat["uTextureSize"] = texSize;

        _rasterScrollMat = new Material(_rasterScrollShader);
        _rasterScrollMat["uTime"] = 0.0f;

        // 2×3グリッドで6エフェクトを配置
        var font      = Font.GetDefault(18);
        var dispSize  = 180;
        var scale     = (float)dispSize / _texture.Size.X;
        var cols      = 3;
        var marginX   = (Window.Width  - cols * dispSize) / (cols + 1);
        var marginY   = 60;
        var labelH    = 28;
        var rowH      = dispSize + labelH + marginY;

        (string Label, Material? Mat)[] effects =
        [
            ("デフォルト",           null),
            ("セピア",               sepiaMat),
            ("グレースケール",        grayscaleMat),
            ("モザイク",              mosaicMat),
            ("縁取り（白 1px）",     outlineMat),
            ("ラスタースクロール",    _rasterScrollMat),
        ];

        for (var i = 0; i < effects.Length; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var x   = marginX + col * (dispSize + marginX);
            var y   = marginY + row * rowH;

            var sprite = new Sprite(_texture)
            {
                Material = effects[i].Mat
            };
            sprite.Location(x, y).Scale(scale, scale);

            var label = new Text(effects[i].Label, font, Color.White)
                .Location(x, y + dispSize + 4);

            Root.AddRange(sprite, label);
        }
    }

    public override void OnUpdate()
    {
        // ラスタースクロールのアニメーション時間を更新
        _rasterScrollMat["uTime"] = (float)Window.TotalTime;

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _texture.Dispose();
        _sepiaShader.Dispose();
        _grayscaleShader.Dispose();
        _mosaicShader.Dispose();
        _outlineShader.Dispose();
        _rasterScrollShader.Dispose();
    }
}
