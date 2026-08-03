using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.ImGui;
using Promete.Nodes;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using Rgba32Image = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

namespace Promete.Experimental.Vulkan;

/// <summary>
/// Vulkan バックエンドの描画検証シーン。
/// フェーズ1: スプライト・プリミティブ・FrameBuffer・PieSprite・カスタムマテリアルを検証。
/// フェーズ2: ポストプロセス (色反転) を適用して検証し、終了します。
/// </summary>
public class MainScene(ImGuiPlugin imGui) : Scene
{
    private const int Phase1Frame = 60;
    private const int Phase2Frame = 120;

    private static readonly string ScreenshotPath1 = Path.Combine(AppContext.BaseDirectory, "vulkan_test.png");
    private static readonly string ScreenshotPath2 = Path.Combine(AppContext.BaseDirectory, "vulkan_test_postprocess.png");

    private const string InstancedVertexShader = """
        #version 450
        layout(location = 0) in vec2 vPos;
        layout(location = 1) in vec2 vUv;
        layout(location = 2) in vec4 iModel0;
        layout(location = 3) in vec4 iModel1;
        layout(location = 4) in vec4 iModel2;
        layout(location = 5) in vec4 iModel3;
        layout(location = 6) in vec4 iTintColor;
        layout(location = 7) in vec4 iUvRect;
        layout(location = 0) out vec2 fUv;
        layout(location = 1) out vec4 fTintColor;
        layout(push_constant) uniform PushConstants { mat4 uProjection; };
        void main()
        {
            mat4 model = mat4(iModel0, iModel1, iModel2, iModel3);
            gl_Position = uProjection * model * vec4(vPos, 0.0, 1.0);
            fUv = mix(iUvRect.xy, iUvRect.zw, vUv);
            fTintColor = iTintColor;
        }
        """;

    private const string OverrideColorFragmentShader = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fTintColor;
        layout(set = 0, binding = 0) uniform sampler2D uTexture0;
        layout(set = 1, binding = 0) uniform Uniforms { vec4 uOverrideColor; };
        layout(location = 0) out vec4 FragColor;
        void main()
        {
            FragColor = uOverrideColor;
        }
        """;

    private const string PrimitiveVertexShader = """
        #version 450
        layout(location = 0) in vec2 vPos;
        void main()
        {
            gl_Position = vec4(vPos, 0.0, 1.0);
        }
        """;

    private const string PrimitiveUboFragmentShader = """
        #version 450
        layout(set = 1, binding = 0) uniform Uniforms { vec4 uFillColor; };
        layout(location = 0) out vec4 FragColor;
        void main()
        {
            FragColor = uFillColor;
        }
        """;

    private const string ExtraTextureFragmentShader = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fTintColor;
        layout(set = 0, binding = 0) uniform sampler2D uTexture0;
        layout(set = 2, binding = 0) uniform sampler2D uExtraTexture;
        layout(location = 0) out vec4 FragColor;
        void main()
        {
            FragColor = texture(uTexture0, fUv) * texture(uExtraTexture, fUv) * fTintColor;
        }
        """;

    private const string PieVertexShader = """
        #version 450
        layout(location = 0) in vec2 vPos;
        layout(location = 1) in vec2 vUv;
        layout(location = 0) out vec2 fUv;
        layout(push_constant) uniform PushConstants
        {
            mat4 uMvp;
            vec4 uTintColor;
            vec2 uAngles;
        };
        void main()
        {
            gl_Position = uMvp * vec4(vPos, 0.0, 1.0);
            fUv = vUv;
        }
        """;

    private const string PieUboFragmentShader = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(set = 0, binding = 0) uniform sampler2D uTexture0;
        layout(set = 1, binding = 0) uniform Uniforms { vec4 uPieColor; };
        layout(location = 0) out vec4 FragColor;
        void main()
        {
            FragColor = texture(uTexture0, fUv) * uPieColor;
        }
        """;

    private const string BlitVertexShader = """
        #version 450
        layout(location = 0) out vec2 fUv;
        void main()
        {
            vec2 pos = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
            fUv = pos;
            gl_Position = vec4(pos * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    private const string InvertFragmentShader = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(set = 0, binding = 0) uniform sampler2D uScreenTexture;
        layout(location = 0) out vec4 FragColor;
        void main()
        {
            vec4 c = texture(uScreenTexture, fUv);
            FragColor = vec4(1.0 - c.rgb, 1.0);
        }
        """;

    private Texture2D _redTexture;
    private FrameBuffer? _frameBuffer;
    private ShaderProgram? _overrideShader;
    private ShaderProgram? _invertShader;
    private ShaderProgram? _primitiveShader;
    private ShaderProgram? _extraTextureShader;
    private ShaderProgram? _pieShader;
    private int _frameCount;
    private int _phase;
    private int _failures;

    public override void OnStart()
    {
        App.BackgroundColor = Color.DarkSlateBlue;

        // 赤い 100x100 スプライト (左上 50,50)
        _redTexture = App.TextureFactory.CreateSolid(Color.Red, (100, 100));
        Root.Add(new Sprite(_redTexture).Location(50, 50));

        // ライム色の塗りつぶし矩形 (250,100)-(350,200)
        Root.Add(Shape.CreateRect(250, 100, 350, 200, Color.Lime));

        // ティント検証用: 白テクスチャ × 青ティント (450,50)
        var white = App.TextureFactory.CreateSolid(Color.White, (50, 50));
        var tinted = new Sprite(white).Location(450, 50);
        tinted.TintColor = Color.Blue;
        Root.Add(tinted);

        // FrameBuffer の上下向き検証: 黄背景 100x100 の上部 30px にマゼンタ帯
        _frameBuffer = new FrameBuffer(100, 100) { BackgroundColor = Color.Yellow };
        var magenta = App.TextureFactory.CreateSolid(Color.Magenta, (100, 30));
        _frameBuffer.Add(new Sprite(magenta).Location(0, 0));
        Root.Add(new Sprite(_frameBuffer.Texture).Location(50, 300));

        // PieSprite 検証: シアン 100x100、0% から 25% (12時→3時の扇形)
        var cyan = App.TextureFactory.CreateSolid(Color.Cyan, (100, 100));
        var pie = new PieSprite(cyan) { StartPercent = 0, Percent = 25 };
        pie.Location = (250, 300);
        Root.Add(pie);

        // カスタムマテリアル検証: UBO の uOverrideColor で塗りつぶすシェーダー
        _overrideShader = ShaderProgram
            .Create()
            .Vertex(InstancedVertexShader)
            .Fragment(OverrideColorFragmentShader)
            .Compile();
        var material = new Material(_overrideShader);
        material["uOverrideColor"] = new Vector4(1f, 0.4f, 0f, 1f); // (255, 102, 0)
        var customSprite = new Sprite(white).Location(450, 150);
        customSprite.Material = material;
        Root.Add(customSprite);

        // ステンシルマスク検証: 左半分白・右半分黒のマスク + オレンジ全面スプライト
        var halfMask100 = CreateHalfMask((100, 100));
        var orange = App.TextureFactory.CreateSolid(Color.Orange, (100, 100));
        var stencilMasked = new MaskedContainer(halfMask100) { Size = (100, 100) };
        stencilMasked.Location = (450, 300);
        stencilMasked.Add(new Sprite(orange));
        Root.Add(stencilMasked);

        // アルファマスク検証: 左半分白・右半分黒のマスク + ピンク全面スプライト
        var halfMask60 = CreateHalfMask((60, 60));
        var pink = App.TextureFactory.CreateSolid(Color.HotPink, (60, 60));
        var alphaMasked = new MaskedContainer(halfMask60, useAlphaMask: true) { Size = (60, 60) };
        alphaMasked.Location = (560, 380);
        alphaMasked.Add(new Sprite(pink));
        Root.Add(alphaMasked);

        // Primitive カスタムマテリアル検証: UBO の uFillColor で塗る矩形
        _primitiveShader = ShaderProgram
            .Create()
            .Vertex(PrimitiveVertexShader)
            .Fragment(PrimitiveUboFragmentShader)
            .Compile();
        var primitiveMaterial = new Material(_primitiveShader);
        primitiveMaterial["uFillColor"] = new Vector4(0.5f, 0f, 0.5f, 1f); // (128, 0, 128)
        var customRect = Shape.CreateRect(50, 430, 110, 470, Color.White);
        customRect.Material = primitiveMaterial;
        Root.Add(customRect);

        // Texture2D Uniform 検証: 白スプライト × 追加テクスチャ (緑, set=2)
        _extraTextureShader = ShaderProgram
            .Create()
            .Vertex(InstancedVertexShader)
            .Fragment(ExtraTextureFragmentShader)
            .Compile();
        var extraMaterial = new Material(_extraTextureShader);
        extraMaterial["uExtraTexture"] = App.TextureFactory.CreateSolid(Color.Lime, (40, 40));
        var extraSprite = new Sprite(App.TextureFactory.CreateSolid(Color.White, (40, 40))).Location(560, 50);
        extraSprite.Material = extraMaterial;
        Root.Add(extraSprite);

        // PieSprite カスタムマテリアル検証: UBO の uPieColor で塗る (全周)
        _pieShader = ShaderProgram
            .Create()
            .Vertex(PieVertexShader)
            .Fragment(PieUboFragmentShader)
            .Compile();
        var pieMaterial = new Material(_pieShader);
        pieMaterial["uPieColor"] = new Vector4(1f, 0.08f, 0.58f, 1f); // DeepPink (255, 20, 147)
        var customPie = new PieSprite(App.TextureFactory.CreateSolid(Color.White, (30, 30)))
        {
            StartPercent = 0,
            Percent = 100,
        };
        customPie.Location = (600, 240);
        customPie.Material = pieMaterial;
        Root.Add(customPie);

        // フェーズ2用: 色反転ポストプロセスシェーダー
        _invertShader = ShaderProgram
            .Create()
            .Vertex(BlitVertexShader)
            .Fragment(InvertFragmentShader)
            .Compile();

        // ImGui 描画検証: デモウィンドウを表示 (スワップチェーンへのオーバーレイ描画パスを通す)
        imGui.Render += OnImGuiRender;

        Console.WriteLine("[MainScene] OnStart: ノード配置・シェーダーコンパイル完了");
    }

    public override void OnUpdate()
    {
        _frameCount++;

        if (_frameCount == Phase1Frame && _phase == 0)
        {
            _phase = 1;
            _ = RunPhase1Async();
        }

        if (_frameCount == Phase2Frame && _phase == 1)
        {
            _phase = 2;
            _ = RunPhase2Async();
        }
    }

    public override void OnDestroy()
    {
        imGui.Render -= OnImGuiRender;
        _frameBuffer?.Dispose();
        _overrideShader?.Dispose();
        _invertShader?.Dispose();
        _primitiveShader?.Dispose();
        _extraTextureShader?.Dispose();
        _pieShader?.Dispose();
        _redTexture.Dispose();
        Console.WriteLine("[MainScene] OnDestroy");
    }

    private async Task RunPhase1Async()
    {
        try
        {
            await Window.SaveScreenshotAsync(ScreenshotPath1);
            using var img = ImageSharpImage.Load<Rgba32>(ScreenshotPath1);

            Console.WriteLine("[MainScene] --- フェーズ1: 通常描画 ---");
            _failures += Verify(img, 10, 10, Color.DarkSlateBlue, "背景 (左上)");
            _failures += Verify(img, 100, 100, Color.Red, "赤スプライト中心");
            _failures += Verify(img, 300, 150, Color.Lime, "ライム矩形中心");
            _failures += Verify(img, 475, 75, Color.Blue, "青ティントスプライト");
            _failures += Verify(img, 620, 460, Color.DarkSlateBlue, "背景 (右下)");
            _failures += Verify(img, 100, 310, Color.Magenta, "FrameBuffer 上部 (マゼンタ帯)");
            _failures += Verify(img, 100, 380, Color.Yellow, "FrameBuffer 下部 (黄背景)");
            _failures += Verify(img, 320, 330, Color.Cyan, "PieSprite 右上 1/4 (シアン)");
            _failures += Verify(img, 280, 370, Color.DarkSlateBlue, "PieSprite 左下 (背景=切り抜き)");
            _failures += Verify(img, 475, 175, Color.FromArgb(255, 102, 0), "カスタムマテリアル (uOverrideColor)");
            _failures += Verify(img, 470, 350, Color.Orange, "ステンシルマスク 左半分 (表示)");
            _failures += Verify(img, 530, 350, Color.DarkSlateBlue, "ステンシルマスク 右半分 (非表示)");
            _failures += Verify(img, 575, 410, Color.HotPink, "アルファマスク 左半分 (表示)");
            _failures += Verify(img, 605, 410, Color.DarkSlateBlue, "アルファマスク 右半分 (非表示)");
            _failures += Verify(img, 80, 450, Color.FromArgb(128, 0, 128), "Primitive カスタムマテリアル (uFillColor)");
            _failures += Verify(img, 580, 70, Color.Lime, "Texture2D Uniform (uExtraTexture)");
            _failures += Verify(img, 615, 255, Color.FromArgb(255, 20, 148), "Pie カスタムマテリアル (uPieColor)");

            // フェーズ2: 色反転ポストプロセスを適用
            App.PostProcessMaterials.Add(new Material(_invertShader!));
            Console.WriteLine("[MainScene] ポストプロセス (色反転) を適用");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainScene] ❌ フェーズ1で例外: {ex}");
            App.Exit(2);
        }
    }

    private async Task RunPhase2Async()
    {
        try
        {
            await Window.SaveScreenshotAsync(ScreenshotPath2);
            using var img = ImageSharpImage.Load<Rgba32>(ScreenshotPath2);

            Console.WriteLine("[MainScene] --- フェーズ2: ポストプロセス (色反転) ---");
            var invBg = Color.FromArgb(255 - 72, 255 - 61, 255 - 139);
            _failures += Verify(img, 10, 10, invBg, "背景 反転");
            _failures += Verify(img, 100, 100, Color.Cyan, "赤スプライト 反転 (シアン)");
            _failures += Verify(img, 300, 150, Color.Magenta, "ライム矩形 反転 (マゼンタ)");

            Console.WriteLine(
                _failures == 0
                    ? "[MainScene] ✅ 全ピクセル検証パス"
                    : $"[MainScene] ❌ {_failures} 件の検証失敗"
            );
            App.Exit(_failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainScene] ❌ フェーズ2で例外: {ex}");
            App.Exit(2);
        }
    }

    private static void OnImGuiRender()
    {
        ImGuiNET.ImGui.ShowDemoWindow();
    }

    /// <summary>
    /// 左半分が白、右半分が黒のマスクテクスチャを生成します。
    /// </summary>
    private Texture2D CreateHalfMask(VectorInt size)
    {
        var arr = new byte[size.X * size.Y * 4];
        for (var y = 0; y < size.Y; y++)
        {
            for (var x = 0; x < size.X; x++)
            {
                var i = ((y * size.X) + x) * 4;
                var value = x < size.X / 2 ? (byte)255 : (byte)0;
                arr[i + 0] = value;
                arr[i + 1] = value;
                arr[i + 2] = value;
                arr[i + 3] = 255;
            }
        }

        return App.TextureFactory.Create(arr, size);
    }

    private static int Verify(Rgba32Image img, int x, int y, Color expected, string label)
    {
        var actual = img[x, y];
        var ok =
            Math.Abs(actual.R - expected.R) <= 2
            && Math.Abs(actual.G - expected.G) <= 2
            && Math.Abs(actual.B - expected.B) <= 2;

        Console.WriteLine(
            $"[MainScene] {(ok ? "OK" : "NG")} {label} ({x},{y}): "
                + $"expected=({expected.R},{expected.G},{expected.B}) actual=({actual.R},{actual.G},{actual.B})"
        );
        return ok ? 0 : 1;
    }
}
