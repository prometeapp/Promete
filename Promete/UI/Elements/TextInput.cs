using System;
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
/// 長い行は幅に合わせて折り返され (<see cref="WrapMode"/>, <see cref="KinsokuMode"/>)、
/// 行数が <see cref="Rows"/> を超えるとキャレットに合わせて縦にスクロールします。
/// </remarks>
public class TextInput : UIElement
{
    private const int SingleLineCaretMargin = 4;
    private const int DefaultHeight = 32;

    private readonly Text _textNode;
    private readonly Text _placeholderNode;
    private readonly Container _viewport = new();
    private readonly KeyRepeater _backSpaceRepeater = new();
    private readonly KeyRepeater _deleteRepeater = new();
    private readonly KeyRepeater _leftRepeater = new();
    private readonly KeyRepeater _rightRepeater = new();
    private readonly KeyRepeater _upRepeater = new();
    private readonly KeyRepeater _downRepeater = new();
    private readonly KeyRepeater _enterRepeater = new();
    private string _text = "";
    private string _placeholder = "";
    private int _cursorPosition;
    private int _desiredCaretX = -1;
    private int _rows = 1;
    private int _scrollY;
    private float _cursorBlinkTimer;
    private bool _cursorVisible;
    private bool _needsBufferFlush;
    private WrapMode _wrapMode = WrapMode.Mixed;
    private KinsokuMode _kinsokuMode = KinsokuMode.Standard;
    private VectorInt _lastSize;
    private IUIStyle<TextInput> _style;

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
        _viewport.Add(_textNode);
        Add(_placeholderNode);
        Add(_viewport);
        Size = (200, DefaultHeight);
        IsFocusable = true;
        _needsBufferFlush = true;

        GotFocus += () =>
        {
            _needsBufferFlush = true;
            ResetKeyRepeat();
        };
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
    /// 複数行の入力欄の高さは、<c>行数 × 1行の高さ + 上下の余白</c> に自動で決まります。
    /// <see cref="Node.Size"/> で高さを指定した場合はその高さが優先され、収まる分の行だけが表示されます。
    /// 高さに 0 を指定すると、<see cref="Rows"/> から決まる高さに戻ります。
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

            // 複数行では高さを Rows から決める。1 行に戻す場合は、既定の高さに戻す
            if (IsMultiline)
                base.Size = (base.Size.X, 0);
            else if (base.Size.Y <= 0)
                base.Size = (base.Size.X, DefaultHeight);

            _scrollY = 0;
            _desiredCaretX = -1;
            OnContentChanged();
        }
    }

    /// <summary>複数行の入力欄かどうか。</summary>
    public bool IsMultiline => _rows > 1;

    /// <summary>
    /// 複数行のときに、行を折り返す方法を取得または設定します。デフォルトは <see cref="WrapMode.Mixed"/> です。
    /// 1行の入力欄では折り返しません。
    /// </summary>
    public WrapMode WrapMode
    {
        get => _wrapMode;
        set
        {
            _wrapMode = value;
            OnContentChanged();
        }
    }

    /// <summary>
    /// 複数行のときに、折り返しで行頭・行末に置けない文字を扱う方法 (禁則処理) を取得または設定します。
    /// </summary>
    public KinsokuMode KinsokuMode
    {
        get => _kinsokuMode;
        set
        {
            _kinsokuMode = value;
            OnContentChanged();
        }
    }

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

    /// <summary>
    /// テキスト表示用の Text ノード。
    /// 複数行のときは、表示範囲で切り抜かれるコンテナの中に置かれ、縦にスクロールされます。
    /// </summary>
    public Text TextNode => _textNode;

    /// <summary>プレースホルダー表示用の Text ノード。</summary>
    public Text PlaceholderNode => _placeholderNode;

    /// <summary>テキストの左右のパディング。</summary>
    public int PaddingLeft { get; set; } = 6;

    /// <summary>複数行のときの、テキストの上下のパディング。</summary>
    public int PaddingTop { get; set; } = 6;

    /// <summary>カーソルの点滅間隔（秒）。フレームレートに依存しません。</summary>
    public float CursorBlinkInterval { get; set; } = 0.5f;

    /// <summary>
    /// BackSpace・Delete・矢印キー・Enter (複数行の改行) を押し続けたとき、繰り返しが始まるまでの時間 (秒)。
    /// 文字キーの繰り返しは OS が行うので、この設定の対象ではありません。
    /// </summary>
    public float KeyRepeatDelay { get; set; } = 0.5f;

    /// <summary>キーを押し続けたときの、繰り返しの間隔 (秒)。<see cref="KeyRepeatDelay"/> を参照してください。</summary>
    public float KeyRepeatInterval { get; set; } = 0.033f;

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

            return new Vector(x, PaddingTop + CaretLine(layout).Top - _scrollY);
        }
    }

    /// <summary>キャレットの高さを取得します。</summary>
    public int CaretHeight =>
        IsMultiline ? CaretLine(GetLayout()).Height : Size.Y - SingleLineCaretMargin * 2;

    /// <summary>
    /// このノードのサイズを取得または設定します。
    /// 複数行の入力欄で高さが 0 の場合、高さは <see cref="Rows"/> から決まります。
    /// </summary>
    public override VectorInt Size
    {
        get
        {
            var size = base.Size;
            return IsMultiline && size.Y <= 0
                ? new VectorInt(size.X, _rows * GetLineHeight() + PaddingTop * 2)
                : size;
        }
        set => base.Size = value;
    }

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

    /// <summary>縦にスクロールしている量 (ピクセル)。</summary>
    internal int ScrollOffset => _scrollY;

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

        // BackSpace / Delete / カーソル移動は、押し続けると繰り返す
        for (var n = Repeats(_backSpaceRepeater, keyboard.BackSpace, deltaTime); n > 0; n--)
        {
            if (_cursorPosition <= 0)
                break;

            _text = _text.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
            AfterEdit();
        }

        for (var n = Repeats(_deleteRepeater, keyboard.Delete, deltaTime); n > 0; n--)
        {
            if (_cursorPosition >= _text.Length)
                break;

            _text = _text.Remove(_cursorPosition, 1);
            AfterEdit();
        }

        for (var n = Repeats(_leftRepeater, keyboard.Left, deltaTime); n > 0; n--)
        {
            if (_cursorPosition > 0)
                MoveCaretTo(_cursorPosition - 1, keepColumn: false);
        }

        for (var n = Repeats(_rightRepeater, keyboard.Right, deltaTime); n > 0; n--)
        {
            if (_cursorPosition < _text.Length)
                MoveCaretTo(_cursorPosition + 1, keepColumn: false);
        }

        // 上下キーは、複数行のときだけキャレットの移動に使う
        var up = Repeats(_upRepeater, keyboard.Up, deltaTime);
        var down = Repeats(_downRepeater, keyboard.Down, deltaTime);
        if (IsMultiline)
        {
            for (; up > 0; up--)
                MoveCaretVertically(-1);
            for (; down > 0; down--)
                MoveCaretVertically(1);
        }

        if (keyboard.Home.IsKeyDown)
        {
            var layout = GetLayout();
            var home = IsMultiline ? CaretNavigator.LineStart(layout, CaretLineIndex(layout)) : 0;
            MoveCaretTo(home, keepColumn: false);
        }

        if (keyboard.End.IsKeyDown)
        {
            var layout = GetLayout();
            var end = IsMultiline
                ? CaretNavigator.LineEnd(layout, CaretLineIndex(layout))
                : _text.Length;
            MoveCaretTo(end, keepColumn: false);
        }

        // Enter: 複数行では改行 (押し続けると繰り返す)、Ctrl+Enter または1行では Submit (押した瞬間だけ)
        var enter = Repeats(_enterRepeater, keyboard.Enter, deltaTime);
        if (enter > 0)
        {
            var isCtrl = keyboard.ControlLeft.IsPressed || keyboard.ControlRight.IsPressed;
            if (IsMultiline && !isCtrl)
            {
                for (; enter > 0; enter--)
                    InsertAtCursor("\n");
            }
            else if (keyboard.Enter.IsKeyDown)
            {
                Submitted?.Invoke(_text);
            }
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
            if (IsMultiline)
                EnsureCaretVisible();
        }

        // プレースホルダーの表示切り替え
        _placeholderNode.IsVisible = string.IsNullOrEmpty(_text) && !IsFocused;

        ConfigureTextNode();
        if (IsMultiline)
            LayoutMultiline();
        else
            LayoutSingleLine();
    }

    /// <summary>
    /// 1行のときは、テキストを上下中央に置きます。
    /// </summary>
    private void LayoutSingleLine()
    {
        Place(_viewport, trim: false, Vector.Zero, default);

        var textY = (Size.Y - _textNode.Size.Y) / 2f;
        if (_textNode.Size.Y > 0)
            Place(_textNode, new Vector(PaddingLeft, textY));
        if (_placeholderNode.Size.Y > 0)
            Place(_placeholderNode, new Vector(PaddingLeft, textY));
    }

    /// <summary>
    /// 複数行のときは、上下の余白の内側を表示範囲にして切り抜き、スクロールした分だけテキストをずらします。
    /// </summary>
    private void LayoutMultiline()
    {
        var width = Math.Max(1, Size.X - PaddingLeft * 2);
        Place(_viewport, trim: true, new Vector(PaddingLeft, PaddingTop), (width, ViewHeight()));
        Place(_textNode, new Vector(0, -_scrollY));
        Place(_placeholderNode, new Vector(PaddingLeft, PaddingTop));
    }

    /// <summary>
    /// 現在のモードに合わせて、Text ノードの折り返しの設定を整えます。
    /// 複数行では入力欄の幅で折り返し、1行では折り返しません。
    /// </summary>
    private void ConfigureTextNode()
    {
        if (IsMultiline)
        {
            _textNode.PreferredSize = (Math.Max(1, Size.X - PaddingLeft * 2), 0);
            _textNode.WrapMode = _wrapMode;
            _textNode.KinsokuMode = _kinsokuMode;
        }
        else
        {
            _textNode.PreferredSize = (0, 0);
            _textNode.WrapMode = WrapMode.None;
        }
    }

    /// <summary>
    /// 設定を整えたうえで、現在のテキストのレイアウトを取得します。
    /// </summary>
    private TextLayout GetLayout()
    {
        ConfigureTextNode();

        // Text のレイアウトは Size を読むと更新される
        _ = _textNode.Size;
        return _textNode.Layout;
    }

    private int GetLineHeight() =>
        (int)MathF.Ceiling(_textNode.Font.Metrics.LineHeight * _textNode.LineSpacing);

    /// <summary>テキストを表示できる領域の高さ。</summary>
    private int ViewHeight() => Math.Max(1, Size.Y - PaddingTop * 2);

    private int CaretLineIndex(TextLayout layout) =>
        CaretNavigator.FindLine(layout, _cursorPosition);

    /// <summary>キャレットがある行。テキストが空で行がない場合は、1行分の既定値。</summary>
    private TextLine CaretLine(TextLayout layout) =>
        layout.Lines.Count == 0
            ? new TextLine(0, 0, 0, 0, GetLineHeight(), 0)
            : layout.Lines[CaretLineIndex(layout)];

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
    /// キャレットのある行が表示範囲に入るよう、スクロール量を調整します。
    /// </summary>
    private void EnsureCaretVisible()
    {
        var layout = GetLayout();
        var line = CaretLine(layout);
        var viewHeight = ViewHeight();

        if (line.Top < _scrollY)
            _scrollY = line.Top;
        else if (line.Top + line.Height > _scrollY + viewHeight)
            _scrollY = line.Top + line.Height - viewHeight;

        // 末尾に余白ができないように、スクロール量を範囲内に収める
        _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, layout.Size.Y - viewHeight));
    }

    private int Repeats(KeyRepeater repeater, Key key, float deltaTime) =>
        repeater.Poll(key, deltaTime, KeyRepeatDelay, KeyRepeatInterval);

    private void ResetKeyRepeat()
    {
        _backSpaceRepeater.Reset();
        _deleteRepeater.Reset();
        _leftRepeater.Reset();
        _rightRepeater.Reset();
        _upRepeater.Reset();
        _downRepeater.Reset();
        _enterRepeater.Reset();
    }

    private void ResetCursorBlink()
    {
        _cursorVisible = true;
        _cursorBlinkTimer = 0;
    }

    private static void Place(Node node, Vector location)
    {
        if (node.Location != location)
            node.Location = location;
    }

    private static void Place(Container viewport, bool trim, Vector location, VectorInt size)
    {
        viewport.IsTrimmable = trim;
        Place(viewport, location);
        if (trim && !viewport.Size.Equals(size))
            viewport.Size = size;
    }
}
