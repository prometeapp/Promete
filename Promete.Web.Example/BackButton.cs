using System.Drawing;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Web.Example;

/// <summary>
/// 画面の右上に「戻る」ボタンを出し、押すとデモの一覧に戻るプラグイン。
/// キーボードの無い端末 (スマートフォンなど) では、デモから Esc で戻れないので、代わりに使う。
/// </summary>
/// <remarks>
/// ゲームのシーンではなく、アプリ全体に置く部品なので、プラグインとして <c>Use&lt;BackButton&gt;()</c> で登録する。
/// 一覧の画面 (<see cref="Promete.Example.MainScene" />) では隠れる。
/// </remarks>
public class BackButton(PrometeApp app, Mouse mouse) : IInitializable
{
    private const int Width = 64;
    private const int Height = 32;
    private const int Margin = 8;

    private readonly Container _root = [];

    public void OnStart()
    {
        _root.Add(
            Shape.CreateRect(0, 0, Width, Height, Color.FromArgb(200, 20, 24, 40), 1, Color.White)
        );
        _root.Add(new Text("戻る", Font.GetDefault(16), Color.White).Location(12, 8));
        _root.IsVisible = false;
        app.GlobalForeground.Add(_root);

        app.SceneWillChange += e =>
            _root.IsVisible = e.Next is not (null or Promete.Example.MainScene);
        app.Update += OnUpdate;
    }

    private void OnUpdate()
    {
        // ウィンドウの大きさが変わっても右上に付くよう、毎フレーム置き直す
        var x = app.View.Width - Width - Margin;
        _root.Location = (x, Margin);

        if (!_root.IsVisible || !mouse[MouseButtonType.Left].IsButtonDown)
            return;

        var p = mouse.Position;
        if (p.X < x || p.X >= x + Width || p.Y < Margin || p.Y >= Margin + Height)
            return;

        app.NextFrame(() => app.LoadScene<Promete.Example.MainScene>());
    }
}
