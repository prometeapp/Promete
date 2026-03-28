using System.Drawing;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Example.examples.ui;

[Demo("ui/ui_demo.demo", "UIライブラリのデモ — ボタン、チェックボックス、スライダー、テキスト入力、スクロール、パネル")]
public class UIDemoScene(Keyboard keyboard, Mouse mouse, UIManager uiManager, ConsoleLayer console) : Scene
{
	private Text statusText = null!;
	private int clickCount;

	public override void OnStart()
	{
		// --- ステータス表示 ---
		statusText = new Text("UI要素をクリック・操作してみよう！", color: Color.White)
			.Location(20, 16);
		Root.Add(statusText);

		// ========== 左カラム ==========

		// --- 基本ボタン ---
		var basicButton = new Button("クリック！")
			.Location(30, 60)
			.Size(180, 45)
			.NavigationOrder(1)
			.OnClick(() =>
			{
				clickCount++;
				statusText.Content = $"クリック回数: {clickCount}";
			});
		Root.Add(basicButton);

		// --- カスタムカラーのボタン ---
		var customStyle = new DefaultButtonStyle
		{
			NormalColor = Color.FromArgb(255, 20, 80, 60),
			HoveredColor = Color.FromArgb(255, 30, 120, 90),
			PressedColor = Color.FromArgb(255, 10, 50, 40),
			BorderColor = Color.FromArgb(255, 60, 180, 130),
			FocusedBorderColor = Color.FromArgb(255, 100, 255, 180),
			TextColor = Color.FromArgb(255, 200, 255, 220),
		};

		var greenButton = new Button("カスタムスタイル")
		{
			Style = customStyle,
		};
		greenButton
			.Location(30, 115)
			.Size(180, 45)
			.NavigationOrder(2)
			.OnClick(() => statusText.Content = "緑のボタンがクリックされた！");
		Root.Add(greenButton);

		// --- 無効化されたボタン ---
		var disabledButton = new Button("無効なボタン")
			.Location(30, 170)
			.Size(180, 45)
			.Enabled(false);
		Root.Add(disabledButton);

		// --- NineSlice ボタン ---
		var nineSliceTex = Window.TextureFactory.Load9Sliced("assets/rect.png", 16, 16, 16, 16);
		var nineSliceStyle = new NineSliceButtonStyle
		{
			NormalTexture = nineSliceTex,
			HoveredTexture = nineSliceTex,
			PressedTexture = nineSliceTex,
			TextColor = Color.White,
		};
		var nineSliceButton = new Button("9-Slice ボタン") { Style = nineSliceStyle };
		nineSliceButton
			.Location(30, 225)
			.Size(180, 50)
			.NavigationOrder(3)
			.OnClick(() => statusText.Content = "NineSlice ボタンがクリックされた！");
		Root.Add(nineSliceButton);

		// --- チェックボックス ---
		var checkLabel = new Text("Checkbox:", color: Color.LightGray)
			.Location(30, 290);
		Root.Add(checkLabel);

		var checkbox1 = new Checkbox("オプション A", isChecked: true)
			.Location(30, 315)
			.Size(200, 24)
			.NavigationOrder(4);
		Root.Add(checkbox1);

		var checkbox2 = new Checkbox("オプション B")
			.Location(30, 345)
			.Size(200, 24)
			.NavigationOrder(5);
		Root.Add(checkbox2);

		checkbox1.CheckedChanged += v => statusText.Content = $"オプション A: {(v ? "ON" : "OFF")}";
		checkbox2.CheckedChanged += v => statusText.Content = $"オプション B: {(v ? "ON" : "OFF")}";

		// --- スライダー ---
		var sliderLabel = new Text("Slider:", color: Color.LightGray)
			.Location(30, 385);
		Root.Add(sliderLabel);

		var slider = new Slider(0, 5, 3)
			.Location(30, 410)
			.Size(200, 28)
			.NavigationOrder(6);
		Root.Add(slider);

		slider.ValueChanged += v => statusText.Content = $"スライダー値: {v:F1}";

		// --- モーダルボタン ---
		var modalButton = new Button("モーダルを開く")
			.Location(30, 460)
			.Size(180, 45)
			.NavigationOrder(7)
			.OnClick(() =>
			{
				var modal = new Modal((280, 160));
                modal.Size = View.Size;

				var label = new Text("モーダルダイアログです", color: Color.White)
					.Location(20, 20);
				modal.Body.Add(label);

				var closeBtn = new Button("閉じる")
					.Location(90, 100)
					.Size(100, 40)
					.OnClick(() => modal.Close());
				modal.Body.Add(closeBtn);

				modal.Show(uiManager, Root);
				statusText.Content = "モーダルが開きました (外側クリックでも閉じます)";
			});
		Root.Add(modalButton);

		// ========== 右カラム ==========

		// --- テキスト入力 ---
		var inputLabel = new Text("TextInput:", color: Color.LightGray)
			.Location(280, 60);
		Root.Add(inputLabel);

		var textInput = new TextInput("入力してください...")
			.Location(280, 85)
			.Size(250, 34)
			.NavigationOrder(10);
		Root.Add(textInput);

		textInput.TextChanged += t => statusText.Content = $"入力中: {t}";
		textInput.Submitted += t => statusText.Content = $"確定: {t}";

		// --- パネル内にボタン群 ---
		var panelLabel = new Text("Panel + FocusGroup:", color: Color.LightGray)
			.Location(280, 135);
		Root.Add(panelLabel);

		var panel = new Panel()
			.Location(280, 160)
			.Size(250, 200);
		Root.Add(panel);

		var focusGroup = new FocusGroup("menu");
		for (var i = 0; i < 4; i++)
		{
			var index = i;
			var menuButton = new Button($"メニュー {i + 1}")
				.Location(10, 10 + i * 46)
				.Size(230, 38)
				.NavigationOrder(20 + i)
				.InFocusGroup(focusGroup)
				.OnClick(() => statusText.Content = $"メニュー {index + 1} が選ばれた！");
			panel.Add(menuButton);
		}

		// --- スクロールビュー ---
		var scrollLabel = new Text("ScrollView (ホイールでスクロール):", color: Color.LightGray)
			.Location(280, 375);
		Root.Add(scrollLabel);

		var scrollView = new ScrollView { ContentSize = (250, 400) };
		scrollView
			.Location(280, 400)
			.Size(250, 120);
		Root.Add(scrollView);

		for (var i = 0; i < 10; i++)
		{
			var item = new Text($"  アイテム {i + 1}", color: Color.FromArgb(255, 180, 200, 255))
				.Location(0, i * 35);
			scrollView.Content.Add(item);
		}

		// --- 操作説明 ---
		var helpText = new Text(
			"[Tab] フォーカス移動  [Enter/Space] ボタン押下  [ESC] 終了",
			color: Color.Gray
		).Location(20, 540);
		Root.Add(helpText);

		// 初期フォーカス
		uiManager.SetFocus(basicButton);
	}

	// InputMap はUIと独立して使える
	private readonly InputMap _inputMap = new();
	private int _inputMapCount;
	private bool _inputMapInitialized;

	private void InitInputMap()
	{
		_inputMap.AddAction("count");
		_inputMap.BindKey("count", KeyCode.Space);
		_inputMap.AddAction("reset");
		_inputMap.BindKey("reset", KeyCode.G);
		_inputMapInitialized = true;
	}

	public override void OnUpdate()
	{
		if (!_inputMapInitialized) InitInputMap();
		_inputMap.Update(keyboard, mouse);

		if (_inputMap.GetAction("count").IsJustPressed)
		{
			_inputMapCount++;
			statusText.Content = $"InputMap カウント: {_inputMapCount}";
		}

		if (_inputMap.GetAction("reset").IsJustPressed)
		{
			_inputMapCount = 0;
			statusText.Content = "InputMap カウントをリセット！";
		}

		if (keyboard.Escape.IsKeyDown) App.LoadScene<MainScene>();

		console.Clear();
		console.Print($"Hovered: {uiManager.HoveredElement?.GetType().Name ?? "none"}");
		console.Print($"Focused: {uiManager.FocusedElement?.GetType().Name ?? "none"}");
		console.Print($"Mouse: {mouse.Position}");
	}
}
