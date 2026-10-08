using System.Runtime.InteropServices.JavaScript;

namespace Promete.Web;

/// <summary>
/// canvas とドキュメントを操作する <c>promete.js</c> の関数との橋渡しです。
/// </summary>
internal static partial class CanvasInterop
{
    /// <summary>
    /// canvas の描画バッファと表示サイズを設定します。
    /// </summary>
    /// <param name="selector">canvas を指す CSS セレクタ。</param>
    /// <param name="width">幅 (ピクセル)。</param>
    /// <param name="height">高さ (ピクセル)。</param>
    [JSImport("setCanvasSize", PrometeWeb.MainModule)]
    internal static partial void SetCanvasSize(string selector, int width, int height);

    /// <summary>
    /// ドキュメントのタイトルを設定します。
    /// </summary>
    /// <param name="title">タイトル。</param>
    [JSImport("setTitle", PrometeWeb.MainModule)]
    internal static partial void SetTitle(string title);
}
