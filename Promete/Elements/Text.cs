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
		set => PreferredSize = value;
	}

	public VectorInt PreferredSize
	{
		get => options.Size;
		set
		{
			if (options.Size == value) return;
			options.Size = value;
			_isUpdateRequested = true;
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

	public Color Color
	{
		get => options.TextColor;
		set
		{
			if (options.TextColor == value) return;
			options.TextColor = value;
			_isUpdateRequested = true;
		}
	}

	public Color? BorderColor
	{
		get => options.BorderColor;
		set
		{
			if (options.BorderColor == value) return;
			options.BorderColor = value;
			_isUpdateRequested = true;
		}
	}

	public int BorderThickness
	{
		get => options.BorderThickness;
		set
		{
			if (options.BorderThickness == value) return;
			options.BorderThickness = value;
			_isUpdateRequested = true;
		}
	}

	public Font Font
	{
		get => options.Font;
		set
		{
			if (options.Font.Equals(value)) return;
			options.Font = value;
			_isUpdateRequested = true;
		}
	}

	public float LineSpacing
	{
		get => options.LineSpacing;
		set
		{
			if (options.LineSpacing == value) return;
			options.LineSpacing = value;
			_isUpdateRequested = true;
		}
	}

	public bool WordWrap
	{
		get => options.WordWrap;
		set
		{
			if (options.WordWrap == value) return;
			options.WordWrap = value;
			_isUpdateRequested = true;
		}
	}

	public VerticalAlignment VerticalAlignment
	{
		get => options.VerticalAlignment;
		set
		{
			if (options.VerticalAlignment == value) return;
			options.VerticalAlignment = value;
			_isUpdateRequested = true;
		}
	}

	public HorizontalAlignment HorizontalAlignment
	{
		get => options.HorizontalAlignment;
		set
		{
			if (options.HorizontalAlignment == value) return;
			options.HorizontalAlignment = value;
			_isUpdateRequested = true;
		}
	}

	public bool UseRichText
	{
		get => options.UseRichText;
		set
		{
			if (options.UseRichText == value) return;
			options.UseRichText = value;
			_isUpdateRequested = true;
		}
	}

	private string content;

	private bool _isUpdateRequested;

	private readonly TextRenderingOptions options = new();
	private readonly GlyphRenderer glyphRenderer;

	public Text(string content, Font? font = default, Color? color = default)
	{
		glyphRenderer = PrometeApp.Current.GetPlugin<GlyphRenderer>() ?? throw new InvalidOperationException("System is not initialized yet!");
		this.content = content;
		options.Font = font ?? Font.GetDefault();
		options.TextColor = color ?? Color.White;

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
		RenderedTexture = glyphRenderer.Generate(Content, options);
		oldTexture?.Dispose();
	}
}
