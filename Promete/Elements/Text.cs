using System.Drawing;
using Promete.Graphics;
using Promete.Graphics.Fonts;

namespace Promete.Elements;

public class Text : ElementBase
{
	public Texture2D? RenderedTexture { get; private set; }

	public override VectorInt Size
	{
		get => RenderedTexture?.Size ?? (0, 0);
		set => PreferredSize = value;
	}

	public VectorInt PreferredSize
	{
		get => _options.Size;
		set
		{
			if (_options.Size == value) return;
			_options.Size = value;
			_isUpdateRequested = true;
		}
	}

	public string Content
	{
		get => _content;
		set
		{
			if (_content == value) return;
			_content = value;
			_isUpdateRequested = true;
		}
	}

	public Color Color
	{
		get => _options.TextColor;
		set
		{
			if (_options.TextColor == value) return;
			_options.TextColor = value;
			_isUpdateRequested = true;
		}
	}

	public Color? BorderColor
	{
		get => _options.BorderColor;
		set
		{
			if (_options.BorderColor == value) return;
			_options.BorderColor = value;
			_isUpdateRequested = true;
		}
	}

	public int BorderThickness
	{
		get => _options.BorderThickness;
		set
		{
			if (_options.BorderThickness == value) return;
			_options.BorderThickness = value;
			_isUpdateRequested = true;
		}
	}

	public Font Font
	{
		get => _font;
		set
		{
			if (_font.Equals(value)) return;
			_font = value;
			_isUpdateRequested = true;
		}
	}

	public float LineSpacing
	{
		get => _options.LineSpacing;
		set
		{
			if (_options.LineSpacing.Equals(value)) return;
			_options.LineSpacing = value;
			_isUpdateRequested = true;
		}
	}

	public bool WordWrap
	{
		get => _options.WordWrap;
		set
		{
			if (_options.WordWrap == value) return;
			_options.WordWrap = value;
			_isUpdateRequested = true;
		}
	}

	public VerticalAlignment VerticalAlignment
	{
		get => _options.VerticalAlignment;
		set
		{
			if (_options.VerticalAlignment == value) return;
			_options.VerticalAlignment = value;
			_isUpdateRequested = true;
		}
	}

	public HorizontalAlignment HorizontalAlignment
	{
		get => _options.HorizontalAlignment;
		set
		{
			if (_options.HorizontalAlignment == value) return;
			_options.HorizontalAlignment = value;
			_isUpdateRequested = true;
		}
	}

	public bool UseRichText
	{
		get => _options.UseRichText;
		set
		{
			if (_options.UseRichText == value) return;
			_options.UseRichText = value;
			_isUpdateRequested = true;
		}
	}

	private string _content;
	private Font _font;
	private bool _isUpdateRequested;

	private readonly TextRenderingOptions _options = new();

	public Text(string content, Font? font = default, Color? color = default)
	{
		_content = content;
		_font = font ?? Font.GetDefault();
		_options.TextColor = color ?? Color.White;

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
		RenderedTexture = _font.GenerateTexture(PrometeApp.Current.Window.TextureFactory, Content, _options);
		oldTexture?.Dispose();
	}
}
