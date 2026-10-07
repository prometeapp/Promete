using System.Runtime.InteropServices.JavaScript;
using Promete;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Experimental.Wasm;
using Promete.Experimental.Wasm.Web;
using Promete.Input;
using Promete.Windowing;

/// <summary>
/// Promete の GL ランナーなどが、ブラウザ (WebGL2) 上で動くかを確かめる PoC。
/// </summary>
public static partial class Program
{
    private static PrometeApp? app;

    public static void Main() { }

    /// <summary>
    /// Promete を起動する。成功時は "ok" で始まる文字列、失敗時は例外の内容を返す。
    /// </summary>
    /// <param name="canvasSelector">描画先の canvas を指す CSS セレクタ。</param>
    /// <param name="scene">起動するシーン (main / feature)。</param>
    /// <param name="baseAddress">ページの URL (HttpClient の基準アドレス)。</param>
    /// <param name="features">feature シーンで有効にする機能名 (カンマ区切り。空なら全部)。</param>
    [JSExport]
    public static string Start(
        string canvasSelector,
        string scene,
        string features,
        string baseAddress
    )
    {
        try
        {
            WebBackend.CanvasSelector = canvasSelector;
            MiscScene.BaseAddress = baseAddress;
            foreach (var f in features.Split(',', StringSplitOptions.RemoveEmptyEntries))
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
                .BuildWithWeb(WindowOptions.Default with { Title = "Promete WASM PoC" });

            if (scene == "feature")
                app.Run<FeatureScene>();
            else if (scene == "text")
                app.Run<TextScene>();
            else if (scene == "input")
                app.Run<InputScene>();
            else if (scene == "audio")
                app.Run<AudioScene>();
            else if (scene == "misc")
                app.Run<MiscScene>();
            else if (scene == "example")
                app.Run<Promete.Example.MainScene>();
            else
                app.Run<MainScene>();

            var failures = FeatureScene.Failures.Select(kv => $"FAIL {kv.Key}: {kv.Value}");
            return string.Join('\n', new[] { "ok" }.Concat(failures));
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    /// <summary>Promete.Example のデモを、フォルダを含むパス (例: graphics/pieSprite.demo) で列挙する。</summary>
    /// <returns>デモのパスの一覧。</returns>
    [JSExport]
    public static string[] ListDemos()
    {
        var list = new List<string>();
        CollectDemos(DemoKernel.FileSystem.Root, string.Empty, list);
        return [.. list];
    }

    /// <summary>指定したデモを次のフレームで開く。</summary>
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

    /// <summary>
    /// ページが fetch したアセットを、仮想ファイルシステムに書き込む。
    /// </summary>
    /// <param name="path">書き込み先のパス (例: /assets/ichigo.png)。</param>
    /// <param name="data">ファイルの内容。</param>
    [JSExport]
    public static void WriteAsset(string path, byte[] data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(path, data);
    }

    /// <summary>Web Audio 用の JavaScript モジュールを読み込む。</summary>
    /// <param name="moduleUrl">webAudio.js の絶対 URL。</param>
    /// <returns>読み込みの完了を表すタスク。</returns>
    [JSExport]
    public static Task ImportAudioModule(string moduleUrl) =>
        WebAudioOutput.ImportModule(moduleUrl);

    /// <summary>その他の検証シーンの結果を返す。</summary>
    /// <param name="baseAddress">ページの URL (HttpClient の基準アドレス)。</param>
    /// <returns>結果の文字列。</returns>
    [JSExport]
    public static string MiscReport(string baseAddress)
    {
        MiscScene.BaseAddress = baseAddress;
        return MiscScene.Report;
    }

    /// <summary>オーディオ検証シーンの結果を返す。</summary>
    /// <returns>結果の文字列。</returns>
    [JSExport]
    public static string AudioReport() => AudioScene.Report;

    /// <summary>DOM のキーイベントを入力に反映する。</summary>
    /// <param name="code"><c>KeyboardEvent.code</c>。</param>
    /// <param name="down">押下なら true。</param>
    [JSExport]
    public static void OnKey(string code, bool down)
    {
        var key = WebKeyMap.ToKey(code);
        if (down)
            WebInputContext.Instance.Keyboard.Press(key);
        else
            WebInputContext.Instance.Keyboard.Release(key);
    }

    /// <summary>DOM の文字入力を反映する。</summary>
    /// <param name="text">入力された文字列。</param>
    [JSExport]
    public static void OnChar(string text)
    {
        foreach (var c in text)
            WebInputContext.Instance.Keyboard.Type(c);
    }

    /// <summary>マウスの移動を反映する。</summary>
    /// <param name="x">canvas 上の X 座標。</param>
    /// <param name="y">canvas 上の Y 座標。</param>
    [JSExport]
    public static void OnMouseMove(double x, double y) =>
        WebInputContext.Instance.Mouse.Move((float)x, (float)y);

    /// <summary>マウスボタンの押下 / 解放を反映する。</summary>
    /// <param name="button"><c>MouseEvent.button</c>。</param>
    /// <param name="down">押下なら true。</param>
    [JSExport]
    public static void OnMouseButton(int button, bool down) =>
        WebInputContext.Instance.Mouse.Button(WebKeyMap.ToMouseButton(button), down);

    /// <summary>ホイールを反映する。</summary>
    /// <param name="dx">横方向の移動量。</param>
    /// <param name="dy">縦方向の移動量。</param>
    [JSExport]
    public static void OnWheel(double dx, double dy) =>
        WebInputContext.Instance.Mouse.Wheel((float)dx, (float)dy);

    /// <summary>Canvas2D によるグリフ描画用の JavaScript モジュールを読み込む。</summary>
    /// <param name="moduleUrl">canvasGlyph.js の絶対 URL。</param>
    /// <returns>読み込みの完了を表すタスク。</returns>
    [JSExport]
    public static Task ImportGlyphModule(string moduleUrl) =>
        CanvasGlyphSource.ImportModule(moduleUrl);

    /// <summary>requestAnimationFrame から毎フレーム呼ばれる。</summary>
    [JSExport]
    public static string Frame(double timeMs)
    {
        try
        {
            WebBackend.Current?.Frame(timeMs);
            return string.Empty;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}
