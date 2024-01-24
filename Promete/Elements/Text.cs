using System;
using System.Drawing;
using Promete.Graphics;

namespace Promete.Elements;

public class Text : ElementBase
{
	public ITexture? RenderedTexture { get; private set; }

	public override VectorInt Size
	{
		get => RenderedTexture?.Size ?? (0, 0);
		set
		{
			/* nop */
		}
	}

	public string Content
	{
		get => content;
		set => SetAndUpdateTexture(ref content, value);
	}

	public Color? Color
	{
		get => textColor;
		set => SetAndUpdateTexture(ref textColor, value);
	}

	public Color? BorderColor
	{
		get => borderColor;
		set => SetAndUpdateTexture(ref borderColor, value);
	}

	public int BorderThickness
	{
		get => borderThickness;
		set => SetAndUpdateTexture(ref borderThickness, value);
	}

	public Font Font
	{
		get => font;
		set => SetAndUpdateTexture(ref font, value);
	}

	private string content;
	private Color? textColor;
	private Color? borderColor;
	private int borderThickness;
	private Font font;

	private readonly GlyphRenderer glyphRenderer;

	public Text(
		string content,
		Font? font = default,
		Color? color = default,
		string name = "",
		Vector? location = default,
		Vector? scale = default) : base(name, location, scale)
	{
		glyphRenderer = PrometeApp.Current?.GetPlugin<GlyphRenderer>() ?? throw new InvalidOperationException("System is not initialized yet!");
		this.content = content;
		this.font = font ?? Font.GetDefault(16);
		textColor = color ?? System.Drawing.Color.White;

		RenderTexture();
	}

	protected override void OnDestroy()
	{
		RenderedTexture?.Dispose();
	}

	private void SetAndUpdateTexture<T>(ref T variable, T value)
	{
		if (variable?.Equals(value) ?? false) return;
		variable = value;
		RenderTexture();
	}

	private void RenderTexture()
	{
		RenderedTexture?.Dispose();

		RenderedTexture = glyphRenderer.Generate(Content, Font, Color, BorderColor, BorderThickness);
	}
}
