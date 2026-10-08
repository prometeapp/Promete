using Promete.Backends.GL;
using Promete.Windowing;

namespace Promete.GLDesktop;

/// <summary>
/// OpenGLを使用したデスクトップアプリケーション用の拡張機能を提供するクラスです。
/// </summary>
public static class OpenGLDesktopAppExtension
{
    /// <summary>
    /// PrometeAppをOpenGL デスクトップアプリケーションとして構築します。
    /// </summary>
    /// <param name="builder">PrometeAppのビルダー</param>
    /// <returns>構築されたPrometeAppインスタンス</returns>
    public static PrometeApp BuildWithOpenGLDesktop(
        this PrometeApp.PrometeAppBuilder builder,
        WindowOptions? opts = null
    )
    {
        return builder.BuildWithGLBackend<OpenGLDesktopBackend>(opts);
    }
}
