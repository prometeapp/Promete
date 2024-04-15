using System;
using System.Drawing;
using Promete.Graphics;

namespace Promete.Elements;

public class Text : ElementBase
{
	public Texture2D? RenderedTexture { get; private set; }

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
		set
		{
			if (content == value) return;
			content = value;
			_isUpdateRequested = true;
		}
	}

	public Color? Color
	{
		get => textColor;
		set
		{
			if (textColor == value) return;
			textColor = value;
			_isUpdateRequested = true;
		}
	}

	public Color? BorderColor
	{
		get => borderColor;
		set
		{
			if (borderColor == value) return;
			borderColor = value;
			_isUpdateRequested = true;
		}
	}

	public int BorderThickness
	{
		get => borderThickness;
		set
		{
			if (borderThickness == value) return;
			borderThickness = value;
			_isUpdateRequested = true;
		}
	}

	public Font Font
	{
		get => font;
		set
		{
			if (font.Equals(value)) return;
			font = value;
			_isUpdateRequested = true;
		}
	}

	private string content;
	private Color? textColor;
	private Color? borderColor;
	private int borderThickness;
	private Font font;

	private bool _isUpdateRequested;

	private readonly GlyphRenderer glyphRenderer;

	public Text(string content, Font? font = default, Color? color = default)
	{
		glyphRenderer = PrometeApp.Current?.GetPlugin<GlyphRenderer>() ?? throw new InvalidOperationException("System is not initialized yet!");
		this.content = content;
		this.font = font ?? Font.GetDefault(16);
		textColor = color ?? System.Drawing.Color.White;

		RenderTexture();
	}

	protected override void OnUpdate()
	{
		if (!_isUpdateRequested) return;
		RenderTexture();
		_isUpdateRequested = false;
	}

	protected override void OnDestroy()
	{
		RenderedTexture?.Dispose();
	}

	private void RenderTexture()
	{
		var oldTexture = RenderedTexture;
		RenderedTexture = glyphRenderer.Generate(Content, Font, Color, BorderColor, BorderThickness);
		oldTexture?.Dispose();
	}
}
