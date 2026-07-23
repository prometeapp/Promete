using System;
using Silk.NET.Core.Native;
using Silk.NET.Shaderc;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// shaderc による GLSL → SPIR-V のランタイムコンパイルを提供します。
/// </summary>
internal sealed unsafe class VulkanShaderCompiler : IDisposable
{
    private readonly Shaderc _shaderc = Shaderc.GetApi();
    private readonly Compiler* _compiler;
    private bool _disposed;

    public VulkanShaderCompiler()
    {
        _compiler = _shaderc.CompilerInitialize();
        if (_compiler == null)
            throw new InvalidOperationException("shaderc コンパイラの初期化に失敗しました。");
    }

    /// <summary>
    /// GLSL ソースコードを SPIR-V にコンパイルします。
    /// </summary>
    /// <param name="source">Vulkan 方言の GLSL ソースコード。</param>
    /// <param name="kind">シェーダーステージ。</param>
    /// <param name="name">エラーメッセージに使用する名前。</param>
    /// <returns>SPIR-V バイトコード。</returns>
    public byte[] Compile(string source, ShaderKind kind, string name)
    {
        var options = _shaderc.CompileOptionsInitialize();
        _shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan12);

        var result = _shaderc.CompileIntoSpv(
            _compiler,
            source,
            (nuint)System.Text.Encoding.UTF8.GetByteCount(source),
            kind,
            name,
            "main",
            options
        );

        try
        {
            var status = _shaderc.ResultGetCompilationStatus(result);
            if (status != CompilationStatus.Success)
            {
                var message = SilkMarshal.PtrToString(
                    (nint)_shaderc.ResultGetErrorMessage(result)
                );
                throw new InvalidOperationException(
                    $"シェーダーのコンパイルエラー ({name}): {message}"
                );
            }

            var length = _shaderc.ResultGetLength(result);
            var bytes = new byte[length];
            fixed (byte* dst = bytes)
            {
                System.Buffer.MemoryCopy(_shaderc.ResultGetBytes(result), dst, (long)length, (long)length);
            }

            return bytes;
        }
        finally
        {
            _shaderc.ResultRelease(result);
            _shaderc.CompileOptionsRelease(options);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _shaderc.CompilerRelease(_compiler);
        _shaderc.Dispose();
    }
}
