using Promete.Backends.Headless;
using Promete.Windowing;

namespace Promete.Headless;

/// <summary>
/// Prometeアプリケーションを、ヘッドレス（ウィンドウなし）で実行するための拡張クラス
/// </summary>
public static class HeadlessAppExtension
{
    /// <summary>
    /// ヘッドレスモードでPrometeアプリケーションをビルドします
    /// </summary>
    /// <param name="builder">Prometeアプリケーションビルダー</param>
    /// <param name="opts">ウィンドウオプション</param>
    /// <returns>構築されたPrometeアプリケーション</returns>
    public static PrometeApp BuildWithHeadless(this PrometeApp.PrometeAppBuilder builder, WindowOptions? opts = null)
    {
        return builder.Build<HeadlessBackend>(opts);
    }
}
