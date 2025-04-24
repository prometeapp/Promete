using Promete.Windowing.Headless;

namespace Promete.Headless;

/// <summary>
/// Prometeアプリケーションを、ヘッドレス（ウィンドウなし）で実行するための拡張クラス
/// </summary>
public static class HeadlessAppExtesion
{
    /// <summary>
    /// ヘッドレスモードでPrometeアプリケーションをビルドします
    /// </summary>
    /// <param name="builder">Prometeアプリケーションビルダー</param>
    /// <returns>構築されたPrometeアプリケーション</returns>
    public static PrometeApp BuildWithHeadless(this PrometeApp.PrometeAppBuilder builder)
    {
        return builder.Build<HeadlessWindow>();
    }
}
