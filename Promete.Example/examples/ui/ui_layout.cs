using System.Drawing;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Example.examples.ui;

[Demo(
    "ui/ui_layout.demo",
    "StackContainerでレイアウトした設定画面 — 入れ子のスタックでもTabと方向キーで全体を移動できる"
)]
public class UILayoutDemoScene(Keyboard keyboard, UIManager uiManager) : Scene
{
    private Text statusText = null!;

    public override void OnStart()
    {
        uiManager.SpatialNavigation = true;

        statusText = new Text("項目を操作してみよう！", color: Color.Yellow);

        // --- プロフィール ---
        var nameInput = new TextInput("名前を入力...").Size(240, 34);
        nameInput.Submitted += t => statusText.Content = $"名前を確定: {t}";

        var newsCheckbox = new Checkbox("お知らせを受け取る", isChecked: true).Size(240, 24);
        newsCheckbox.CheckedChanged += v =>
            statusText.Content = $"お知らせ: {(v ? "受け取る" : "受け取らない")}";

        var languageDropdown = new Dropdown("言語を選択").Size(240, 34);
        languageDropdown.AddItem("日本語").AddItem("English").AddItem("한국어").AddItem("中文");
        languageDropdown.SelectedIndex = 0;
        languageDropdown.SelectionChanged += i =>
            statusText.Content = $"言語: {languageDropdown.SelectedItem}";

        var profileCard = CreateCard(
            "プロフィール",
            Label("名前"),
            nameInput,
            Label("言語"),
            languageDropdown,
            Label("通知"),
            newsCheckbox
        );

        // --- サウンド ---
        var bgmSlider = new Slider(0, 10, 7).Size(240, 28);
        bgmSlider.ValueChanged += v => statusText.Content = $"BGM: {v:F0}";

        var seSlider = new Slider(0, 10, 5).Size(240, 28);
        seSlider.ValueChanged += v => statusText.Content = $"SE: {v:F0}";

        var muteCheckbox = new Checkbox("ミュート").Size(240, 24);
        muteCheckbox.CheckedChanged += v => statusText.Content = $"ミュート: {(v ? "ON" : "OFF")}";

        var soundCard = CreateCard(
            "サウンド",
            Label("BGM"),
            bgmSlider,
            Label("SE"),
            seSlider,
            muteCheckbox
        );

        // --- ボタン列 ---
        var buttons = new StackContainer(StackDirection.Horizontal, gap: 12)
        {
            new Button("保存").Size(110, 40).OnClick(() => statusText.Content = "保存しました"),
            new Button("初期値に戻す")
                .Size(150, 40)
                .OnClick(() =>
                {
                    bgmSlider.Value = 7;
                    seSlider.Value = 5;
                    muteCheckbox.IsChecked = false;
                    statusText.Content = "初期値に戻しました";
                }),
            new Button("戻る").Size(110, 40).OnClick(() => App.LoadScene<MainScene>()),
        };

        // --- 画面全体: 縦スタックの中に、横スタックとボタン列を入れ子にする ---
        var screen = new StackContainer(StackDirection.Vertical, gap: 16, padding: 24)
        {
            new Text("設定", color: Color.White),
            new StackContainer(StackDirection.Horizontal, gap: 16) { profileCard, soundCard },
            buttons,
            statusText,
            new Text(
                "[Tab/方向キー] フォーカス移動  [Enter/Space] 押下  [ESC] 終了",
                color: Color.Gray
            ),
        };
        Root.Add(screen);

        uiManager.SetFocus(nameInput);
    }

    public override void OnUpdate()
    {
        // ドロップダウンを閉じた Esc では、シーンを抜けない
        if (keyboard.Escape.IsKeyDown && !uiManager.IsCancelConsumed)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        uiManager.SpatialNavigation = false;
    }

    private static Text Label(string text) => new(text, color: Color.LightGray);

    /// <summary>
    /// タイトルと項目を縦に並べ、背景パネルで囲んだカードを作ります。
    /// 項目に NavigationOrder や FocusGroup は指定しません。ツリーの順にフォーカスが移動します。
    /// </summary>
    private static Panel CreateCard(string title, params Node[] items)
    {
        var content = new StackContainer(StackDirection.Vertical, gap: 8, padding: 14)
        {
            new Text(title, color: Color.White),
        };
        foreach (var item in items)
            content.Add(item);

        var card = new Panel { AutoSize = true };
        card.Add(content);
        return card;
    }
}
