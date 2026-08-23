using System.Drawing;
using Promete.Graphics.Fonts;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

namespace Promete.Nodes;

/// <summary>
/// テキストを表示するノード
/// </summary>
/// <remarks>
/// テキストはグリフ単位でグリフアトラスから読み出して描画されます。
/// 内容を変更してもテクスチャは再生成されず、レイアウトのみが再計算されます。
/// </remarks>
public class Text : Node
{
    private string _content;
    private Font _font;
    private bool _isUpdateRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="Text"/> class.
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

        UpdateLayout();
    }

    /// <summary>
    /// レイアウト済みのテキストを取得します。
    /// </summary>
    public TextLayout Layout { get; private set; } = TextLayout.Empty;

    /// <summary>
    /// ノードのサイズ
    /// </summary>
    public override VectorInt Size
    {
        get => Layout.Size;
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
            if (Options.Size == value)
                return;
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
            if (_content == value)
                return;
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
            if (Options.TextColor == value)
                return;
            Options.TextColor = value;
            _isUpdateRequested = true;
        }
    }

    /// <summary>
    /// 縁取りの色
    /// </summary>
    public Color? BorderColor
    {
        get => Options.BorderColor;
        set
        {
            if (Options.BorderColor == value)
                return;
            Options.BorderColor = value;
            _isUpdateRequested = true;
        }
    }

    /// <summary>
    /// 縁取りの太さ
    /// </summary>
    public int BorderThickness
    {
        get => Options.BorderThickness;
        set
        {
            if (Options.BorderThickness == value)
                return;
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
            if (_font.Equals(value))
                return;
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
            if (Options.LineSpacing.Equals(value))
                return;
            Options.LineSpacing = value;
            _isUpdateRequested = true;
        }
    }

    /// <summary>
    /// 文字間隔
    /// </summary>
    public float LetterSpacing
    {
        get => Options.LetterSpacing;
        set
        {
            if (Options.LetterSpacing.Equals(value))
                return;
            Options.LetterSpacing = value;
            _isUpdateRequested = true;
        }
    }

    /// <summary>
    /// テキストを折り返す方法
    /// </summary>
    public WrapMode WrapMode
    {
        get => Options.WrapMode;
        set
        {
            if (Options.WrapMode == value)
                return;
            Options.WrapMode = value;
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
            if (Options.VerticalAlignment == value)
                return;
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
            if (Options.HorizontalAlignment == value)
                return;
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
            if (Options.UseRichText == value)
                return;
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

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (Layout.Glyphs.Count == 0)
            return;

        var atlas = PrometeApp.Current.GlyphAtlas;

        foreach (var placed in Layout.Glyphs)
        {
            var entry = atlas.GetOrAdd(placed.Glyph, placed.Options);
            if (entry.IsEmpty)
                continue;

            var location = placed.Position + entry.Bearing;
            queue.Enqueue(
                new DrawTextureCommand
                {
                    Texture = entry.Texture,
                    ModelMatrix = ModelMatrix,
                    TintColor = placed.Color,
                    Width = entry.Size.X,
                    Height = entry.Size.Y,
                    Pivot = location,
                }
            );
        }
    }

    protected override void OnPreRender()
    {
        if (!_isUpdateRequested)
            return;
        UpdateLayout();
        _isUpdateRequested = false;
    }

    /// <summary>
    /// テキストのレイアウトを再計算します。
    /// </summary>
    public void UpdateLayout()
    {
        Layout = TextLayoutEngine.Layout(Content, _font, Options);
        UpdateModelMatrix();
    }
}
