using System.Drawing;
using System.Numerics;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/postProcess.demo", "画面全体にエフェクトをかける")]
public class PostProcessDemo(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private Texture2D _texture;
    private ShaderProgram _sepiaShader = null!;
    private ShaderProgram _grayscaleShader = null!;
    private ShaderProgram _mosaicShader = null!;
    private ShaderProgram _rasterScrollShader = null!;

    private Material _sepiaMat = null!;
    private Material _grayscaleMat = null!;
    private Material _mosaicMat = null!;
    private Material _rasterScrollMat = null!;

    // 頂点シェーダー
    private const string VertSrc = """
        #version 330 core
        layout (location = 0) in vec2 vPos;
        layout (location = 1) in vec2 vUv;

        out vec2 fUv;

        void main()
        {
            gl_Position = vec4(vPos.x, vPos.y, 0.0, 1.0);
            fUv = vUv;
        }
        """;

    // セピア変換
    private const string SepiaFragSrc = """
        #version 330 core
        in vec2 fUv;
        uniform sampler2D uScreenTexture;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uScreenTexture, fUv);
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
        uniform sampler2D uScreenTexture;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uScreenTexture, fUv);
            float gray = dot(c.rgb, vec3(0.299, 0.587, 0.114));
            FragColor = vec4(gray, gray, gray, c.a);
        }
        """;

    // モザイク（uBlockSize px 単位でスナップ）
    private const string MosaicFragSrc = """
        #version 330 core
        in vec2 fUv;
        uniform sampler2D uScreenTexture;
        uniform vec2 uTextureSize;
        uniform float uBlockSize;
        out vec4 FragColor;

        void main()
        {
            vec2 pixel   = fUv * uTextureSize;
            vec2 snapped = floor(pixel / uBlockSize) * uBlockSize + uBlockSize * 0.5;
            vec2 uv      = snapped / uTextureSize;
            FragColor = texture(uScreenTexture, uv);
        }
        """;

    // ラスタースクロール: 走査線ごとに sin でX方向オフセット → うにょうにょ
    private const string RasterScrollFragSrc = """
        #version 330 core
        in vec2 fUv;
        uniform sampler2D uScreenTexture;
        uniform float uTime;
        out vec4 FragColor;

        void main()
        {
            // 走査線（Y座標）ごとにサイン波でX方向へオフセット
            float offset = sin(fUv.y * 18.0 + uTime * 4.0) * 0.06;
            vec2 uv = vec2(fUv.x + offset, fUv.y);
            FragColor = texture(uScreenTexture, uv);
        }
        """;

    public override void OnStart()
    {
        console.Print("Press [ESC] to exit");
        _texture = App.TextureFactory.Load("assets/ichigo.png");

        // シェーダーのコンパイル（OnStart以降・メインスレッドで呼ぶ必要あり）
        _sepiaShader = ShaderProgram.Create().Vertex(VertSrc).Fragment(SepiaFragSrc).Compile();
        _grayscaleShader = ShaderProgram
            .Create()
            .Vertex(VertSrc)
            .Fragment(GrayscaleFragSrc)
            .Compile();
        _mosaicShader = ShaderProgram.Create().Vertex(VertSrc).Fragment(MosaicFragSrc).Compile();
        _rasterScrollShader = ShaderProgram
            .Create()
            .Vertex(VertSrc)
            .Fragment(RasterScrollFragSrc)
            .Compile();

        var texSize = new Vector2(_texture.Size.X, _texture.Size.Y);

        // マテリアルの作成とUniform設定
        _sepiaMat = new Material(_sepiaShader);
        _grayscaleMat = new Material(_grayscaleShader);
        _mosaicMat = new Material(_mosaicShader)
        {
            ["uTextureSize"] = texSize,
            ["uBlockSize"] = 2.0f,
        };

        _rasterScrollMat = new Material(_rasterScrollShader) { ["uTime"] = 0.0f };

        // 画面上にいくつかの位置、サイズ、色、角度がランダムないちごを生成
        for (var i = 0; i < 100; i++)
        {
            var ichigo = new Sprite(_texture)
                .Location(Random.Shared.NextVector(View.Size))
                .Size(Random.Shared.NextVectorInt(128, 128) + (16, 16))
                .Angle(Random.Shared.Next(0, 359).Degrees)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Center);
            ichigo.TintColor = Random.Shared.NextColor();

            Root.Add(ichigo);
        }

        App.BackgroundColor = Color.DarkGray;
    }

    public override void OnUpdate()
    {
        // ラスタースクロールのアニメーション時間を更新
        _rasterScrollMat["uTime"] = (float)Time.TotalTime;

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (keyboard.Number1.IsKeyUp)
            ToggleMaterial(_sepiaMat);
        if (keyboard.Number2.IsKeyUp)
            ToggleMaterial(_grayscaleMat);
        if (keyboard.Number3.IsKeyUp)
            ToggleMaterial(_mosaicMat);
        if (keyboard.Number4.IsKeyUp)
            ToggleMaterial(_rasterScrollMat);
    }

    public override void OnDestroy()
    {
        App.BackgroundColor = Color.Black;
        App.PostProcessMaterials.Clear();
        _texture.Dispose();
        _sepiaShader.Dispose();
        _grayscaleShader.Dispose();
        _mosaicShader.Dispose();
        _rasterScrollShader.Dispose();
    }

    private void ToggleMaterial(Material mat)
    {
        if (!App.PostProcessMaterials.Remove(mat))
            App.PostProcessMaterials.Add(mat);
    }
}
