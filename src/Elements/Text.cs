using System.Drawing;

namespace Promete.Elements;

public class Text : ElementBase
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

	private Texture2D texture;
	private string content = "";
	private Color? textColor;
	private Color? borderColor;
	private int borderThickness;
	private Font font;

	public Text() : this("")
	{
	}

	public Text(string content) : this(content, Font.GetDefault(16))
	{
	}

	public Text(string content, Font font) : this(content, font, null)
	{
	}

	public Text(string content, Font font, Color? color)
	{
		this.content = content;
		this.font = font;
		textColor = color;

		RenderTexture();
	}

	public Text(string content, float fontSize) : this(content, fontSize, FontStyle.Normal)
	{
	}

	public Text(string content, float fontSize, FontStyle fontStyle) : this(content, fontSize, fontStyle, null)
	{
	}

	public Text(string content, float fontSize, FontStyle fontStyle, Color? color) : this(content,
		Font.GetDefault(fontSize, fontStyle), color)
	{
	}

	protected override void OnDestroy()
	{
		texture.Dispose();
	}

	private void SetAndUpdateTexture<T>(ref T variable, T value)
	{
		if (variable?.Equals(value) ?? false) return;
		variable = value;
		RenderTexture();
	}

	private void RenderTexture()
	{
		texture.Dispose();

		texture = GlyphRenderer.Generate(Content, Font, Color, BorderColor, BorderThickness);
	}
}
