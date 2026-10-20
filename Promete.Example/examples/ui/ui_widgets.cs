using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Example.examples.ui;

[Demo(
    "ui/ui_widgets.demo",
    "全面のタブバーに、プログレスバー・ラジオボタン・ドロップダウン・複数行テキスト入力・入れ子のタブバーを並べたデモ"
)]
public class UIWidgetsDemoScene(Keyboard keyboard, UIManager uiManager) : Scene
{
    private const int Margin = 12;
    private const int FooterHeight = 52;

    private Text statusText = null!;
    private Text helpText = null!;
    private TabBar tabBar = null!;
    private ProgressBar loadingBar = null!;
    private Text loadingLabel = null!;
    private VectorInt lastViewSize;

    public override void OnStart()
    {
        statusText = new Text("各要素を操作してみよう！", color: Color.Yellow);
        helpText = new Text(
            "[Tab/方向キー] フォーカス移動  [Enter/Space] 決定  [ESC] 終了",
            color: Color.Gray
        );

        tabBar = new TabBar().Location(Margin, Margin);
        tabBar
            .AddTab("ProgressBar", CreateProgressPage())
            .AddTab("RadioGroup / Dropdown", CreateSelectionPage())
            .AddTab("TextInput", CreateTextInputPage())
            .AddTab("TabBar", CreateNestedTabPage());
        tabBar.SelectionChanged += i =>
            statusText.Content = $"タブ: {tabBar.GetTabButton(i).TextContent}";

        Root.Add(tabBar);
        Root.Add(statusText);
        Root.Add(helpText);

        FitToWindow();
        uiManager.SetFocus(tabBar.GetTabButton(0));
    }

    public override void OnUpdate()
    {
        loadingBar.Value = (loadingBar.Value + App.Time.DeltaTime * 0.25f) % 1.0001f;
        loadingLabel.Content = $"{loadingBar.Fraction:P0}";

        if (!View.Size.Equals(lastViewSize))
            FitToWindow();

        // ドロップダウンを閉じた Esc では、シーンを抜けない
        if (keyboard.Escape.IsKeyDown && !uiManager.IsCancelConsumed)
            App.LoadScene<MainScene>();
    }

    /// <summary>
    /// タブバーをウィンドウいっぱいに広げ、下に状態表示と操作説明を置きます。
    /// </summary>
    private void FitToWindow()
    {
        var view = View.Size;
        lastViewSize = view;

        tabBar.Size = (
            Math.Max(200, view.X - Margin * 2),
            Math.Max(120, view.Y - Margin * 2 - FooterHeight)
        );
        statusText.Location = new Vector(Margin, view.Y - FooterHeight + 6);
        helpText.Location = new Vector(Margin, view.Y - 24);
    }

    private Container CreateProgressPage()
    {
        loadingBar = new ProgressBar().Size(300, 16);
        loadingLabel = new Text("0%", color: Color.White);

        var manualBar = new ProgressBar(0, 10, 3).Size(300, 16);
        var manualSlider = new Slider(0, 10, 3).Size(300, 24);
        manualSlider.ValueChanged += v => manualBar.Value = v;

        return Page(
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
    }

    private Container CreateSelectionPage()
    {
        var quality = new RadioGroup(["低", "中", "高"]);
        quality.SelectedIndex = 1;
        quality.SelectionChanged += i => statusText.Content = $"画質: {quality.SelectedItem}";

        var difficulty = new RadioGroup(
            ["かんたん", "ふつう", "むずかしい"],
            StackDirection.Horizontal
        );
        difficulty.SelectionChanged += i =>
            statusText.Content = $"難易度: {difficulty.SelectedItem}";

        var language = new Dropdown("言語を選択").Size(240, 34);
        language.AddItem("日本語").AddItem("English").AddItem("한국어").AddItem("中文");
        language.SelectionChanged += i => statusText.Content = $"言語: {language.SelectedItem}";

        return Page(
            Label("画質 (縦並び)"),
            quality,
            Label("難易度 (横並び)"),
            difficulty,
            Label("言語 (ドロップダウン)"),
            language
        );
    }

    private Container CreateTextInputPage()
    {
        var single = new TextInput("1 行の入力...").Size(360, 32);
        single.Submitted += t => statusText.Content = $"確定: {t}";

        // 高さは Rows から決まる (0 を指定すると Rows に従う)
        var memo = new TextInput("複数行の入力 (Ctrl+Enter で確定)...") { Rows = 5 }.Size(360, 0);
        memo.Submitted += t => statusText.Content = $"確定: {t.Replace("\n", " / ")}";

        WrapMode[] wrapModes = [WrapMode.Mixed, WrapMode.Word, WrapMode.Character, WrapMode.None];
        var wrap = new RadioGroup(wrapModes.Select(m => m.ToString()), StackDirection.Horizontal);
        wrap.SelectedIndex = 0;
        wrap.SelectionChanged += i =>
        {
            if (i < 0)
                return;
            memo.WrapMode = wrapModes[i];
            statusText.Content = $"折り返し: {wrapModes[i]}";
        };

        return Page(
            Label("1 行"),
            single,
            Label("複数行 (Rows = 5)"),
            memo,
            Label("折り返し (WrapMode)"),
            wrap
        );
    }

    private Container CreateNestedTabPage()
    {
        // タブバーの中に、さらにタブバーを置ける
        var inner = new TabBar().Size(420, 170);
        inner
            .AddTab(
                "設定",
                Page(
                    new Checkbox("全画面表示").Size(220, 24),
                    new Checkbox("垂直同期", isChecked: true).Size(220, 24)
                )
            )
            .AddTab(
                "情報",
                Page(
                    Label("入れ子のタブバーです"),
                    new Button("OK")
                        .Size(100, 32)
                        .OnClick(() => statusText.Content = "OK が押された")
                )
            );
        inner.SelectionChanged += i =>
            statusText.Content = $"内側のタブ: {inner.GetTabButton(i).TextContent}";

        return Page(Label("入れ子にしたタブバー"), inner);
    }

    private static Text Label(string text) => new(text, color: Color.LightGray);

    /// <summary>
    /// 項目を縦に並べたページを作ります。
    /// </summary>
    private static StackContainer Page(params Node[] items)
    {
        var page = new StackContainer(StackDirection.Vertical, gap: 10);
        foreach (var item in items)
            page.Add(item);
        return page;
    }
}
