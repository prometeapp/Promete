using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/font.demo", "フォントの描画例")]
public class font(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly (string name, string? path, int size, bool antialias)[] _fontDefinitions =
    [
        ("Default", null, 16, true),
        ("Default (w/o antialiasing)", null, 16, false),
        ("美咲ゴシック", "assets/MisakiGothic.ttf", 8, false),
        ("JF-Dot-Shinonome14", "assets/JfDotShinonome14.ttf", 14, false),
        ("Koruri", "assets/Koruri.ttf", 16, true)
    ];

    private readonly List<Text> _menuItems = [];

    private readonly Text _preview = new("""
                                         あのイーハトーヴォのすきとおった風、
                                         夏でも底に冷たさをもつ青いそら、
                                         うつくしい森で飾られたモリーオ市、
                                         郊外のぎらぎらひかる草の波。

                                         [↑][↓]キーでフォントを選択
                                         [Enter]キーでコンソールのフォントを変更
                                         [D]キーでダークモード/ライトモード切替
                                         [ESC]キーでメニューに戻る
                                         """, Font.GetDefault(), Color.White);

    private int _index;
    private bool _isDarkMode = true;

    public override void OnStart()
    {
        var i = 0;
        foreach (var (name, path, size, antialias) in _fontDefinitions)
        {
            var font = path == null
                ? Font.GetDefault(size, FontStyle.Normal, antialias)
                : Font.FromFile(path, size, isAntialiased: antialias);
            var text = new Text(name, font, Color.White)
                .Location(16, 24 + i * 24);

            _menuItems.Add(text);
            i++;
        }

        Root.AddRange(_menuItems);
        Root.Add(_preview.Location(256, 24));

        Choose();
    }

    public override void OnUpdate()
    {
        if (keyboard.Up.IsKeyDown)
        {
            _index--;
            if (_index < 0) _index = _menuItems.Count - 1;
            Choose();
        }
        else if (keyboard.Down.IsKeyDown)
        {
            _index++;
            if (_index >= _menuItems.Count) _index = 0;
            Choose();
        }
        else if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
        else if (keyboard.D.IsKeyDown)
        {
            _isDarkMode = !_isDarkMode;
            _preview.Color = _isDarkMode ? Color.White : Color.Black;
            foreach (var (x, i) in _menuItems.Select((x, i) => (x, i)))
            {
                if (i == _index)
                    x.Color = Color.Red;
                else
                    x.Color = _isDarkMode ? Color.White : Color.Black;
            }

            App.BackgroundColor = _isDarkMode ? Color.Black : Color.White;
        }
        else if (keyboard.Enter.IsKeyDown)
        {
            console.Font = _menuItems[_index].Font;
            console.Clear();
            console.Print($"Set console font to {_fontDefinitions[_index].name}.");
        }
    }

    public override void OnDestroy()
    {
        App.BackgroundColor = Color.Black;
    }

    private void Choose()
    {
        _preview.Font = _menuItems[_index].Font;
        _menuItems.ForEach(x =>
        {
            x.Color = _isDarkMode ? Color.White : Color.Black;
        });
        _menuItems[_index].Color = Color.Red;
    }
}
