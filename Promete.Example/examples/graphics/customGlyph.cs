using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/customGlyph.demo", "外字を本文に差し込む例")]
public class CustomGlyph(Keyboard keyboard) : Scene
{
    /// <summary>
    /// 外字として登録するアイコンのドット絵。X の部分が不透明になります。
    /// </summary>
    private static readonly (string Name, Color Color, string[] Dots)[] Icons =
    [
        (
            "heart",
            Color.FromArgb(255, 90, 120),
            [
                "................",
                "..XXX....XXX....",
                ".XXXXX..XXXXX...",
                "XXXXXXXXXXXXXX..",
                "XXXXXXXXXXXXXX..",
                "XXXXXXXXXXXXXX..",
                ".XXXXXXXXXXXX...",
                ".XXXXXXXXXXXX...",
                "..XXXXXXXXXX....",
                "...XXXXXXXX.....",
                "....XXXXXX......",
                ".....XXXX.......",
                "......XX........",
                "................",
                "................",
                "................",
            ]
        ),
        (
            "star",
            Color.FromArgb(255, 220, 70),
            [
                "................",
                ".......XX.......",
                "......XXXX......",
                "......XXXX......",
                ".....XXXXXX.....",
                "XXXXXXXXXXXXXXXX",
                ".XXXXXXXXXXXXXX.",
                "..XXXXXXXXXXXX..",
                "...XXXXXXXXXX...",
                "...XXXXXXXXXX...",
                "..XXXXX..XXXXX..",
                "..XXXX....XXXX..",
                ".XXX........XXX.",
                "................",
                "................",
                "................",
            ]
        ),
        (
            "coin",
            Color.FromArgb(255, 200, 40),
            [
                "................",
                ".....XXXXXX.....",
                "...XXXXXXXXXX...",
                "..XXXXXXXXXXXX..",
                "..XXXXXXXXXXXX..",
                ".XXXXXXXXXXXXXX.",
                ".XXXX.XXXX.XXXX.",
                ".XXXX.XXXX.XXXX.",
                ".XXXX.XXXX.XXXX.",
                ".XXXXXXXXXXXXXX.",
                "..XXXXXXXXXXXX..",
                "..XXXXXXXXXXXX..",
                "...XXXXXXXXXX...",
                ".....XXXXXX.....",
                "................",
                "................",
            ]
        ),
    ];

    private BitmapGlyphSource? _icons;
    private int _size = 16;
    private Text? _body;

    public override void OnStart()
    {
        // 本文のベースラインにアイコンの下端を合わせるため、
        // 基準となるフォントのアセンダーをベースライン位置として指定する
        var baseline = (int)MathF.Round(Graphics.Fonts.Font.GetDefault(16).Metrics.Ascender);
        _icons = new BitmapGlyphSource(16, baseline);
        foreach (var (name, color, dots) in Icons)
            _icons.Register(name, CreatePixels(dots, color), (16, 16));

        Root.Add(
            new Text(
                "外字は BitmapGlyphSource に登録し、フォントへ重ねて使います。\n"
                    + "PTML の <tex=名前> で本文に差し込めます。\n"
                    + "[↑][↓] 文字サイズの変更    [ESC] メニューに戻る",
                Graphics.Fonts.Font.GetDefault(14),
                Color.Gray
            ).Location(16, 16)
        );

        _body = new Text("", color: Color.White).Location(16, 112);
        Root.Add(_body);

        Apply();
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
            return;
        }

        // 外字は元の大きさの整数倍に解決されるため、本文も 16 の倍数で変化させる
        if (keyboard.Up.IsKeyDown)
        {
            _size = Math.Min(48, _size + 16);
            Apply();
        }
        else if (keyboard.Down.IsKeyDown)
        {
            _size = Math.Max(16, _size - 16);
            Apply();
        }
    }

    public override void OnDestroy()
    {
        _icons?.Dispose();
    }

    private void Apply()
    {
        if (_body is null || _icons is null)
            return;

        // 外字はフォントより優先して参照されるよう、チェーンの先頭に差し込む
        _body.Font = Graphics.Fonts.Font.GetDefault(_size).WithOverride(_icons);
        _body.UseRichText = true;
        _body.Content =
            $"現在のサイズ: {_size}px\n"
            + "ライフ <tex=heart><tex=heart><tex=heart>\n"
            + "スコア <tex=star> 12,345\n"
            + "所持金 <tex=coin> 999\n"
            + "<color=#88ccff>外字は通常の文字と同じように</color>"
            + "<tex=star>折り返しや整列の対象になります。";
        _body.WrapMode = WrapMode.Mixed;
        _body.PreferredSize = (560, 0);
    }

    /// <summary>
    /// ドット絵の定義から RGBA のピクセルデータを生成します。
    /// </summary>
    private static byte[] CreatePixels(string[] dots, Color color)
    {
        var pixels = new byte[16 * 16 * 4];
        for (var y = 0; y < 16; y++)
        for (var x = 0; x < 16; x++)
        {
            if (dots[y][x] != 'X')
                continue;

            var offset = ((y * 16) + x) * 4;
            pixels[offset] = color.R;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.B;
            pixels[offset + 3] = 255;
        }

        return pixels;
    }
}
