using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// レビュー指摘 #06 / #08 の再現シーン。
///
/// #06: VulkanShaderManager の未対応 Uniform ブロック警告は fragBlocks しか検査しない。
///      頂点ステージで set != 1 の Uniform ブロックを宣言すると、値は merged から
///      除外されるにもかかわらず警告が一切出ない。
///
/// #08: VulkanPipelineProvider は SPIR-V リフレクションで得た maxSet を上限チェックなしに
///      stackalloc のサイズへ渡す。巨大な set 番号を宣言したシェーダーでスタックが溢れる。
///      デバイスの maxBoundDescriptorSets (通常 4〜8) でクランプすべき。
///
/// 注意: #08 のシェーダーは意図的にプロセスを巻き添えにする可能性がある。
///       クランプ修正の検証用であり、通常のデモ操作では実行しないこと。
/// </summary>
[Demo("/debug/vulkan_shader_reflection", "指摘#06/#08: シェーダーリフレクションの境界処理")]
public class VulkanShaderReflectionDebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private const string StandardVertSrc = """
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

    // 頂点ステージに set=2 の Uniform ブロックを持つシェーダー (指摘#06)
    // set=1 ではないため merged から落ちるが、警告は出ない
    private const string VertexUniformVertSrc = """
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

        uniform mat4 uProjection;

        // set=1 ではないため、この uWobble は静かに無視される
        layout(set = 2, binding = 0) uniform VertexParams {
            float uWobble;
        };

        void main()
        {
            mat4 model = mat4(iModel0, iModel1, iModel2, iModel3);
            vec2 p = vPos + vec2(0.0, uWobble);
            gl_Position = uProjection * model * vec4(p, 0.0, 1.0);
            fUv = mix(iUvRect.xy, iUvRect.zw, vUv);
            fTintColor = iTintColor;
        }
        """;

    private const string PlainFragSrc = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fTintColor;
        layout(set = 0, binding = 0) uniform sampler2D uTexture0;
        layout(location = 0) out vec4 FragColor;

        void main()
        {
            FragColor = texture(uTexture0, fUv) * fTintColor;
        }
        """;

    // 巨大な set 番号を宣言するシェーダー (指摘#08)
    private const string HugeSetFragSrc = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fTintColor;
        layout(set = 0, binding = 0) uniform sampler2D uTexture0;
        layout(set = 100000, binding = 0) uniform sampler2D uUnreasonable;
        layout(location = 0) out vec4 FragColor;

        void main()
        {
            FragColor = texture(uTexture0, fUv) * fTintColor
                      + texture(uUnreasonable, fUv) * 0.0;
        }
        """;

    private Texture2D _texture;
    private Sprite _sprite = null!;

    public override void OnStart()
    {
        _texture = App.TextureFactory.Load("assets/ichigo2.png");
        _sprite = new Sprite(_texture).Location(280, 180).Scale(4, 4);
        Root.Add(_sprite);

        console.Print("指摘#06 / #08 再現シーン");
        console.Print("1: 頂点ステージ set=2 の Uniform (指摘#06)");
        console.Print("   -> uWobble を設定しても効かず、警告も出ないことを確認");
        console.Print("9: set=100000 のサンプラー (指摘#08) ※スタックオーバーフローの危険");
        console.Print("0: 標準シェーダーへ戻す");
    }

    public override void OnUpdate()
    {
        if (keyboard.Number1.IsKeyDown)
            TryVertexUniform();

        if (keyboard.Number9.IsKeyDown)
            TryHugeSet();

        if (keyboard.Number0.IsKeyDown)
        {
            _sprite.Material = null;
            console.Print("標準シェーダーへ戻した");
        }

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    /// <summary>
    /// 指摘#06: 頂点ステージの set=2 Uniform が無警告で捨てられることを確認する。
    /// </summary>
    private void TryVertexUniform()
    {
        try
        {
            var shader = ShaderProgram
                .Create()
                .Vertex(VertexUniformVertSrc)
                .Fragment(PlainFragSrc)
                .Compile();

            _sprite.Material = new Material(shader) { ["uWobble"] = 32f };
            console.Print("set=2 の頂点 Uniform を適用した");
            console.Print("uWobble=32 が効いていなければ指摘#06 の再現 (警告なしで無視)");
        }
        catch (Exception ex)
        {
            console.Print($"コンパイル失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 指摘#08: リフレクション由来の巨大 set 番号で stackalloc が破綻することを確認する。
    /// </summary>
    private void TryHugeSet()
    {
        console.Print("set=100000 のシェーダーを構築中...");
        try
        {
            var shader = ShaderProgram
                .Create()
                .Vertex(StandardVertSrc)
                .Fragment(HugeSetFragSrc)
                .Compile();

            _sprite.Material = new Material(shader);
            console.Print("適用した。次の描画でパイプライン構築が走る");
            console.Print("ここで落ちる、または応答しなくなれば指摘#08 の再現");
        }
        catch (Exception ex)
        {
            console.Print($"例外を捕捉: {ex.GetType().Name}: {ex.Message}");
            console.Print("明確なエラーで弾けていれば、クランプ修正が効いている");
        }
    }

    public override void OnDestroy()
    {
        _sprite.Destroy();
        _texture.Dispose();
    }
}
