using System.Drawing;
using Promete.Graphics;
using Promete.Graphics.Fonts;

namespace Promete.Nodes;

public class Text : Node
{
    private string _content;
    private Font _font;
    private bool _isUpdateRequested;


    public Text(string content, Font? font = default, Color? color = default)
    {
        _content = content;
        _font = font ?? Font.GetDefault();
        Options.TextColor = color ?? Color.White;

        RenderTexture();
    }

    public Texture2D? RenderedTexture { get; private set; }

    public override VectorInt Size
    {
        get => RenderedTexture?.Size ?? (0, 0);
        set => PreferredSize = value;
    }

    public VectorInt PreferredSize
    {
        get => Options.Size;
        set
        {
            if (Options.Size == value) return;
            Options.Size = value;
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
        get => Options.TextColor;
        set
        {
            if (Options.TextColor == value) return;
            Options.TextColor = value;
            _isUpdateRequested = true;
        }
    }

    public Color? BorderColor
    {
        get => Options.BorderColor;
        set
        {
            if (Options.BorderColor == value) return;
            Options.BorderColor = value;
            _isUpdateRequested = true;
        }
    }

    public int BorderThickness
    {
        get => Options.BorderThickness;
        set
        {
            if (Options.BorderThickness == value) return;
            Options.BorderThickness = value;
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
        get => Options.LineSpacing;
        set
        {
            if (Options.LineSpacing.Equals(value)) return;
            Options.LineSpacing = value;
            _isUpdateRequested = true;
        }
    }

    public bool WordWrap
    {
        get => Options.WordWrap;
        set
        {
            if (Options.WordWrap == value) return;
            Options.WordWrap = value;
            _isUpdateRequested = true;
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => Options.VerticalAlignment;
        set
        {
            if (Options.VerticalAlignment == value) return;
            Options.VerticalAlignment = value;
            _isUpdateRequested = true;
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => Options.HorizontalAlignment;
        set
        {
            if (Options.HorizontalAlignment == value) return;
            Options.HorizontalAlignment = value;
            _isUpdateRequested = true;
        }
    }

    public bool UseRichText
    {
        get => Options.UseRichText;
        set
        {
            if (Options.UseRichText == value) return;
            Options.UseRichText = value;
            _isUpdateRequested = true;
        }
    }

    public bool UseAntialiasing
    {
        get => Options.UseAntialiasing;
        set
        {
            if (Options.UseAntialiasing == value) return;
            Options.UseAntialiasing = value;
            _isUpdateRequested = true;
        }
    }

    public TextRenderingOptions Options { get; } = DefaultOptions.Clone();

    public static TextRenderingOptions DefaultOptions { get; } = new();

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

    public void RenderTexture()
    {
        var oldTexture = RenderedTexture;
        RenderedTexture = _font.GenerateTexture(PrometeApp.Current.Window.TextureFactory, Content, Options);
        oldTexture?.Dispose();
    }
}
