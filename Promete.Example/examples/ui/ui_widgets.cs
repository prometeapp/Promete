using System.Drawing;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Example.examples.ui;

[Demo("ui/ui_widgets.demo", "プログレスバー、ラジオボタン、タブバー、複数行テキスト入力のデモ")]
public class UIWidgetsDemoScene(Keyboard keyboard, UIManager uiManager) : Scene
{
    private Text statusText = null!;
    private ProgressBar loadingBar = null!;
    private Text loadingLabel = null!;

    public override void OnStart()
    {
        statusText = new Text("各要素を操作してみよう！", color: Color.Yellow);

        // --- プログレスバー ---
        loadingBar = new ProgressBar().Size(300, 16);
        loadingLabel = new Text("0%", color: Color.White);

        var manualBar = new ProgressBar(0, 10, 3).Size(300, 16);
        var manualSlider = new Slider(0, 10, 3).Size(300, 24);
        manualSlider.ValueChanged += v => manualBar.Value = v;

        var progressSection = Section(
            "ProgressBar",
            Label("自動で進む (ループ)"),
            new StackContainer(StackDirection.Horizontal, gap: 12, align: StackAlignment.Center)
            {
                loadingBar,
                loadingLabel,
            },
            Label("スライダーに連動"),
            manualBar,
            manualSlider
        );

        var screen = new StackContainer(StackDirection.Vertical, gap: 16, padding: 24)
        {
            new Text("UI 要素のデモ", color: Color.White),
            progressSection,
            statusText,
            new Text("[Tab/方向キー] フォーカス移動  [ESC] 終了", color: Color.Gray),
        };
        Root.Add(screen);
    }

    public override void OnUpdate()
    {
        loadingBar.Value = (loadingBar.Value + App.Time.DeltaTime * 0.25f) % 1.0001f;
        loadingLabel.Content = $"{loadingBar.Fraction:P0}";

        if (keyboard.Escape.IsKeyDown && !uiManager.IsCancelConsumed)
            App.LoadScene<MainScene>();
    }

    private static Text Label(string text) => new(text, color: Color.LightGray);

    /// <summary>
    /// タイトルと項目を縦に並べ、背景パネルで囲んだセクションを作ります。
    /// </summary>
    private static Panel Section(string title, params Node[] items)
    {
        var content = new StackContainer(StackDirection.Vertical, gap: 8, padding: 14)
        {
            new Text(title, color: Color.White),
        };
        foreach (var item in items)
            content.Add(item);

        var section = new Panel { AutoSize = true };
        section.Add(content);
        return section;
    }
}
