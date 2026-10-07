using System.Runtime.InteropServices.JavaScript;
using System.Web;
using Promete;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Web;
using Promete.Web.Example;
using Promete.Windowing;

/// <summary>
/// Promete.Web のサンプル。Promete.Example のデモと、Web 向けの検証用シーンを動かす。
/// </summary>
/// <remarks>
/// クエリ: <c>?scene=example|feature|text|input|audio|misc|bench</c> (既定は example)、
/// <c>&amp;demo=&lt;パス&gt;</c> (例: <c>graphics/font.demo</c>)、<c>&amp;f=&lt;機能名&gt;</c> (feature シーン用)。
/// </remarks>
public static partial class Program
{
    private static PrometeApp? app;

    public static async Task Main()
    {
        await PrometeWeb.InitializeAsync();

        var location = JSHost.GlobalThis.GetPropertyAsJSObject("location")!;
        var query = HttpUtility.ParseQueryString(
            location.GetPropertyAsString("search") ?? string.Empty
        );
        MiscScene.BaseAddress = location.GetPropertyAsString("href")!;
        foreach (
            var f in (query["f"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
        )
            FeatureScene.Enabled.Add(f.Trim());

        app = PrometeApp
            .Create()
            .UseScenesFrom<MainScene>()
            .UseScenesFrom<Promete.Example.MainScene>()
            .Use<Keyboard>()
            .Use<Mouse>()
            .Use<CoroutineManager>()
            .Use<Gamepads>()
            .Use<ConsoleLayer>()
            .BuildWithWeb(WindowOptions.Default with { Title = "Promete.Web Example" });

        switch (query["scene"])
        {
            case "feature":
                app.Run<FeatureScene>();
                break;
            case "text":
                app.Run<TextScene>();
                break;
            case "input":
                app.Run<InputScene>();
                break;
            case "audio":
                app.Run<AudioScene>();
                break;
            case "misc":
                app.Run<MiscScene>();
                break;
            case "bench":
                app.Run<BenchScene>();
                break;
            default:
                app.Run<Promete.Example.MainScene>();
                break;
        }

        if (query["demo"] is { } demo)
            OpenDemo(demo);
    }

    /// <summary>Promete.Example のデモを、フォルダを含むパス (例: graphics/pieSprite.demo) で列挙する。スモークテスト用。</summary>
    /// <returns>デモのパスの一覧。</returns>
    [JSExport]
    public static string[] ListDemos()
    {
        var list = new List<string>();
        CollectDemos(DemoKernel.FileSystem.Root, string.Empty, list);
        return [.. list];
    }

    /// <summary>指定したデモを次のフレームで開く。スモークテスト用。</summary>
    /// <param name="path"><see cref="ListDemos"/> が返したパス。</param>
    /// <returns>見つかったら true。</returns>
    [JSExport]
    public static bool OpenDemo(string path)
    {
        var file = FindDemo(DemoKernel.FileSystem.Root, string.Empty, path);
        if (file is null || app is null)
            return false;

        app.NextFrame(() => app.LoadScene(file.Scene));
        return true;
    }

    /// <summary>検証用シーンの結果を返す。スモークテスト用。</summary>
    /// <returns>結果の文字列。</returns>
    [JSExport]
    public static string Report() =>
        $"space={InputScene.SpaceDownCount} audio={AudioScene.Report} misc={MiscScene.Report} "
        + string.Join(' ', FeatureScene.Failures.Select(kv => $"FAIL {kv.Key}: {kv.Value}"));

    private static void CollectDemos(Folder folder, string prefix, List<string> list)
    {
        foreach (var item in folder.Files)
        {
            if (item is Folder sub)
                CollectDemos(sub, prefix + sub.Name + "/", list);
            else if (item is SceneFile file)
                list.Add(prefix + file.Name);
        }
    }

    private static SceneFile? FindDemo(Folder folder, string prefix, string path)
    {
        foreach (var item in folder.Files)
        {
            if (item is Folder sub && FindDemo(sub, prefix + sub.Name + "/", path) is { } found)
                return found;
            if (item is SceneFile file && prefix + file.Name == path)
                return file;
        }

        return null;
    }
}
