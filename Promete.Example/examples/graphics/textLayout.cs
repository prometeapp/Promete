using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/textLayout.demo", "テキストの折り返しと禁則処理")]
public class TextLayoutDemo(Keyboard keyboard) : Scene
{
    private const string SampleText =
        "あのイーハトーヴォのすきとおった風、夏でも底に冷たさをもつ青いそら、"
        + "うつくしい森で飾られたモリーオ市、郊外のぎらぎらひかる草の波。"
        + "Promete is a 2D game engine.";

    private static readonly WrapMode[] WrapModes =
    [
        WrapMode.None,
        WrapMode.Character,
        WrapMode.Word,
        WrapMode.Mixed,
    ];

    private readonly Text _body = new(SampleText, Graphics.Fonts.Font.GetDefault(16), Color.White);
    private readonly Text _status = new("", Graphics.Fonts.Font.GetDefault(14), Color.Yellow);

    private Shape? _frame;

    private int _wrapIndex = 3;
    private int _width = 320;
    private bool _useKinsoku = true;
    private int _maxLines;
    private HorizontalAlignment _alignment = HorizontalAlignment.Left;

    public override void OnStart()
    {
        Root.Add(_status.Location(16, 16));
        Root.Add(_body.Location(16, 96));

        Apply();
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
            return;
        }

        var changed = false;

        if (keyboard.W.IsKeyDown)
        {
            _wrapIndex = (_wrapIndex + 1) % WrapModes.Length;
            changed = true;
        }

        if (keyboard.K.IsKeyDown)
        {
            _useKinsoku = !_useKinsoku;
            changed = true;
        }

        if (keyboard.A.IsKeyDown)
        {
            _alignment = _alignment switch
            {
                HorizontalAlignment.Left => HorizontalAlignment.Center,
                HorizontalAlignment.Center => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left,
            };
            changed = true;
        }

        if (keyboard.L.IsKeyDown)
        {
            _maxLines = _maxLines >= 4 ? 0 : _maxLines + 1;
            changed = true;
        }

        if (keyboard.Left.IsKeyDown)
        {
            _width = Math.Max(64, _width - 32);
            changed = true;
        }

        if (keyboard.Right.IsKeyDown)
        {
            _width = Math.Min(560, _width + 32);
            changed = true;
        }

        if (changed)
            Apply();
    }

    private void Apply()
    {
        _body.WrapMode = WrapModes[_wrapIndex];
        _body.KinsokuMode = _useKinsoku ? KinsokuMode.Standard : KinsokuMode.None;
        _body.HorizontalAlignment = _alignment;
        _body.MaxLines = _maxLines;
        _body.PreferredSize = (_width, 0);

        // レイアウト結果に枠を合わせるため、この時点で確定させる
        _body.UpdateLayout();
        UpdateFrame();

        _status.Content =
            $"[W] 折り返し: {WrapModes[_wrapIndex]}\n"
            + $"[K] 禁則処理: {(_useKinsoku ? "有効" : "無効")}    "
            + $"[A] 整列: {_alignment}    "
            + $"[L] 行数制限: {(_maxLines == 0 ? "なし" : _maxLines.ToString())}\n"
            + $"[←][→] 幅: {_width}px    [ESC] メニューに戻る";
    }

    /// <summary>
    /// レイアウトされた領域を示す枠を引き直します。
    /// </summary>
    private void UpdateFrame()
    {
        if (_frame is not null)
        {
            Root.Remove(_frame);
            _frame.Destroy();
        }

        _frame = Shape
            .CreateRect((0, 0), _body.Size, Color.Transparent, 1, Color.DimGray)
            .Location(16, 96);
        Root.Insert(0, _frame);
    }
}
