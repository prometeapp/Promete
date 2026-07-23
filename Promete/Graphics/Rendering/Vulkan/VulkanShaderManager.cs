using System;
using System.Collections.Generic;
using System.Linq;
using Promete.Internal;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// カスタムシェーダー (Vulkan 方言 GLSL 450) のコンパイル結果を int の ID で管理するテーブルです。
/// <see cref="ShaderProgram.Handle"/> はこのテーブルの ID を指します。
/// </summary>
internal sealed unsafe class VulkanShaderManager(VulkanContext ctx) : IDisposable
{
    private readonly VulkanShaderCompiler _compiler = new();
    private readonly Dictionary<int, VulkanShaderEntry> _shaders = [];
    private int _nextId = 1;
    private bool _disposed;

    /// <summary>
    /// GLSL ソースをコンパイルし、シェーダー ID を返します。
    /// </summary>
    public int Compile(string vertexSource, string fragmentSource, string name)
    {
        var vertSpv = _compiler.Compile(vertexSource, ShaderKind.VertexShader, $"{name}.vert");
        var fragSpv = _compiler.Compile(fragmentSource, ShaderKind.FragmentShader, $"{name}.frag");

        // 両ステージの Uniform ブロックを統合 (規約: set=1, binding=0)
        var blocks = SpirvReflector
            .ReflectUniformBlocks(vertSpv)
            .Concat(SpirvReflector.ReflectUniformBlocks(fragSpv))
            .Where(b => b is { Set: 1, Binding: 0 })
            .ToList();

        SpirvReflector.UniformBlock? merged = null;
        if (blocks.Count > 0)
        {
            var offsets = new Dictionary<string, uint>();
            uint size = 0;
            foreach (var block in blocks)
            {
                foreach (var (memberName, offset) in block.MemberOffsets)
                    offsets[memberName] = offset;
                size = Math.Max(size, block.Size);
            }

            merged = new SpirvReflector.UniformBlock
            {
                Set = 1,
                Binding = 0,
                MemberOffsets = offsets,
                Size = size,
            };
        }

        var unsupported = SpirvReflector
            .ReflectUniformBlocks(fragSpv)
            .FirstOrDefault(b => b is not { Set: 1, Binding: 0 });
        if (unsupported is not null)
        {
            LogHelper.Bug(
                $"カスタムシェーダーの Uniform ブロック (set={unsupported.Set}, binding={unsupported.Binding}) は未対応です。set=1, binding=0 を使用してください。"
            );
        }

        var id = _nextId++;
        _shaders[id] = new VulkanShaderEntry
        {
            VertexModule = CreateModule(vertSpv),
            FragmentModule = CreateModule(fragSpv),
            UniformBlock = merged,
        };
        return id;
    }

    /// <summary>
    /// シェーダー情報を取得します。
    /// </summary>
    public VulkanShaderEntry Get(int id) => _shaders[id];

    /// <summary>
    /// シェーダーが登録されているかを取得します。
    /// </summary>
    public bool Contains(int id) => _shaders.ContainsKey(id);

    /// <summary>
    /// シェーダーを破棄します。実際の破棄はフレーム完了後に遅延されます。
    /// </summary>
    public void Destroy(int id)
    {
        if (!_shaders.Remove(id, out var entry))
            return;

        var vk = ctx.Vk;
        var device = ctx.Device;
        ctx.DeferDestroy(() =>
        {
            vk.DestroyShaderModule(device, entry.VertexModule, null);
            vk.DestroyShaderModule(device, entry.FragmentModule, null);
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var entry in _shaders.Values)
        {
            ctx.Vk.DestroyShaderModule(ctx.Device, entry.VertexModule, null);
            ctx.Vk.DestroyShaderModule(ctx.Device, entry.FragmentModule, null);
        }

        _shaders.Clear();
        _compiler.Dispose();
    }

    private ShaderModule CreateModule(byte[] spirv)
    {
        fixed (byte* code = spirv)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)code,
            };
            var result = ctx.Vk.CreateShaderModule(ctx.Device, in createInfo, null, out var module);
            if (result != Result.Success)
                throw new InvalidOperationException($"シェーダーモジュールの作成に失敗しました: {result}");
            return module;
        }
    }

    /// <summary>
    /// コンパイル済みシェーダーの情報です。
    /// </summary>
    public sealed class VulkanShaderEntry
    {
        /// <summary>頂点シェーダーモジュール。</summary>
        public required ShaderModule VertexModule { get; init; }

        /// <summary>フラグメントシェーダーモジュール。</summary>
        public required ShaderModule FragmentModule { get; init; }

        /// <summary>set=1, binding=0 の Uniform ブロック。存在しない場合 null。</summary>
        public SpirvReflector.UniformBlock? UniformBlock { get; init; }
    }
}
