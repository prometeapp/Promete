using System.Drawing;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Example.examples.ui;

[Demo("ui/ui_demo.demo", "UIライブラリのデモ — ボタン、フォーカス、InputMap")]
public class UIDemoScene(Keyboard keyboard, Mouse mouse, UIManager uiManager, ConsoleLayer console) : Scene
{
	private Text statusText = null!;
	private int clickCount;

	public override void OnStart()
	{
		// --- ステータス表示 ---
		statusText = new Text("ボタンをクリックしてみよう！", color: Color.White)
			.Location(20, 20);
		Root.Add(statusText);

		// --- 基本ボタン ---
		var basicButton = new Button("クリック！")
			.Location(50, 80)
			.Size(180, 50)
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
			.Location(50, 150)
			.Size(180, 50)
			.NavigationOrder(2)
			.OnClick(() => statusText.Content = "緑のボタンがクリックされた！");
		Root.Add(greenButton);

		// --- 無効化されたボタン ---
		var disabledButton = new Button("無効なボタン")
			.Location(50, 220)
			.Size(180, 50)
			.Enabled(false);
		Root.Add(disabledButton);

		// --- フォーカスグループ デモ ---
		var groupLabel = new Text("フォーカスグループ (Tab で移動)", color: Color.LightGray)
			.Location(300, 60);
		Root.Add(groupLabel);

		var focusGroup = new FocusGroup("menu");

		for (var i = 0; i < 4; i++)
		{
			var index = i;
			var menuButton = new Button($"メニュー {i + 1}")
				.Location(300, 90 + i * 55)
				.Size(200, 45)
				.NavigationOrder(10 + i)
				.InFocusGroup(focusGroup)
				.OnClick(() => statusText.Content = $"メニュー {index + 1} が選ばれた！");
			Root.Add(menuButton);
		}

		// --- InputMap デモ ---
		var inputMapLabel = new Text("InputMap: Space=カウント / G=リセット", color: Color.LightGray)
			.Location(300, 310);
		Root.Add(inputMapLabel);

		// --- 操作説明 ---
		var helpText = new Text(
			"[Tab] フォーカス移動  [Enter/Space] フォーカス中のボタンを押す\n" +
			"[ESC] 終了  マウスクリックも使えます",
			color: Color.Gray
		).Location(20, 400);
		Root.Add(helpText);

		// 初期フォーカス
		uiManager.SetFocus(basicButton);
	}

	// InputMap はUIと独立して使える例
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

		// コンソールにホバー/フォーカス情報を表示
		console.Clear();
		console.Print($"Hovered: {uiManager.HoveredElement?.GetType().Name ?? "none"}");
		console.Print($"Focused: {(uiManager.FocusedElement as Button)?.TextContent ?? "none"}");
		console.Print($"Mouse: {mouse.Position}");
	}
}
