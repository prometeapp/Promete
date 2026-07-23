using System;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan バックエンドにおける <see cref="IShaderFactory"/> の実装です。
/// Vulkan 方言の GLSL 450 を shaderc で SPIR-V にコンパイルします。
/// </summary>
/// <remarks>
/// シェーダー規約:
/// <list type="bullet">
/// <item>スプライト用: texture_instanced.vert 互換の頂点レイアウト (location 0-7) と
/// push_constant の mat4 uProjection を使用すること</item>
/// <item>ポストプロセス用: 頂点入力なし (blit.vert 互換)、set=0, binding=0 に入力テクスチャ</item>
/// <item>カスタム Uniform (Material) は set=1, binding=0 の uniform ブロックに宣言すること</item>
/// </list>
/// </remarks>
internal sealed class VulkanShaderFactory(
    VulkanShaderManager shaders,
    VulkanPipelineProvider pipelines
) : IShaderFactory
{
    public void Compile(ShaderProgram program)
    {
        PrometeApp.Current.ThrowIfNotMainThread();

        var vertexSource =
            program.VertexShaderSource
            ?? throw new InvalidOperationException(
                "頂点シェーダーのソースコードが設定されていません。"
            );
        var fragmentSource =
            program.FragmentShaderSource
            ?? throw new InvalidOperationException(
                "フラグメントシェーダーのソースコードが設定されていません。"
            );

        var id = shaders.Compile(vertexSource, fragmentSource, "custom");
        program.SetCompiledData(id, OnDispose);
    }

    private void OnDispose(ShaderProgram program)
    {
        pipelines.InvalidateShader(program.Handle);
        shaders.Destroy(program.Handle);
    }
}
