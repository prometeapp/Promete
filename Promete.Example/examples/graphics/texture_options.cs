using System.Collections;
using System.Diagnostics;
using Promete.Assets;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/texture_options.demo", "TextureOptions と非同期読み込みの例")]
public class Texture_options(
    ConsoleLayer console,
    Keyboard keyboard,
    CoroutineManager coroutine,
    AssetManager assets
) : Scene
{
    private const int AsyncLoadCount = 64;
    private const string AsyncLoadPath = "assets/ichigo.png";

    private readonly List<Node> _asyncNodes = [];

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
        ClearAsyncNodes();
    }

    private void BuildFilterRow()
    {
        var nearest = assets.LoadTexture(
            AsyncLoadPath,
            new TextureOptions(TextureFilterMode.Nearest),
            AssetScope.Scene
        );
        var linear = assets.LoadTexture(
            AsyncLoadPath,
            new TextureOptions(TextureFilterMode.Linear),
            AssetScope.Scene
        );

        Root.AddRange(
            new Text("Filter: Nearest").Location(16, 64),
            new Sprite(nearest).Location(16, 88).Scale(6, 6),
            new Text("Filter: Linear").Location(144, 64),
            new Sprite(linear).Location(144, 88).Scale(6, 6)
        );
    }

    /// <summary>
    /// UV を 0～3 に広げた「見かけ上のテクスチャ」を作り、アドレスモードの違いを確かめる。
    /// ハンドルは元のテクスチャと共有するので、解放は AssetManager (Scene スコープ) に任せる。
    /// </summary>
    private void BuildAddressRow()
    {
        var modes = new[]
        {
            TextureAddressMode.Clamp,
            TextureAddressMode.Repeat,
            TextureAddressMode.Mirror,
        };

        for (var i = 0; i < modes.Length; i++)
        {
            var source = assets.LoadTexture(
                AsyncLoadPath,
                new TextureOptions(TextureFilterMode.Nearest, modes[i]),
                AssetScope.Scene
            );

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
        var spinnerTexture = assets.LoadTexture(
            AsyncLoadPath,
            new TextureOptions(TextureFilterMode.Nearest),
            AssetScope.Scene
        );
        _spinner = new Sprite(spinnerTexture).Location(40, 400).Pivot(0.5f, 0.5f).Scale(2, 2);
        _status = new Text("[SPACE] to load").Location(80, 384);
        _sheetStatus = new Text(string.Empty).Location(80, 408);
        Root.AddRange(_spinner, _status, _sheetStatus);
    }

    /// <summary>
    /// 同じ画像を複数回と、スプライトシートを非同期に読み込む。
    /// 読み込み中も、スピナーが回り続けることを確認できる。
    /// </summary>
    /// <remarks>
    /// AssetManager は同じパスと同じオプションの読み込みを 1 つにまとめるので、同じ画像を 64 回読んでも実際に読むのは 1 回です。
    /// 2 回目以降は、キャッシュされたテクスチャがすぐ返ります。
    /// </remarks>
    private IEnumerator LoadAsync()
    {
        _loading = true;
        ClearAsyncNodes();
        _status.Content = "Loading...";
        _sheetStatus.Content = string.Empty;

        var startFrame = _frame;
        var stopwatch = Stopwatch.StartNew();

        var texturesTask = Task.WhenAll(
            Enumerable
                .Range(0, AsyncLoadCount)
                .Select(_ =>
                    assets.LoadTextureAsync(
                        AsyncLoadPath,
                        new TextureOptions(TextureFilterMode.Linear),
                        AssetScope.Scene
                    )
                )
        );
        var sheetTask = assets.LoadSpriteSheetAsync(
            "assets/icons.png",
            3,
            1,
            (32, 32),
            AssetScope.Scene
        );

        yield return new WaitForTask(texturesTask);
        yield return new WaitForTask(sheetTask);

        stopwatch.Stop();
        var textures = texturesTask.Result;
        var sheet = sheetTask.Result;

        for (var i = 0; i < textures.Length; i++)
        {
            var sprite = new Sprite(textures[i]).Location(
                400 + ((i % 8) * 20),
                64 + ((i / 8) * 20)
            );
            _asyncNodes.Add(sprite);
            Root.Add(sprite);
        }

        for (var i = 0; i < sheet.Length; i++)
        {
            var sprite = new Sprite(sheet[i]).Location(400 + (i * 40), 240);
            _asyncNodes.Add(sprite);
            Root.Add(sprite);
        }

        _status.Content =
            $"Loaded {textures.Length} textures: {stopwatch.ElapsedMilliseconds} ms, {_frame - startFrame} frames";
        _sheetStatus.Content =
            $"Sprite sheet: {sheet.Length} cells, IsSubTexture = {sheet[0].IsSubTexture}";
        _loading = false;
    }

    /// <summary>
    /// 非同期の読み込みで追加したノードを外す。テクスチャは AssetManager が解放するので、ここでは破棄しない。
    /// </summary>
    private void ClearAsyncNodes()
    {
        foreach (var node in _asyncNodes)
            node.Destroy();
        _asyncNodes.Clear();
    }
}
