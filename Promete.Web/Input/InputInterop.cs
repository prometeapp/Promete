using System.Runtime.InteropServices.JavaScript;

namespace Promete.Web.Input;

/// <summary>
/// <c>input.js</c> との橋渡しです。
/// </summary>
internal static partial class InputInterop
{
    /// <summary>
    /// 指定した canvas と window に、入力イベントの受け取りを登録します。
    /// </summary>
    /// <param name="selector">canvas を指す CSS セレクタ。</param>
    [JSImport("attach", PrometeWeb.InputModule)]
    internal static partial void Attach(string selector);

    /// <summary>
    /// 前回の呼び出し以降に溜まった入力イベントを取り出します。
    /// </summary>
    /// <returns>発生した順のイベント。</returns>
    [JSImport("drain", PrometeWeb.InputModule)]
    internal static partial string[] Drain();
}
