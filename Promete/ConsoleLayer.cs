using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Promete.Elements;
using Promete.Graphics;
using Promete.Windowing;

namespace Promete;

/// <summary>
/// A simple text-based console component.
/// </summary>
public class ConsoleLayer
{
	public VectorInt Cursor { get; set; }

	public int FontSize { get; set; }

	public string? FontPath { get; set; }

	/// <summary>
	/// Get or set a text color of the console.
	/// </summary>
	/// <value></value>
	public Color TextColor { get; set; } = Color.White;

	private Text? text;
	private Text? heightCalculator;
	private string? prevFont;
	private int maxLine;

	private readonly IWindow window;
	private readonly List<string> consoleBuffer = [];

	public ConsoleLayer(PrometeApp app, IWindow window, GlyphRenderer glyphRenderer)
	{
		this.window = window;
		FontSize = 16;
		text = new Text("", Font.GetDefault(), Color.White);
		heightCalculator = new Text("", Font.GetDefault(), Color.White);
		maxLine = CalculateMaxLine();
		window.Render += () =>
		{
			if (text == null) return;
			app.RenderElement(text);
		};

		window.PostUpdate += UpdateConsole;
	}

	public void Clear()
	{
		consoleBuffer.Clear();
		FontSize = 16;
		Cursor = VectorInt.Zero;
	}

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
		if (text == null) return;
		var f = text.Font;
		if (f.Size != FontSize || prevFont != FontPath)
		{
			heightCalculator.Font =
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
		if (heightCalculator == null) return 0;
		var l = 0;
		do
		{
			heightCalculator.Content += "A\n";
			l++;
		} while (heightCalculator.Height < window.Height);

		return l - 1;
	}
}
