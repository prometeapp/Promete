using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// Emscripten の WebGL2 (GLES3) を Silk.NET から使うための、gl_shim.c との橋渡しです。
/// </summary>
internal static class WebGlInterop
{
    /// <summary>canvas に WebGL2 コンテキストを作って current にします。失敗時は 0 以下を返します。</summary>
    internal static int CreateContext(string selector) => PocCreateContext(selector);

    [DllImport("gl_shim", EntryPoint = "poc_create_context")]
    private static extern int PocCreateContext(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string selector
    );

    [DllImport("gl_shim", EntryPoint = "poc_get_proc")]
    private static extern nint PocGetProc([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    internal sealed class NativeContext : INativeContext
    {
        public nint GetProcAddress(string proc, int? slot = null) => PocGetProc(proc);

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = PocGetProc(proc);
            return addr != 0;
        }

        public void Dispose() { }
    }
}
