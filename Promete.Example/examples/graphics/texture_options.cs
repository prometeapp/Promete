using System.Collections;
using System.Diagnostics;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/texture_options.demo", "TextureOptions と非同期読み込みの例")]
public class Texture_options(ConsoleLayer console, Keyboard keyboard, CoroutineManager coroutine)
    : Scene
{
    private const int AsyncLoadCount = 64;
    private const string AsyncLoadPath = "assets/ichigo.png";

    private readonly List<Texture2D> _ownedTextures = [];
    private readonly List<Texture2D> _asyncTextures = [];
    private readonly List<Node> _asyncNodes = [];

    private Texture2D[] _asyncSheet = [];
    private Sprite _spinner = null!;
    private Text _status = null!;
    private Text _sheetStatus = null!;
    private int _frame;
    private bool _loading;

    public override void OnStart()
    {
        console.Print("TextureOptions / Async Load");
        console.Print("[SPACE] Load asynchronously");
        console.Print("[ESC] Return");

        BuildFilterRow();
        BuildAddressRow();
        BuildAsyncRow();
    }

    public override void OnUpdate()
    {
        _frame++;
        _spinner.Angle += (180 * Time.DeltaTime).Degrees;

        if (keyboard.Space.IsKeyDown && !_loading)
            coroutine.Start(LoadAsync());

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        foreach (var texture in _ownedTextures)
            texture.Dispose();
        DisposeAsyncResources();
    }

    private void BuildFilterRow()
    {
        var factory = App.TextureFactory;
        var nearest = factory.Load(AsyncLoadPath, new TextureOptions(TextureFilterMode.Nearest));
        var linear = factory.Load(AsyncLoadPath, new TextureOptions(TextureFilterMode.Linear));
        _ownedTextures.AddRange([nearest, linear]);

        Root.AddRange(
            new Text("Filter: Nearest").Location(16, 64),
            new Sprite(nearest).Location(16, 88).Scale(6, 6),
            new Text("Filter: Linear").Location(144, 64),
            new Sprite(linear).Location(144, 88).Scale(6, 6)
        );
    }

    /// <summary>
    /// UV を 0～3 に広げた「見かけ上のテクスチャ」を作り、アドレスモードの違いを確かめる。
    /// ハンドルは元のテクスチャと共有するので、破棄は元のテクスチャ側のみで行う。
    /// </summary>
    private void BuildAddressRow()
    {
        var factory = App.TextureFactory;
        var modes = new[]
        {
            TextureAddressMode.Clamp,
            TextureAddressMode.Repeat,
            TextureAddressMode.Mirror,
        };

        for (var i = 0; i < modes.Length; i++)
        {
            var source = factory.Load(
                AsyncLoadPath,
                new TextureOptions(TextureFilterMode.Nearest, modes[i])
            );
            _ownedTextures.Add(source);

            var tiled = new Texture2D(
                source.Handle,
                (source.Size.X * 3, source.Size.Y * 3),
                _ => { },
                (0, 0),
                (3, 3)
            );

            var x = 16 + (i * 120);
            Root.AddRange(
                new Text($"Address: {modes[i]}").Location(x, 216),
                new Sprite(tiled).Location(x, 240).Scale(2, 2)
            );
        }
    }

    private void BuildAsyncRow()
    {
        _spinner = new Sprite(_ownedTextures[0]).Location(40, 400).Pivot(0.5f, 0.5f).Scale(2, 2);
        _status = new Text("[SPACE] to load").Location(80, 384);
        _sheetStatus = new Text(string.Empty).Location(80, 408);
        Root.AddRange(_spinner, _status, _sheetStatus);
    }

    /// <summary>
    /// 同じ画像を複数回と、スプライトシートを非同期に読み込む。
    /// 読み込み中も、スピナーが回り続けることを確認できる。
    /// </summary>
    private IEnumerator LoadAsync()
    {
        _loading = true;
        DisposeAsyncResources();
        _status.Content = "Loading...";
        _sheetStatus.Content = string.Empty;

        var startFrame = _frame;
        var stopwatch = Stopwatch.StartNew();
        var factory = App.TextureFactory;

        var texturesTask = Task.WhenAll(
            Enumerable
                .Range(0, AsyncLoadCount)
                .Select(_ =>
                    factory.LoadAsync(
                        AsyncLoadPath,
                        new TextureOptions(TextureFilterMode.Linear)
                    )
                )
        );
        var sheetTask = factory.LoadSpriteSheetAsync("assets/icons.png", 3, 1, (32, 32));

        yield return new WaitForTask(texturesTask);
        yield return new WaitForTask(sheetTask);

        stopwatch.Stop();
        _asyncTextures.AddRange(texturesTask.Result);
        _asyncSheet = sheetTask.Result;

        for (var i = 0; i < _asyncTextures.Count; i++)
        {
            var sprite = new Sprite(_asyncTextures[i])
                .Location(400 + ((i % 8) * 20), 64 + ((i / 8) * 20));
            _asyncNodes.Add(sprite);
            Root.Add(sprite);
        }

        for (var i = 0; i < _asyncSheet.Length; i++)
        {
            var sprite = new Sprite(_asyncSheet[i]).Location(400 + (i * 40), 240);
            _asyncNodes.Add(sprite);
            Root.Add(sprite);
        }

        _status.Content =
            $"Loaded {_asyncTextures.Count} textures: {stopwatch.ElapsedMilliseconds} ms, {_frame - startFrame} frames";
        _sheetStatus.Content =
            $"Sprite sheet: {_asyncSheet.Length} cells, IsSubTexture = {_asyncSheet[0].IsSubTexture}";
        _loading = false;
    }

    private void DisposeAsyncResources()
    {
        foreach (var node in _asyncNodes)
            node.Destroy();
        _asyncNodes.Clear();

        foreach (var texture in _asyncTextures)
            texture.Dispose();
        _asyncTextures.Clear();

        // 全セルは 1 枚のテクスチャを共有するので、1 つ破棄すれば解放される
        if (_asyncSheet.Length > 0)
            _asyncSheet[0].Dispose();
        _asyncSheet = [];
    }
}
