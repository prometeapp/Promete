using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Promete.Graphics.Fonts;
using Promete.Nodes;
using Promete.Windowing;

namespace Promete;

/// <summary>
/// 画面上に簡易な文字出力を行うレイヤーを提供する Promete プラグインです。
/// </summary>
public class ConsoleLayer
{
    private readonly List<string> _consoleBuffer = [];

    private readonly Text _text;
    private readonly IWindow _window;
    private int _maxLine;

    public ConsoleLayer(PrometeApp app, IWindow window)
    {
        _window = window;
        _text = new Text("", Font.GetDefault(), Color.White);
        _maxLine = CalculateMaxLine();

        app.SceneWillChange += Clear;
        window.Update += () => { _text.Update(); };
        window.Render += () => { app.RenderNode(_text); };
        window.Resize += () => { _maxLine = CalculateMaxLine(); };
        window.PostUpdate += UpdateConsole;
    }

    /// <summary>
    /// 現在のコンソール上のカーソル位置を取得または設定します。
    /// </summary>
    public VectorInt Cursor { get; set; }

    /// <summary>
    /// コンソールで使用するフォントを取得または設定します。
    /// </summary>
    public Font Font
    {
        get => _text.Font;
        set
        {
            _text.Font = value;

            _maxLine = CalculateMaxLine();
        }
    }

    /// <summary>
    /// 現在の文字色を取得または設定します。
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// コンソール上の文字列を完全に消去します。
    /// </summary>
    public void Clear()
    {
        _consoleBuffer.Clear();
        Cursor = VectorInt.Zero;
    }

    /// <summary>
    /// コンソール上に、指定したオブジェクトを出力します。
    /// </summary>
    /// <param name="obj">表示するデータ。</param>
    public void Print(object? obj)
    {
        var line = obj as string ?? obj?.ToString() ?? "null";
        var (x, y) = Cursor;
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        if (y < _consoleBuffer.Count)
        {
            // 置換
            _consoleBuffer[y] = _consoleBuffer[y].ReplaceAt(x, line);
        }
        else
        {
            // 挿入
            _consoleBuffer.AddRange(Enumerable.Repeat("", y - _consoleBuffer.Count));
            _consoleBuffer.Add(new string(' ', x) + line);
        }

        Cursor = new VectorInt(0, y + 1);
    }

    private void UpdateConsole()
    {
        var buf = _consoleBuffer.Count > _maxLine
            ? _consoleBuffer.Skip(_consoleBuffer.Count - _maxLine)
            : _consoleBuffer;

        _text.Color = TextColor;
        _text.Content = string.Join('\n', buf);
    }

    private int CalculateMaxLine()
    {
        var textToTest = "";
        var l = 0;
        Rect bounds;
        do
        {
            textToTest += "A\n";
            bounds = _text.Font.GetTextBounds(textToTest, _text.Options);
            l++;
        } while (bounds.Height < _window.Height);

        return l - 1;
    }
}
