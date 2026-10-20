using Promete.Assets;
using Promete.Graphics;

namespace Promete.Test.AssetsApi;

/// <summary>
/// シーンのスコープのテスト用に、型ごとに決めたパスを <see cref="AssetScope.Scene"/> で読み込むシーン。
/// 振る舞いは <see cref="Script"/> で型ごとに設定する。テストの最初に <see cref="Script.Reset"/> を呼ぶこと。
/// </summary>
internal abstract class ScriptedScene : Scene
{
    private readonly List<(string Path, Texture2D Texture)> _loaded = [];

    protected ScriptedScene(AssetManager assets)
    {
        Assets = assets;
        foreach (var path in Script.CtorLoads.GetValueOrDefault(GetType(), []))
            Load(path);
    }

    protected AssetManager Assets { get; }

    public override void OnStart()
    {
        foreach (var path in Script.StartLoads.GetValueOrDefault(GetType(), []))
            Load(path);
    }

    /// <summary>
    /// このシーンで読んだテクスチャが、OnDestroy の時点でまだ破棄されていないかを記録する。
    /// </summary>
    public override void OnDestroy()
    {
        var disposed = ((AssetsTestTextureFactory)App.TextureFactory).DisposedHandlesSnapshot();
        foreach (var (path, texture) in _loaded)
            Script.UsableAtDestroy.Add(
                $"{GetType().Name}:{path}={!disposed.Contains(texture.Handle)}"
            );
    }

    /// <summary>
    /// <paramref name="path"/> を <see cref="AssetScope.Scene"/> で読み、ハンドルを <see cref="Script.Handles"/> に記録する。
    /// </summary>
    protected void Load(string path)
    {
        var texture = Assets.LoadTexture(path, AssetScope.Scene);
        _loaded.Add((path, texture));
        Script.Handles[path] = texture.Handle;
    }

    /// <summary>
    /// シーンの型ごとの振る舞いと、記録した結果。
    /// </summary>
    internal static class Script
    {
        /// <summary>コンストラクタで読むパス。</summary>
        public static Dictionary<Type, string[]> CtorLoads { get; } = [];

        /// <summary>OnStart で読むパス。</summary>
        public static Dictionary<Type, string[]> StartLoads { get; } = [];

        /// <summary>最後に読んだときのハンドル (パスごと)。</summary>
        public static Dictionary<string, int> Handles { get; } = [];

        /// <summary>OnDestroy の時点で使えたかどうか (<c>型名:パス=True</c> の形)。</summary>
        public static List<string> UsableAtDestroy { get; } = [];

        public static void Reset()
        {
            CtorLoads.Clear();
            StartLoads.Clear();
            Handles.Clear();
            UsableAtDestroy.Clear();
        }
    }
}

internal sealed class ScriptedSceneA(AssetManager assets) : ScriptedScene(assets);

internal sealed class ScriptedSceneB(AssetManager assets) : ScriptedScene(assets);

internal sealed class ScriptedSceneC(AssetManager assets) : ScriptedScene(assets);

/// <summary>
/// 何も読まないシーン (<see cref="Script"/> で設定しない限り)。
/// </summary>
internal sealed class ScriptedEmptyScene(AssetManager assets) : ScriptedScene(assets);

/// <summary>
/// コンストラクタで読んだ後に、例外を投げるシーン。
/// </summary>
internal sealed class ScriptedThrowingCtorScene : ScriptedScene
{
    public ScriptedThrowingCtorScene(AssetManager assets)
        : base(assets)
    {
        throw new InvalidOperationException("シーンの生成に失敗しました。");
    }
}

/// <summary>
/// OnStart で読んだ後に、例外を投げるシーン。
/// </summary>
internal sealed class ScriptedThrowingOnStartScene(AssetManager assets) : ScriptedScene(assets)
{
    public override void OnStart()
    {
        base.OnStart();
        throw new InvalidOperationException("OnStart で失敗しました。");
    }
}

/// <summary>
/// OnStart の中で <see cref="ScriptedSceneC"/> を LoadScene し、その後に <c>d.png</c> を <see cref="AssetScope.Scene"/> で読むシーン。
/// </summary>
internal sealed class ScriptedNestingScene(AssetManager assets) : ScriptedScene(assets)
{
    public override void OnStart()
    {
        base.OnStart();
        App.LoadScene<ScriptedSceneC>();
        Load("d.png");
    }
}

/// <summary>
/// フィールド初期化子で <c>field.png</c> を <see cref="AssetScope.Scene"/> で読むシーン。
/// フィールド初期化子は、基底の <see cref="Scene"/> のコンストラクタより先に走る。
/// </summary>
internal sealed class FieldInitializerScene(AssetManager assets) : Scene
{
    private readonly Texture2D _field = Record(assets.LoadTexture("field.png", AssetScope.Scene));

    public Texture2D Field => _field;

    private static Texture2D Record(Texture2D texture)
    {
        ScriptedScene.Script.Handles["field.png"] = texture.Handle;
        return texture;
    }
}
