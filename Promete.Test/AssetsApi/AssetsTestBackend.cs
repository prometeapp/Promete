using Promete.Assets;
using Promete.Backends.Headless;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Test.Fakes;
using Promete.Windowing;

namespace Promete.Test.AssetsApi;

/// <summary>
/// テクスチャとフォントの生成・破棄を観測できるようにした、ヘッドレスのバックエンド。
/// テクスチャは <see cref="FakeTextureFactory"/>、フォントは <see cref="FakeFontProvider"/> で作る。
/// それ以外は <see cref="HeadlessBackend"/> (<c>BuildWithHeadless()</c>) と同じ。
/// </summary>
public sealed class AssetsTestBackend : HeadlessBackend
{
    public override TextureFactoryBase SetupTextureFactory() => new AssetsTestTextureFactory();

    public override IFontProvider SetupFontProvider() => new FakeFontProvider();
}

/// <summary>
/// <see cref="FakeTextureFactory"/> に、メインスレッド以外での破棄を数える機能を足したもの。
/// GL のバックエンドは、メインスレッド以外でテクスチャを破棄すると例外を投げるため。
/// </summary>
public sealed class AssetsTestTextureFactory : FakeTextureFactory
{
    private int _offMainThreadDestroyCount;

    /// <summary>
    /// メインスレッド以外でテクスチャが破棄された回数。
    /// </summary>
    public int OffMainThreadDestroyCount => Volatile.Read(ref _offMainThreadDestroyCount);

    /// <summary>
    /// 破棄されたハンドルの一覧を、スレッド安全に複製して返す。
    /// </summary>
    public int[] DisposedHandlesSnapshot()
    {
        lock (DisposedHandles)
            return DisposedHandles.ToArray();
    }

    /// <summary>
    /// 非同期の読み込みはスレッドプールから転送するので、ロックで守る。
    /// </summary>
    protected override int UploadTexture(in TextureUploadRequest request)
    {
        lock (DisposedHandles)
            return base.UploadTexture(request);
    }

    protected override void DestroyTexture(int handle)
    {
        if (PrometeApp.CurrentOrNull is { } app && !app.IsMainThread())
            Interlocked.Increment(ref _offMainThreadDestroyCount);
        lock (DisposedHandles)
            base.DestroyTexture(handle);
    }
}

/// <summary>
/// Assets API のテスト用に、アプリを組み立てて動かすヘルパー。
/// </summary>
internal static class AssetsTestApp
{
    /// <summary>
    /// <paramref name="source"/> をマウントした <see cref="AssetManager"/> を持つアプリを、<see cref="AssetsTestBackend"/> で組み立てる。
    /// シーンは Promete.Test のアセンブリから登録される。
    /// </summary>
    public static PrometeApp Build(
        IAssetSource source,
        Func<PrometeApp.PrometeAppBuilder, PrometeApp.PrometeAppBuilder>? configure = null
    )
    {
        var builder = PrometeApp
            .Create()
            .UseScenesFrom<AssetsTestBackend>()
            .UseAssets(typeof(AssetsTestBackend).Assembly, o => o.Mount(source));
        if (configure != null)
            builder = configure(builder);
        return builder.Build<AssetsTestBackend>(WindowOptions.Default);
    }

    /// <summary>
    /// アプリのテクスチャファクトリを、観測用の <see cref="FakeTextureFactory"/> として取得する。
    /// </summary>
    public static AssetsTestTextureFactory Textures(this PrometeApp app) =>
        (AssetsTestTextureFactory)app.TextureFactory;

    /// <summary>
    /// アプリのフォントプロバイダを、観測用の <see cref="FakeFontProvider"/> として取得する。
    /// </summary>
    public static FakeFontProvider Fonts(this PrometeApp app) =>
        (FakeFontProvider)app.GetPlugin<IFontProvider>();

    /// <summary>
    /// アプリの <see cref="AssetManager"/> を取得する。
    /// </summary>
    public static AssetManager Assets(this PrometeApp app) => app.GetPlugin<AssetManager>();

    /// <summary>
    /// <typeparamref name="TScene"/> から始めて、<see cref="PrometeApp.Start"/> の中で <paramref name="body"/> を実行し、アプリを終了する。
    /// <paramref name="body"/> の例外は、Run から戻った後に投げ直す。<paramref name="body"/> はメインスレッドで動く。
    /// </summary>
    public static void RunWith<TScene>(this PrometeApp app, Action body)
        where TScene : Scene
    {
        Exception? thrown = null;
        app.Start += () =>
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                thrown = e;
            }
            finally
            {
                app.Exit();
            }
        };
        app.Run<TScene>();
        if (thrown != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(thrown);
    }

    /// <summary>
    /// 次のフレームに回された処理 (<see cref="PrometeApp.NextFrame"/>) を、呼び出したスレッドで実行する。
    /// メインスレッド以外で決まった解放は次のフレームに行われるので、それを確かめるときに使う。
    /// </summary>
    public static void PumpFrame(this PrometeApp app) => app.OnUpdate();

    /// <summary>
    /// 専用のスレッド (メインスレッドではない) で <paramref name="action"/> を実行し、終わるまで待つ。例外は投げ直す。
    /// </summary>
    /// <remarks>
    /// <c>Task.Run(...).Wait()</c> は、待つ側のスレッドで処理をその場で実行 (インライン化) することがあり、メインスレッド以外での挙動を確かめられないため。
    /// </remarks>
    public static void RunOnWorkerThread(Action action)
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                thrown = e;
            }
        });
        thread.Start();
        thread.Join();
        if (thrown != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(thrown);
    }

    /// <inheritdoc cref="RunOnWorkerThread(Action)"/>
    public static T RunOnWorkerThread<T>(Func<T> func)
    {
        T result = default!;
        RunOnWorkerThread(() =>
        {
            result = func();
        });
        return result;
    }
}
