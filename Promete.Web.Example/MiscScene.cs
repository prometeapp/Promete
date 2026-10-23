using System.Collections;
using System.Drawing;
using Promete;
using Promete.Coroutines;
using Promete.Graphics.Fonts;
using Promete.Nodes;

namespace Promete.Web.Example;

/// <summary>
/// その他の検証用シーン。既定フォント、コルーチン、HttpClient による目次の取得、メインスレッド判定、View の操作を調べる。
/// </summary>
public class MiscScene(CoroutineManager coroutine) : Scene
{
    private static readonly List<string> Lines = [];

    /// <summary>検証結果を JavaScript から読むための文字列。</summary>
    public static string Report => string.Join('\n', Lines);

    /// <summary>ページの URL (HttpClient の基準アドレス)。JavaScript から設定される。</summary>
    public static string BaseAddress { get; set; } = "http://localhost/";

    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        Check("Font.GetDefault()", () => Font.GetDefault().Metrics.ToString());
        Check("SystemFonts count", () => SystemFonts.Fonts.Count.ToString());
        Check(
            "View.Title set",
            () =>
            {
                View.Title = "changed";
                return View.Title;
            }
        );
        Check(
            "View.Size set (canvas は追従しない)",
            () =>
            {
                View.Size = (320, 240);
                return View.Size.ToString();
            }
        );
        Check("IsMainThread", () => App.IsMainThread().ToString());

        coroutine.Start(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(0.3f);
        Lines.Add("coroutine WaitForSeconds(0.3): ok");

        var delay = Task.Delay(200);
        yield return new WaitForTask(delay);
        Lines.Add("coroutine WaitForTask(Task.Delay): ok");

        // Package モードではばらのアセットを配信しないので、どちらのモードでも配信される目次を取得する
        var http = new HttpClient { BaseAddress = new Uri(BaseAddress) };
        var request = http.GetStreamAsync("promete-assets.json");
        yield return new WaitForTask(request);
        if (request.IsFaulted)
        {
            Lines.Add(
                "HttpClient GET promete-assets.json: "
                    + request.Exception!.GetBaseException().GetType().Name
                    + " "
                    + request.Exception.GetBaseException().Message
            );
            yield break;
        }

        using var manifest = request.Result;
        Lines.Add("HttpClient GET promete-assets.json: ok");

        Task.Run(() => Lines.Add("Task.Run: ok (IsMainThread=" + App.IsMainThread() + ")"));
        yield return new WaitForSeconds(0.3f);
        Lines.Add("done");
    }

    private static void Check(string name, Func<string> action)
    {
        try
        {
            Lines.Add($"{name}: {action()}");
        }
        catch (Exception e)
        {
            Lines.Add($"{name}: FAIL {e.GetType().Name} {e.Message}");
        }
    }
}
