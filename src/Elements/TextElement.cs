using System.Drawing;

namespace Promete.Elements;

public class TextElement : ElementBase
{
	public Texture2D RenderedTexture => texture;

	public override VectorInt Size
	{
		get => texture.Size;
		set
		{
			/* nop */
		}
	}

	public string Text
	{
		get => text;
		set => Set(ref text, value);
	}

	public Color? Color
	{
		get => textColor;
		set => Set(ref textColor, value);
	}

	public Color? BorderColor
	{
		get => borderColor;
		set => Set(ref borderColor, value);
	}

	public int BorderThickness
	{
		get => borderThickness;
		set => Set(ref borderThickness, value);
	}

	public Font Font
	{
		get => font;
		set => Set(ref font, value);
	}

	public TextElement() : this("")
	{
	}

	public TextElement(string text) : this(text, Font.GetDefault(16))
	{
	}

	public TextElement(string text, Font font) : this(text, font, null)
	{
	}

	public TextElement(string text, Font font, Color? color)
	{
		this.text = text;
		this.font = font;
		textColor = color;

		RenderTexture();
	}

	public TextElement(string text, float fontSize) : this(text, fontSize, FontStyle.Normal)
	{
	}

	public TextElement(string text, float fontSize, FontStyle fontStyle) : this(text, fontSize, fontStyle, null)
	{
	}

	public TextElement(string text, float fontSize, FontStyle fontStyle, Color? color) : this(text,
		Font.GetDefault(fontSize, fontStyle), color)
	{
	}

	protected override void OnRender()
	{
		DF.TextureDrawer.Draw(texture, AbsoluteLocation, AbsoluteScale);
	}

	protected override void OnDestroy()
	{
		texture.Dispose();
	}

	private void Set<T>(ref T variable, T value)
	{
		if (variable?.Equals(value) ?? false) return;
		variable = value;
		RenderTexture();
	}

	private void RenderTexture()
	{
		texture.Dispose();

		texture = GlyphRenderer.Generate(Text, Font, Color, BorderColor, BorderThickness);
	}

	private Texture2D texture;
	private string text = "";
	private Color? textColor;
	private Color? borderColor;
	private int borderThickness;
	private Font font;
}
