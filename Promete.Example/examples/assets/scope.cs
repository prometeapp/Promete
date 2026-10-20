using System.Collections;
using System.Drawing;
using Promete.Assets;
using Promete.Audio;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.assets;

/// <summary>
/// Assets API のデモ。スコープごとの読み込みと、非同期の読み込みを示します。
/// </summary>
/// <remarks>
/// AssetManager が返したリソースは Dispose しないでください。解放は <see cref="AssetScope"/> の規則に従って
/// Assets が行います。Scene で読んだものはこのシーンが終わると解放され、Global で読んだものはアプリが終わるまで残ります。
/// </remarks>
[Demo("/assets/scope.demo", "Assets API でアセットを読み込み、シーンと一緒に解放します")]
public class ScopeScene(
    AssetManager manager,
    ConsoleLayer console,
    Keyboard keyboard,
    CoroutineManager coroutine
) : Scene
{
    // Scene スコープ: このシーンのインスタンスが所有する。次のシーンに移ると、同じものを読まない限り解放される
    private readonly Texture2D _ichigo = manager.LoadTexture("assets/ichigo.png", AssetScope.Scene);
    private readonly AudioPlayer _player = new();
    private readonly IAudioSource _sfx = manager.LoadAudio(
        "assets/lineclear.wav",
        AssetScope.Scene
    );

    // Global スコープ: アプリが終わるまで残る。同じファイルでもサイズが違えばグリフソースは 1 つだけ読まれ、共有される
    private readonly Font _smallFont = manager.LoadFont("assets/Koruri.ttf", 16, AssetScope.Global);
    private readonly Font _largeFont = manager.LoadFont("assets/Koruri.ttf", 32, AssetScope.Global);

    public override void OnStart()
    {
        console.Print("Assets API Example\n");
        console.Print("[SPACE]: Play sound effect");
        console.Print("[ESC]: Quit");

        // "./" の付いた別の書き方でも論理パスが同じなので、キャッシュの同じ項目を共有する (読み直さない)
        var sameTexture = manager.LoadTexture("./assets/ichigo.png", AssetScope.Scene);
        Root.Add(new Sprite(_ichigo).Location(32, 96));
        Root.Add(new Sprite(sameTexture).Location(160, 96));

        Root.Add(new Text("Koruri 16px", _smallFont, Color.White).Location(32, 232));
        Root.Add(new Text("Koruri 32px", _largeFont, Color.Lime).Location(32, 256));

        // 非同期版は呼び出したスレッドを止めずに読み、完了したらコルーチンで待って使う
        coroutine.Start(LoadIconsAsync());
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyDown)
            _player.PlayOneShot(_sfx);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _player.Dispose();
    }

    /// <summary>
    /// アイコンの画像を非同期に読み込み、表示します。
    /// </summary>
    /// <returns>コルーチンの列挙子。</returns>
    private IEnumerator LoadIconsAsync()
    {
        var task = manager.LoadTextureAsync("assets/icons.png", AssetScope.Scene);
        yield return new WaitForTask(task);

        Root.Add(new Sprite(task.Result).Location(256, 96));
    }
}
