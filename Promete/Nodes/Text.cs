using System.Drawing;
using Promete.Graphics;
using Promete.Graphics.Fonts;

namespace Promete.Nodes;

/// <summary>
/// テキストを表示するノード
/// </summary>
public class Text : Node
{
    private string _content;
    private Font _font;
    private bool _isUpdateRequested;


    /// <summary>
    /// テキストノードのコンストラクタ
    /// </summary>
    /// <param name="content">表示するテキスト内容</param>
    /// <param name="font">使用するフォント</param>
    /// <param name="color">テキストの色</param>
    public Text(string content, Font? font = default, Color? color = default)
    {
        _content = content;
        _font = font ?? Font.GetDefault();
        Options.TextColor = color ?? Color.White;

        RenderTexture();
    }

    /// <summary>
    /// レンダリングされたテキストのテクスチャ
    /// </summary>
    public Texture2D? RenderedTexture { get; private set; }

    /// <summary>
    /// ノードのサイズ
    /// </summary>
    public override VectorInt Size
    {
        get => RenderedTexture?.Size ?? (0, 0);
        set => PreferredSize = value;
    }

    /// <summary>
    /// 希望するテキストサイズ
    /// </summary>
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

    /// <summary>
    /// テキスト内容
    /// </summary>
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

    /// <summary>
    /// テキストの色
    /// </summary>
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

    /// <summary>
    /// ボーダーの色
    /// </summary>
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

    /// <summary>
    /// ボーダーの太さ
    /// </summary>
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

    /// <summary>
    /// 使用するフォント
    /// </summary>
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

    /// <summary>
    /// 行間隔
    /// </summary>
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

    /// <summary>
    /// ワードラップの有効/無効
    /// </summary>
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

    /// <summary>
    /// 垂直方向の配置
    /// </summary>
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

    /// <summary>
    /// 水平方向の配置
    /// </summary>
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

    /// <summary>
    /// リッチテキスト機能の使用有無
    /// </summary>
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

    /// <summary>
    /// テキストレンダリングオプション
    /// </summary>
    public TextRenderingOptions Options { get; } = DefaultOptions.Clone();

    /// <summary>
    /// デフォルトのテキストレンダリングオプション
    /// </summary>
    public static TextRenderingOptions DefaultOptions { get; } = new();

    protected override void OnPreRender()
    {
        if (!_isUpdateRequested) return;
        RenderTexture();
        _isUpdateRequested = false;
    }

    protected override void OnDestroy()
    {
        RenderedTexture?.Dispose();
    }

    /// <summary>
    /// テクスチャをレンダリングする
    /// </summary>
    public void RenderTexture()
    {
        var oldTexture = RenderedTexture;
        RenderedTexture = _font.GenerateTexture(PrometeApp.Current.Window.TextureFactory, Content, Options);
        UpdateModelMatrix();
        oldTexture?.Dispose();
    }
}
