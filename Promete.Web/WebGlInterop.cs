using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;

namespace Promete.Web;

/// <summary>
/// Emscripten の WebGL2 (GLES3) を Silk.NET から使うための、native/promete_web.c との橋渡しです。
/// </summary>
internal static class WebGlInterop
{
    /// <summary>canvas に WebGL2 コンテキストを作って current にします。失敗時は 0 以下を返します。</summary>
    internal static int CreateContext(string selector) => CreateContextNative(selector);

    [DllImport("promete_web", EntryPoint = "promete_web_create_context")]
    private static extern int CreateContextNative(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string selector
    );

    [DllImport("promete_web", EntryPoint = "promete_web_get_proc")]
    private static extern nint GetProc([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    internal sealed class NativeContext : INativeContext
    {
        public nint GetProcAddress(string proc, int? slot = null) => GetProc(proc);

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = GetProc(proc);
            return addr != 0;
        }

        public void Dispose() { }
    }
}
