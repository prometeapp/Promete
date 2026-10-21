using System.Runtime.InteropServices.JavaScript;

namespace Promete.Web;

/// <summary>
/// ブラウザで Promete を動かすための入口です。
/// </summary>
/// <remarks>
/// ページの JavaScript (<c>startPromete</c>) がランタイムを起動し、アセットを配置してから <c>Main</c> を呼びます。
/// <c>Main</c> では、<see cref="InitializeAsync"/> の後に <see cref="WebAppExtension.BuildWithWeb"/> でアプリを構築し、
/// <c>Run</c> を呼びます。<c>Run</c> はすぐに戻り、以降はブラウザの描画ループがゲームを進めます。
/// </remarks>
public static partial class PrometeWeb
{
    internal const string MainModule = "promete";
    internal const string InputModule = "promete-input";
    internal const string AudioModule = "promete-audio";
    internal const string GlyphModule = "promete-glyph";

    private const string ContentPath = "_content/Promete.Web/";

    /// <summary>
    /// Promete.Web が使う JavaScript モジュールを読み込みます。アプリを構築する前に 1 回呼び出してください。
    /// </summary>
    /// <returns>読み込みの完了を表すタスク。</returns>
    public static async Task InitializeAsync()
    {
        var baseUri = new Uri(
            JSHost.GlobalThis.GetPropertyAsJSObject("document")!.GetPropertyAsString("baseURI")!
        );

        await Task.WhenAll(
            JSHost.ImportAsync(MainModule, ResolveModule(baseUri, "promete.js")),
            JSHost.ImportAsync(InputModule, ResolveModule(baseUri, "input.js")),
            JSHost.ImportAsync(AudioModule, ResolveModule(baseUri, "audio.js")),
            JSHost.ImportAsync(GlyphModule, ResolveModule(baseUri, "glyph.js"))
        );
    }

    /// <summary>
    /// ブラウザの描画ループから毎フレーム呼ばれ、ゲームを 1 フレーム進めます。
    /// </summary>
    /// <param name="timeMs"><c>requestAnimationFrame</c> が渡すタイムスタンプ (ミリ秒)。</param>
    /// <returns>
    /// 続ける場合は <see langword="null"/>、ゲームが終了した場合は空文字列、例外が起きた場合はその内容。
    /// </returns>
    [JSExport]
    internal static string? Frame(double timeMs)
    {
        try
        {
            var backend = WebBackend.Current;
            if (backend is null || !backend.Frame(timeMs))
                return string.Empty;
            return null;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    /// <summary>
    /// 仮想ファイルシステムのルートに置かれたアセットパッケージから、フォントを取り出します。
    /// <c>promete.js</c> のプリロードが、<c>Main</c> より前に呼びます。
    /// </summary>
    /// <param name="location">パッケージのファイル名 (ルートからの相対パス)。</param>
    /// <returns>取り出したフォントのファイルの絶対パス。<c>promete.js</c> は、これをファミリー名として <c>FontFace</c> に登録します。</returns>
    [JSExport]
    internal static string[] PreparePackageFonts(string location)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// アセンブリに埋め込んだアセットパッケージから、フォントを取り出します。
    /// <c>promete.js</c> のプリロードが、<c>Main</c> より前に呼びます。
    /// </summary>
    /// <param name="assemblyName">リソースを持つアセンブリの名前。</param>
    /// <param name="resourceName">埋め込みリソースの名前。</param>
    /// <returns>取り出したフォントのファイルの絶対パス。</returns>
    [JSExport]
    internal static string[] PrepareEmbeddedPackageFonts(string assemblyName, string resourceName)
    {
        throw new NotImplementedException();
    }

    [JSImport("startLoop", MainModule)]
    internal static partial void StartLoop();

    private static string ResolveModule(Uri baseUri, string fileName) =>
        new Uri(baseUri, ContentPath + fileName).AbsoluteUri;
}
