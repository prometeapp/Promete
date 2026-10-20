using System;
using System.Collections.Generic;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// テキスト入力UI要素です。
/// フォーカス中にキーボード入力を受け付けます。
/// </summary>
/// <remarks>
/// <see cref="Rows"/> を 2 以上にすると、複数行の入力欄になります。
/// 複数行では、Enter で改行し、Ctrl+Enter で <see cref="Submitted"/> を発火します。
/// 長い行は幅に合わせて折り返され、行数が <see cref="Rows"/> を超えるとキャレットに合わせて縦にスクロールします。
/// </remarks>
public class TextInput : UIElement
{
    private const int SingleLineCaretMargin = 4;

    private readonly Text _textNode;
    private readonly Text _placeholderNode;
    private readonly Container _lineContainer = new();
    private readonly List<Text> _lineNodes = [];
    private string _text = "";
    private string _placeholder = "";
    private int _cursorPosition;
    private int _desiredCaretX = -1;
    private int _rows = 1;
    private int _scrollLine;
    private float _cursorBlinkTimer;
    private bool _cursorVisible;
    private bool _needsBufferFlush;
    private bool _linesDirty = true;
    private IUIStyle<TextInput> _style;

    private TextLayout? _layout;
    private string? _layoutText;
    private int _layoutWidth;
    private Font? _layoutFont;
    private float _layoutLetterSpacing;

    private VectorInt _lastSize;
    private System.Drawing.Color _lastLineColor;

    /// <summary>
    /// TextInput の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="placeholder">プレースホルダーテキスト。</param>
    /// <param name="style">適用するスタイル。null の場合は DefaultTextInputStyle が使用されます。</param>
    public TextInput(string placeholder = "", IUIStyle<TextInput>? style = null)
    {
        _style = style ?? new DefaultTextInputStyle();
        _placeholder = placeholder;
        _placeholderNode = new Text(placeholder);
        _textNode = new Text("");
        Add(_placeholderNode);
        Add(_textNode);
        Add(_lineContainer);
        Size = (200, 32);
        IsFocusable = true;
        _needsBufferFlush = true;

        GotFocus += () => _needsBufferFlush = true;
    }

    /// <summary>テキストが変更されたときに発火します。</summary>
    public event Action<string>? TextChanged;

    /// <summary>
    /// 確定の操作が行われたときに発火します。
    /// 1行の入力欄では Enter、複数行の入力欄では Ctrl+Enter です。
    /// </summary>
    public event Action<string>? Submitted;

    /// <summary>入力テキストを取得または設定します。</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
                return;
            _text = value;
            _cursorPosition = Math.Min(_cursorPosition, _text.Length);
            _desiredCaretX = -1;
            _textNode.Content = _text;
            OnContentChanged();
            TextChanged?.Invoke(_text);
        }
    }

    /// <summary>プレースホルダーテキストを取得または設定します。</summary>
    public string Placeholder
    {
        get => _placeholder;
        set
        {
            _placeholder = value;
            _placeholderNode.Content = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// 表示する行数を取得または設定します。デフォルトは 1 (1行の入力欄) です。
    /// 2 以上にすると複数行の入力欄になり、高さがこの行数に合わせて決まります。
    /// </summary>
    /// <remarks>
    /// 高さは、設定した時点で <c>行数 × 1行の高さ + 上下の余白</c> に更新されます。
    /// その後に <see cref="Node.Size"/> で高さを変えた場合は、その高さに収まる分だけが表示されます。
    /// </remarks>
    public int Rows
    {
        get => _rows;
        set
        {
            var rows = Math.Max(1, value);
            if (_rows == rows)
                return;

            _rows = rows;
            if (IsMultiline)
                Size = (Size.X, rows * GetLineHeight() + PaddingTop * 2);

            _scrollLine = 0;
            _desiredCaretX = -1;
            OnContentChanged();
        }
    }

    /// <summary>複数行の入力欄かどうか。</summary>
    public bool IsMultiline => _rows > 1;

    /// <summary>カーソル位置を取得します。</summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            _cursorPosition = Math.Clamp(value, 0, _text.Length);
            _desiredCaretX = -1;
            OnCaretMoved();
        }
    }

    /// <summary>テキスト表示用の Text ノード。</summary>
    public Text TextNode => _textNode;

    /// <summary>プレースホルダー表示用の Text ノード。</summary>
    public Text PlaceholderNode => _placeholderNode;

    /// <summary>テキストの左右のパディング。</summary>
    public int PaddingLeft { get; set; } = 6;

    /// <summary>複数行のときの、テキストの上下のパディング。</summary>
    public int PaddingTop { get; set; } = 6;

    /// <summary>カーソルの点滅間隔（秒）。フレームレートに依存しません。</summary>
    public float CursorBlinkInterval { get; set; } = 0.5f;

    /// <summary>カーソル表示状態。スタイルから参照されます。</summary>
    public bool IsCursorVisible => _cursorVisible && IsFocused;

    /// <summary>
    /// キャレットの左上の座標を、この要素の左上を原点として取得します。
    /// スタイルがキャレットを描くときに参照します。
    /// </summary>
    public Vector CaretLocation
    {
        get
        {
            var layout = GetLayout();
            var x = PaddingLeft + CaretNavigator.GetX(layout, _cursorPosition);
            if (!IsMultiline)
                return new Vector(x, SingleLineCaretMargin);

            return new Vector(x, PaddingTop + CaretTop(layout) - ScrollTop(layout));
        }
    }

    /// <summary>キャレットの高さを取得します。</summary>
    public int CaretHeight => IsMultiline ? GetLineHeight() : Size.Y - SingleLineCaretMargin * 2;

    /// <summary>スタイルを取得または設定します。</summary>
    public IUIStyle<TextInput> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>先頭に表示している行のインデックス。</summary>
    internal int ScrollLine => _scrollLine;

    /// <summary>複数行のとき、現在表示されている行の内容。</summary>
    internal IReadOnlyList<string> VisibleLines
    {
        get
        {
            var result = new List<string>();
            if (!IsMultiline)
                return result;

            foreach (var node in _lineNodes)
            {
                if (node.IsVisible)
                    result.Add(node.Content);
            }

            return result;
        }
    }

    /// <summary>
    /// フォーカス中にキーボード入力を処理します。
    /// UIManager から毎フレーム呼ばれます。
    /// </summary>
    internal void ProcessInput(Keyboard keyboard, float deltaTime)
    {
        if (!IsFocused)
            return;

        // フォーカス直後はバッファに溜まった入力を捨てる
        if (_needsBufferFlush)
        {
            keyboard.GetString();
            _needsBufferFlush = false;
            return;
        }

        // カーソル点滅
        _cursorBlinkTimer += deltaTime;
        if (_cursorBlinkTimer >= CursorBlinkInterval)
        {
            _cursorVisible = !_cursorVisible;
            _cursorBlinkTimer = 0;
            NotifyStyleDirty();
        }

        // 文字入力。改行は Enter キーで扱うので、入力文字列からは取り除く
        var input = keyboard.GetString().Replace("\r", "").Replace("\n", "");
        if (input.Length > 0)
            InsertAtCursor(input);

        // BackSpace
        if (keyboard.BackSpace.IsKeyDown && _cursorPosition > 0)
        {
            _text = _text.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
            AfterEdit();
        }

        // Delete
        if (keyboard.Delete.IsKeyDown && _cursorPosition < _text.Length)
        {
            _text = _text.Remove(_cursorPosition, 1);
            AfterEdit();
        }

        // カーソル移動
        if (keyboard.Left.IsKeyDown && _cursorPosition > 0)
            MoveCaretTo(_cursorPosition - 1, keepColumn: false);

        if (keyboard.Right.IsKeyDown && _cursorPosition < _text.Length)
            MoveCaretTo(_cursorPosition + 1, keepColumn: false);

        if (IsMultiline)
        {
            if (keyboard.Up.IsKeyDown)
                MoveCaretVertically(-1);
            if (keyboard.Down.IsKeyDown)
                MoveCaretVertically(1);
        }

        if (keyboard.Home.IsKeyDown)
        {
            var home = IsMultiline ? LineStartOfCaret() : 0;
            MoveCaretTo(home, keepColumn: false);
        }

        if (keyboard.End.IsKeyDown)
        {
            var end = IsMultiline ? LineEndOfCaret() : _text.Length;
            MoveCaretTo(end, keepColumn: false);
        }

        // Enter: 複数行では改行、Ctrl+Enter または1行では Submit
        if (keyboard.Enter.IsKeyDown)
        {
            var isCtrl = keyboard.ControlLeft.IsPressed || keyboard.ControlRight.IsPressed;
            if (IsMultiline && !isCtrl)
                InsertAtCursor("\n");
            else
                Submitted?.Invoke(_text);
        }
    }

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // サイズが変わったら、折り返しと表示範囲が変わる
        var size = Size;
        if (!size.Equals(_lastSize))
        {
            _lastSize = size;
            _linesDirty = true;
            if (IsMultiline)
                EnsureCaretVisible();
        }

        // プレースホルダーの表示切り替え
        _placeholderNode.IsVisible = string.IsNullOrEmpty(_text) && !IsFocused;

        if (IsMultiline)
            UpdateMultiline();
        else
            UpdateSingleLine();
    }

    private void UpdateSingleLine()
    {
        _textNode.IsVisible = true;
        _lineContainer.IsVisible = false;

        // テキストとプレースホルダーの位置
        var textY = (Size.Y - _textNode.Size.Y) / 2f;
        if (_textNode.Size.Y > 0)
            _textNode.Location = new Vector(PaddingLeft, textY);
        if (_placeholderNode.Size.Y > 0)
            _placeholderNode.Location = new Vector(PaddingLeft, textY);
    }

    private void UpdateMultiline()
    {
        // 表示は行ごとの Text で行うので、全体の Text ノードは隠す (スタイルが色を決める窓口として残す)
        _textNode.IsVisible = false;
        _lineContainer.IsVisible = true;

        if (_placeholderNode.Size.Y > 0)
            _placeholderNode.Location = new Vector(PaddingLeft, PaddingTop);

        if (_linesDirty || _lastLineColor != _textNode.Color)
            RefreshLineNodes();
    }

    /// <summary>
    /// 見える行ごとに Text ノードを作り直します。
    /// 行単位でスクロールするため、見切れる行は作りません。
    /// </summary>
    private void RefreshLineNodes()
    {
        _linesDirty = false;
        _lastLineColor = _textNode.Color;

        var layout = GetLayout();
        var scrollTop = ScrollTop(layout);
        var viewHeight = ViewHeight();

        var visible = 0;
        for (var i = _scrollLine; i < layout.Lines.Count; i++)
        {
            var line = layout.Lines[i];

            // 最低でも1行は表示する (高さが1行に満たない場合の保険)
            if (visible > 0 && line.Top + line.Height - scrollTop > viewHeight)
                break;

            if (visible >= _lineNodes.Count)
            {
                var node = new Text("");
                _lineNodes.Add(node);
                _lineContainer.Add(node);
            }

            var lineNode = _lineNodes[visible];
            lineNode.Font = _textNode.Font;
            lineNode.Color = _textNode.Color;
            lineNode.LetterSpacing = _textNode.LetterSpacing;
            lineNode.Content = _text.Substring(line.StartIndex, line.Length);
            lineNode.Location = new Vector(PaddingLeft, PaddingTop + line.Top - scrollTop);
            lineNode.IsVisible = true;
            visible++;
        }

        for (var i = visible; i < _lineNodes.Count; i++)
            _lineNodes[i].IsVisible = false;
    }

    /// <summary>
    /// 現在のテキストと幅に対するレイアウトを取得します。同じ条件の間は、計算結果を使い回します。
    /// </summary>
    private TextLayout GetLayout()
    {
        var width = IsMultiline ? Math.Max(1, Size.X - PaddingLeft * 2) : 0;
        var font = _textNode.Font;
        var letterSpacing = _textNode.LetterSpacing;

        if (
            _layout != null
            && ReferenceEquals(_layoutText, _text)
            && _layoutWidth == width
            && _layoutFont == font
            && _layoutLetterSpacing == letterSpacing
        )
        {
            return _layout;
        }

        var options = (TextRenderingOptions)_textNode.Options.Clone();
        options.Size = (width, 0);
        options.WrapMode = width > 0 ? WrapMode.Character : WrapMode.None;
        options.LetterSpacing = letterSpacing;

        _layout = TextLayoutEngine.Layout(_text, font, options);
        _layoutText = _text;
        _layoutWidth = width;
        _layoutFont = font;
        _layoutLetterSpacing = letterSpacing;
        _linesDirty = true;
        return _layout;
    }

    private int GetLineHeight()
    {
        var layout = _layout;
        if (layout is { Lines.Count: > 0 })
            return layout.Lines[0].Height;

        return (int)MathF.Ceiling(_textNode.Font.Metrics.LineHeight * _textNode.LineSpacing);
    }

    /// <summary>テキストを表示できる領域の高さ。</summary>
    private int ViewHeight() => Math.Max(1, Size.Y - PaddingTop * 2);

    /// <summary>スクロールによって隠れている部分の高さ。</summary>
    private int ScrollTop(TextLayout layout) =>
        layout.Lines.Count == 0
            ? 0
            : layout.Lines[Math.Min(_scrollLine, layout.Lines.Count - 1)].Top;

    /// <summary>キャレットがある行の上端の、テキスト全体の上端からの距離。</summary>
    private int CaretTop(TextLayout layout) =>
        layout.Lines.Count == 0
            ? 0
            : layout.Lines[CaretNavigator.FindLine(layout, _cursorPosition)].Top;

    private int LineStartOfCaret()
    {
        var layout = GetLayout();
        return CaretNavigator.LineStart(layout, CaretNavigator.FindLine(layout, _cursorPosition));
    }

    private int LineEndOfCaret()
    {
        var layout = GetLayout();
        return CaretNavigator.LineEnd(layout, CaretNavigator.FindLine(layout, _cursorPosition));
    }

    /// <summary>
    /// キャレットの位置に文字列を挿入します。
    /// </summary>
    private void InsertAtCursor(string value)
    {
        _text = _text.Insert(_cursorPosition, value);
        _cursorPosition += value.Length;
        AfterEdit();
    }

    /// <summary>
    /// テキストを編集した後の共通の処理を行います。
    /// </summary>
    private void AfterEdit()
    {
        _desiredCaretX = -1;
        _textNode.Content = _text;
        OnContentChanged();
        TextChanged?.Invoke(_text);
    }

    private void MoveCaretTo(int index, bool keepColumn)
    {
        _cursorPosition = index;
        if (!keepColumn)
            _desiredCaretX = -1;

        OnCaretMoved();
    }

    private void MoveCaretVertically(int direction)
    {
        var layout = GetLayout();
        if (_desiredCaretX < 0)
            _desiredCaretX = CaretNavigator.GetX(layout, _cursorPosition);

        var target = CaretNavigator.MoveVertically(
            layout,
            _cursorPosition,
            _desiredCaretX,
            direction
        );
        if (target != _cursorPosition)
            MoveCaretTo(target, keepColumn: true);
    }

    private void OnContentChanged()
    {
        _linesDirty = true;
        if (IsMultiline)
            EnsureCaretVisible();

        ResetCursorBlink();
        NotifyStyleDirty();
    }

    private void OnCaretMoved()
    {
        if (IsMultiline)
            EnsureCaretVisible();

        ResetCursorBlink();
        NotifyStyleDirty();
    }

    /// <summary>
    /// キャレットのある行が見える範囲に入るよう、先頭に表示する行を調整します。
    /// </summary>
    private void EnsureCaretVisible()
    {
        var layout = GetLayout();
        var lines = layout.Lines;
        if (lines.Count == 0)
        {
            _scrollLine = 0;
            return;
        }

        var viewHeight = ViewHeight();
        var caretLine = CaretNavigator.FindLine(layout, _cursorPosition);
        var previous = _scrollLine;

        _scrollLine = Math.Min(_scrollLine, lines.Count - 1);
        if (caretLine < _scrollLine)
        {
            _scrollLine = caretLine;
        }
        else
        {
            // キャレットの行の下端が収まるまで、先頭の行を送る
            while (
                _scrollLine < caretLine
                && lines[caretLine].Top + lines[caretLine].Height - lines[_scrollLine].Top
                    > viewHeight
            )
            {
                _scrollLine++;
            }
        }

        // 末尾に余白ができる (全体がもっと上から収まる) 場合は、スクロールを戻す
        var lastBottom = lines[^1].Top + lines[^1].Height;
        while (_scrollLine > 0 && lastBottom - lines[_scrollLine - 1].Top <= viewHeight)
            _scrollLine--;

        if (_scrollLine != previous)
            _linesDirty = true;
    }

    private void ResetCursorBlink()
    {
        _cursorVisible = true;
        _cursorBlinkTimer = 0;
    }
}
