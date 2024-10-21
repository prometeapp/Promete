using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Promete.Elements;
using Promete.Graphics;
using Promete.Windowing;

namespace Promete;

/// <summary>
/// 画面上に簡易な文字出力を行うレイヤーを提供する Promete プラグインです。
/// </summary>
public class ConsoleLayer
{
	/// <summary>
	/// 現在のコンソール上のカーソル位置を取得または設定します。
	/// </summary>
	public VectorInt Cursor { get; set; }

	/// <summary>
	/// 現在のフォントサイズを取得または設定します。
	/// </summary>
	public int FontSize { get; set; }

	/// <summary>
	/// 現在使用しているフォントのパスを取得または設定します。
	/// </summary>
	public string? FontPath { get; set; }

	public Font Font
	{
		get => text.Font;
		set => text.Font = value;
	}

	/// <summary>
	/// 現在の文字色を取得または設定します。
	/// </summary>
	public Color TextColor { get; set; } = Color.White;

	private string? prevFont;
	private int maxLine;

	private readonly Text text;
	private readonly IWindow window;
	private readonly GlyphRenderer glyphRenderer;
	private readonly List<string> consoleBuffer = [];

	public ConsoleLayer(PrometeApp app, IWindow window, GlyphRenderer glyphRenderer)
	{
		this.window = window;
		this.glyphRenderer = glyphRenderer;
		FontSize = 16;
		text = new Text("", Font.GetDefault(), Color.White);
		maxLine = CalculateMaxLine();

		window.Update += () =>
		{
			text.Update();
		};

		window.Render += () =>
		{
			app.RenderElement(text);
		};

		app.SceneWillChange += Clear;

		window.PostUpdate += UpdateConsole;
	}

	/// <summary>
	/// コンソール上の文字列を完全に消去します。
	/// </summary>
	public void Clear()
	{
		consoleBuffer.Clear();
		FontSize = 16;
		Cursor = VectorInt.Zero;
	}

	/// <summary>
	/// コンソール上に、指定したオブジェクトを出力します。
	/// </summary>
	/// <param name="obj">表示するデータ。</param>
	public void Print(object? obj)
	{
		var line = obj as string ?? obj?.ToString() ?? "null";
		var (x, y) = Cursor;
		x = Math.Max(0, x);
		y = Math.Max(0, y);
		if (y < consoleBuffer.Count)
		{
			// 置換
			consoleBuffer[y] = consoleBuffer[y].ReplaceAt(x, line);
		}
		else
		{
			// 挿入
			consoleBuffer.AddRange(Enumerable.Repeat("", y - consoleBuffer.Count));
			consoleBuffer.Add(new string(' ', x) + line);
		}

		Cursor = new VectorInt(0, y + 1);
	}

	private void UpdateConsole()
	{
		var f = text.Font;
		if (f.Size != FontSize || prevFont != FontPath)
		{
			text.Font = FontPath == null ? Font.GetDefault(FontSize) : new Font(FontPath, FontSize);
			maxLine = CalculateMaxLine();
		}

		var buf = consoleBuffer.Count > maxLine ? consoleBuffer.Skip(consoleBuffer.Count - maxLine) : consoleBuffer;

		text.Color = TextColor;
		text.Content = string.Join('\n', buf);
		prevFont = FontPath;
	}

	private int CalculateMaxLine()
	{
		var textToTest = "";
		var l = 0;
		Rect bounds;
		do
		{
			textToTest += "A\n";
			bounds = glyphRenderer.GetTextBounds(textToTest, text.Font);
			l++;
		} while (bounds.Height < window.Height);

		return l - 1;
	}
}
