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
///      stackalloc のサイズへ渡す。デバイスの maxBoundDescriptorSets (通常 4〜8) で
///      クランプすべきだが、していない。
///
///      当初「スタックオーバーフローする」と評価したが、これは誤り。
///      shaderc が set >= 255 を拒否するため stackalloc は最大 2KB 程度にとどまる。
///
///      実際の危険はより深刻で、デバイスの maxBoundDescriptorSets を超える
///      セット数を CreatePipelineLayout に渡すこと。仕様違反 (VUID-...-00286) であり、
///      バリデーションレイヤ不在の環境ではドライバ内でアクセス違反 (0xC0000005) により
///      プロセスが即死する。修正後は事前に InvalidOperationException で弾く。
/// </summary>
[Demo("/debug/vulkan_shader_reflection", "指摘#06/#08: シェーダーリフレクションの境界処理")]
public class VulkanShaderReflectionDebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    // Vulkan 向けの標準頂点シェーダー。
    // uProjection は push constant (64 バイト = mat4)、in/out はすべて location 必須。
    private const string StandardVertSrc = """
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

        layout(push_constant) uniform PushConstants
        {
            mat4 uProjection;
        };

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

        layout(push_constant) uniform PushConstants
        {
            mat4 uProjection;
        };

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

    // 大きな set 番号を宣言するシェーダー (指摘#08)。
    // shaderc が 255 以上の set を 'set is too large' で拒否するため、
    // GLSL 経由で宣言できるのは 32〜254 未満の範囲にとどまる。
    // よって stackalloc は最大でも DescriptorSetLayout(8バイト) × 255 = 約 2KB で、
    // スタックオーバーフローには至らない。
    //
    // 実際の危険は maxBoundDescriptorSets (多くの実装で 4〜8、環境により 32) の超過。
    // 修正前はこの値を検査せず CreatePipelineLayout に渡していたため、
    // バリデーションレイヤ不在の環境ではドライバ内でアクセス違反 (0xC0000005) を起こした。
    private const string HugeSetFragSrc = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fTintColor;
        layout(set = 0, binding = 0) uniform sampler2D uTexture0;
        layout(set = 32, binding = 0) uniform sampler2D uUnreasonable;
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
        console.Print("9: set=32 のサンプラー (指摘#08) デバイス上限超過を弾けるか");
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
    /// 指摘#08: デバイス上限を超える set 番号が、事前に弾かれるかを確認する。
    /// </summary>
    private void TryHugeSet()
    {
        console.Print("set=32 のシェーダーを構築中...");
        try
        {
            var shader = ShaderProgram
                .Create()
                .Vertex(StandardVertSrc)
                .Fragment(HugeSetFragSrc)
                .Compile();

            _sprite.Material = new Material(shader);
            console.Print("適用した。次の描画でパイプライン構築が走る");
            console.Print("修正前はここでドライバ内アクセス違反 (0xC0000005) により即死する");
        }
        catch (Exception ex)
        {
            console.Print($"例外を捕捉: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // 補足: パイプライン構築は描画時に走るため、修正後の InvalidOperationException は
    // ここではなく OnRender 経由で送出される。コンソールではなく標準の例外として現れる。

    public override void OnDestroy()
    {
        _sprite.Destroy();
        _texture.Dispose();
    }
}
