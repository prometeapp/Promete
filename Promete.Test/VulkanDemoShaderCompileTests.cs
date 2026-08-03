using FluentAssertions;
using Promete.Graphics.Rendering.Vulkan;
using Silk.NET.Shaderc;

namespace Promete.Test;

/// <summary>
/// デバッグデモが使うカスタムシェーダーが、Vulkan 方言の GLSL として
/// 実際にコンパイルできることを確認する。
///
/// シェーダーのコンパイルは実行時に行われるため、C# のビルドが通っても
/// シェーダーの誤りは検出されない。デモを起動せずに検証するためのテスト。
///
/// Vulkan の規約:
///   - #version 450
///   - in / out はすべて location 指定が必須
///   - 非 opaque な uniform はブロックに入れる (裸の uniform mat4 は不可)
///   - uProjection は push constant (64 バイト = mat4)
/// </summary>
public class VulkanDemoShaderCompileTests
{
    /// <summary>デモが使う標準頂点シェーダー。</summary>
    private const string StandardVert = """
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

    /// <summary>指摘#06 用。頂点ステージに set=2 の Uniform ブロックを持つ。</summary>
    private const string VertexUniformVert = """
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

    private const string PlainFrag = """
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

    /// <summary>指摘#08 用。GLSL で到達可能な上限付近の set 番号を宣言する。</summary>
    private const string HugeSetFrag = """
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

    /// <summary>指摘#07 用。Material Uniform を set=1, binding=0 に置く。</summary>
    private const string MaterialFrag = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fTintColor;

        layout(set = 0, binding = 0) uniform sampler2D uTexture0;

        layout(set = 1, binding = 0) uniform MaterialParams
        {
            float uTime;
        };

        layout(location = 0) out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uTexture0, fUv) * fTintColor;
            FragColor = vec4(c.rgb * (0.5 + 0.5 * sin(uTime)), c.a);
        }
        """;

    [Theory]
    [InlineData("StandardVert", ShaderKind.VertexShader)]
    [InlineData("VertexUniformVert", ShaderKind.VertexShader)]
    [InlineData("PlainFrag", ShaderKind.FragmentShader)]
    [InlineData("HugeSetFrag", ShaderKind.FragmentShader)]
    [InlineData("MaterialFrag", ShaderKind.FragmentShader)]
    public void デモのシェーダーがコンパイルできる(string name, ShaderKind kind)
    {
        var source = name switch
        {
            "StandardVert" => StandardVert,
            "VertexUniformVert" => VertexUniformVert,
            "PlainFrag" => PlainFrag,
            "HugeSetFrag" => HugeSetFrag,
            "MaterialFrag" => MaterialFrag,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

        using var compiler = new VulkanShaderCompiler();

        var act = () => compiler.Compile(source, kind, $"{name}.glsl");

        act.Should().NotThrow();
    }

    /// <summary>
    /// 指摘#06 のシェーダーが、意図通り set=2 の Uniform ブロックとして
    /// リフレクションに現れることを確認する。
    /// </summary>
    [Fact]
    public void 指摘06のシェーダーはset2のUniformブロックを持つ()
    {
        using var compiler = new VulkanShaderCompiler();
        var spirv = compiler.Compile(VertexUniformVert, ShaderKind.VertexShader, "wobble.vert");

        var (blocks, _) = SpirvReflector.Reflect(spirv);

        blocks
            .Should()
            .Contain(
                b => b.Set == 2 && b.Binding == 0,
                "set=1 以外のブロックは merged から落ちるが、警告が出ない (指摘#06)"
            );
    }

    /// <summary>
    /// 指摘#06 の修正確認。未対応ブロックの検査は両ステージを対象にする必要がある。
    /// 頂点ステージのみに set!=1 のブロックがある場合、フラグメント側だけを見ると
    /// 検出できないことを示す。
    /// </summary>
    [Fact]
    public void 未対応ブロックの検査は両ステージを見なければ検出できない()
    {
        using var compiler = new VulkanShaderCompiler();

        var vertSpv = compiler.Compile(VertexUniformVert, ShaderKind.VertexShader, "wobble.vert");
        var fragSpv = compiler.Compile(PlainFrag, ShaderKind.FragmentShader, "plain.frag");

        var (vertBlocks, _) = SpirvReflector.Reflect(vertSpv);
        var (fragBlocks, _) = SpirvReflector.Reflect(fragSpv);

        // 修正前の判定 (フラグメントのみ): 検出できない
        fragBlocks
            .Should()
            .NotContain(
                b => b.Set != 1 || b.Binding != 0,
                "フラグメント側だけを見ると未対応ブロックを見逃す (修正前の挙動)"
            );

        // 修正後の判定 (両ステージ): 検出できる
        vertBlocks
            .Concat(fragBlocks)
            .Should()
            .Contain(
                b => b.Set != 1 || b.Binding != 0,
                "両ステージを見れば頂点側の set=2 ブロックを検出できる"
            );
    }

    /// <summary>
    /// 指摘#08 のシェーダーが、上限付近の set 番号をリフレクションに載せることを確認する。
    /// この値が VulkanPipelineProvider のレイアウト生成へ渡る。
    /// </summary>
    [Fact]
    public void 指摘08のシェーダーは上限付近のset番号を報告する()
    {
        using var compiler = new VulkanShaderCompiler();
        var spirv = compiler.Compile(HugeSetFrag, ShaderKind.FragmentShader, "huge.frag");

        var (_, samplers) = SpirvReflector.Reflect(spirv);

        samplers
            .Should()
            .Contain(s => s.Set == 32, "この値が上限チェックなしに stackalloc とレイアウト生成へ渡る (指摘#08)");
    }
}
