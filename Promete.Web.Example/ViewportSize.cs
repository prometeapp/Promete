using System.Runtime.InteropServices.JavaScript;

namespace Promete.Web.Example;

/// <summary>
/// ゲーム画面の大きさを、ブラウザのビューポート (ウィンドウの表示領域) に合わせ続けるプラグイン。
/// canvas は等倍のまま、画面の大きさそのものを変えるので、拡大縮小によるにじみや、マウス座標のずれは起きない。
/// </summary>
/// <remarks>
/// 一覧の画面は大きさに追従するが、各デモは 640x480 前提の座標で書かれているものが多い。
/// ビューポートが小さいと、そのようなデモは見切れる。
/// </remarks>
public class ViewportSize(PrometeApp app) : IInitializable
{
    private readonly JSObject _window = JSHost.GlobalThis;

    public void OnStart()
    {
        Apply();
        app.Update += Apply;
    }

    private void Apply()
    {
        var width = Math.Max(1, _window.GetPropertyAsInt32("innerWidth"));
        var height = Math.Max(1, _window.GetPropertyAsInt32("innerHeight"));

        // 同じ値の再設定は View 側で無視されるので、毎フレーム呼んでよい
        app.View.Size = (width, height);
    }
}
