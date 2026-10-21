using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Promete.Assets.Infrastructure;
using Promete.Audio;
using Promete.Graphics;
using Promete.Graphics.Fonts;

namespace Promete.Assets;

/// <summary>
/// アセットの読み込みを管理するプラグインです。<see cref="AssetsAppBuilderExtension"/> の <c>UseAssets</c> で登録します。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AssetsOptions"/> で指定したマウントは、このクラスのコンストラクタで行います。
/// プラグインの <c>OnStart</c> は登録した順に呼ばれるので、<c>OnStart</c> でマウントすると、先に登録された他のプラグインが
/// <c>OnStart</c> の中で読み込めないためです。
/// </para>
/// <para>
/// 読み込んだアセットは、(読み込みの種類, 正規化した論理パス, オプション) の組をキーにキャッシュされます。
/// 同じキーで読み込むと、読み直さずに同じものを返します。各アセットは、それを読み込んだスコープ (<see cref="AssetScope"/>) の集合に所有され、
/// 集合が空になったときに解放されます。
/// </para>
/// <para>
/// <b>このクラスが返したリソースは、利用者が Dispose してはいけません。</b> 解放は <see cref="Unload(string, AssetScope)"/> などで行います。
/// <see cref="Texture2D.Dispose"/> は冪等ではないため、利用者とスコープの両方が Dispose すると、無関係なテクスチャを破棄することがあります。
/// </para>
/// <para>
/// リソースの Dispose は、常にメインスレッドで行います。メインスレッド以外で解放が決まった場合は、次のフレームに行います。
/// </para>
/// <para>
/// テクスチャの同期の読み込み (<see cref="LoadTexture(string, AssetScope)"/> など) は、バックエンドによってはメインスレッドからしか呼べません
/// (GL / Vulkan では、メインスレッド以外から呼ぶと <see cref="InvalidOperationException"/> になります)。
/// ワーカースレッドでテクスチャを読むときは、<c>...Async</c> のメソッド (<see cref="LoadTextureAsync(string, AssetScope, CancellationToken)"/> など) を使ってください。
/// 非同期版は、ファイルの読み込みと画像のデコードをスレッドプールで行い、GPU への転送だけをメインスレッドで行います。
/// </para>
/// <para>
/// 汎用の <see cref="Load{T}(string, string, Func{Stream, T}, AssetScope)"/>、音声、フォントは、ワーカースレッドから <see cref="AssetScope.Global"/> で読めます。
/// </para>
/// </remarks>
public sealed class AssetManager : IDisposable
{
    /// <summary>
    /// グリフソースだけを作るときに渡すフォントサイズ。グリフソースはサイズに依存しないため、値は使われません。
    /// </summary>
    private const float GlyphSourceProbeSize = 16;

    /// <summary>
    /// 音声の形式を判定するために読む、先頭のバイト数。<see cref="IAudioDecoder.CanDecode"/> に渡されます。
    /// </summary>
    private const int AudioHeaderLength = 64;

    private readonly object _lock = new();
    private readonly Dictionary<CacheKey, Entry> _cache = new();
    private readonly object _globalOwner = new();
    private readonly PrometeApp _app;
    private readonly IAudioDecoder[] _audioDecoders;
    private readonly ConcurrentQueue<Action> _pendingReleases = new();
    private readonly Stack<SceneScope> _constructingScopes = new();
    private readonly Dictionary<Scene, SceneScope> _sceneScopes = new(
        ReferenceEqualityComparer.Instance
    );
    private int _disposed;
    private int _flushScheduled;

    /// <summary>
    /// <see cref="AssetManager"/> の新しいインスタンスを初期化し、設定に従ってソースをマウントします。
    /// </summary>
    /// <param name="assembly">Assets の属性を探すアセンブリ。無い場合は <see langword="null"/>。</param>
    /// <param name="options">明示的なマウントの設定。</param>
    /// <param name="app">メインスレッドの判定と、解放の予約に使うアプリ。</param>
    /// <param name="audioDecoders">音声の判定に使うデコーダー。登録された順に判定します。</param>
    /// <param name="manifestLoader">
    /// <paramref name="assembly"/> の <see cref="PrometeAssetSourceAttribute"/> からソースを作る実装。
    /// 属性によるマウントは、明示的なマウントより先に、優先度 <see cref="int.MinValue"/> で行います。
    /// </param>
    /// <exception cref="DirectoryNotFoundException">マウントするフォルダが存在しない場合。</exception>
    /// <exception cref="FileNotFoundException">属性が指すマニフェストやパッケージが見つからない場合。</exception>
    internal AssetManager(
        Assembly? assembly,
        AssetsOptions options,
        PrometeApp app,
        IAudioDecoder[] audioDecoders,
        IAssetManifestLoader manifestLoader
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(audioDecoders);
        ArgumentNullException.ThrowIfNull(manifestLoader);

        _app = app;
        _audioDecoders = audioDecoders;

        FileSystem = new AssetFileSystem();
        try
        {
            // 属性によるマウントは、明示的なマウントより低い優先度で先に行う
            var attributes =
                assembly?.GetCustomAttributes<PrometeAssetSourceAttribute>().ToArray() ?? [];
            foreach (var attribute in attributes)
            {
                FileSystem.Mount(manifestLoader.CreateSource(assembly!, attribute), int.MinValue);
            }

            foreach (var (factory, priority) in options.Mounts)
            {
                FileSystem.Mount(factory(), priority);
            }

            if (attributes.Length == 0 && options.Mounts.Count == 0)
            {
                FileSystem.EmptyHint = CreateEmptyHint(assembly);
            }
        }
        catch
        {
            // ロールバックの失敗で、利用者が知るべき元の例外を隠さない
            try
            {
                FileSystem.Dispose();
            }
            catch
            {
                // Dispose 側の例外は捨てる。元の例外を優先して再スローする
            }

            throw;
        }

        // シーンの生成と破棄を受け取り、シーンのスコープを作る
        app.AddSceneLifecycleHook(new SceneScopeHook(this));
    }

    /// <summary>
    /// キャッシュのエントリの種類です。同じパスでも種類が違えば、別のエントリになります。
    /// </summary>
    private enum CacheKind
    {
        Texture,
        SpriteGrid,
        SpriteRects,
        NineSlice,
        FontSource,
        Audio,
        Custom,
    }

    /// <summary>
    /// アセットを読み出す仮想ファイルシステムを取得します。
    /// </summary>
    public AssetFileSystem FileSystem { get; }

    /// <summary>
    /// テクスチャを読み込みます。 (v2.4~)
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture2D LoadTexture(string path, AssetScope scope)
    {
        return LoadTexture(path, TextureOptions.Default, scope);
    }

    /// <summary>
    /// テクスチャを読み込みます。 (v2.4~)
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="options">サンプリングの設定。キャッシュのキーに含まれます。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture2D LoadTexture(string path, TextureOptions options, AssetScope scope)
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(CacheKind.Texture, normalized, options);
        return GetOrLoad(
            key,
            owner,
            () =>
                ReadWith(
                    normalized,
                    stream => _app.TextureFactory.Load(stream, options),
                    leaveOpen: false
                ),
            ReleaseDisposable
        );
    }

    /// <summary>
    /// 画像を読み込み、格子状に切り抜きます (<see cref="TextureFactoryBase.LoadSpriteSheet(Stream, int, int, VectorInt)"/>)。 (v2.4~)
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="horizontalCount">横方向のセルの数。</param>
    /// <param name="verticalCount">縦方向のセルの数。</param>
    /// <param name="size">セルの大きさ。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合や、分割が画像に収まらない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        AssetScope scope
    )
    {
        return LoadSpriteSheet(
            path,
            horizontalCount,
            verticalCount,
            size,
            TextureOptions.Default,
            scope
        );
    }

    /// <summary>
    /// 画像を読み込み、格子状に切り抜きます (<see cref="TextureFactoryBase.LoadSpriteSheet(Stream, int, int, VectorInt, TextureOptions)"/>)。 (v2.4~)
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="horizontalCount">横方向のセルの数。</param>
    /// <param name="verticalCount">縦方向のセルの数。</param>
    /// <param name="size">セルの大きさ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合や、分割が画像に収まらない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        TextureOptions options,
        AssetScope scope
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(
            CacheKind.SpriteGrid,
            normalized,
            (horizontalCount, verticalCount, size, options)
        );
        var cells = GetOrLoad(
            key,
            owner,
            () =>
                ReadWith(
                    normalized,
                    stream =>
                        _app.TextureFactory.LoadSpriteSheet(
                            stream,
                            horizontalCount,
                            verticalCount,
                            size,
                            options
                        ),
                    leaveOpen: false
                ),
            ReleaseTextureArray
        );
        return (Texture2D[])cells.Clone();
    }

    /// <summary>
    /// 画像を読み込み、任意の矩形で切り抜きます (<see cref="TextureFactoryBase.LoadSpriteSheet(Stream, ReadOnlySpan{RectInt})"/>)。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// キャッシュのキーには、矩形の並び (順序と各要素の値) を使います。呼び出した後に元の配列を書き換えても、キャッシュには影響しません。
    /// </remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="rects">切り抜く領域 (ピクセル単位、左上が原点)。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合や、矩形が画像の範囲外の場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture2D[] LoadSpriteSheet(
        string path,
        AssetScope scope,
        params ReadOnlySpan<RectInt> rects
    )
    {
        return LoadSpriteRects(path, TextureOptions.Default, scope, rects.ToArray());
    }

    /// <summary>
    /// 画像を読み込み、任意の矩形で切り抜きます (<see cref="TextureFactoryBase.LoadSpriteSheet(Stream, TextureOptions, ReadOnlySpan{RectInt})"/>)。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadSpriteSheet(string, AssetScope, ReadOnlySpan{RectInt})" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="rects">切り抜く領域 (ピクセル単位、左上が原点)。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合や、矩形が画像の範囲外の場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture2D[] LoadSpriteSheet(
        string path,
        TextureOptions options,
        AssetScope scope,
        params ReadOnlySpan<RectInt> rects
    )
    {
        return LoadSpriteRects(path, options, scope, rects.ToArray());
    }

    /// <summary>
    /// 9 スライステクスチャを読み込みます (<see cref="TextureFactoryBase.Load9Sliced(Stream, int, int, int, int)"/>)。 (v2.4~)
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="left">左端の幅。</param>
    /// <param name="top">上端の高さ。</param>
    /// <param name="right">右端の幅。</param>
    /// <param name="bottom">下端の高さ。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture9Sliced Load9Sliced(
        string path,
        int left,
        int top,
        int right,
        int bottom,
        AssetScope scope
    )
    {
        return Load9Sliced(path, left, top, right, bottom, TextureOptions.Default, scope);
    }

    /// <summary>
    /// 9 スライステクスチャを読み込みます (<see cref="TextureFactoryBase.Load9Sliced(Stream, int, int, int, int, TextureOptions)"/>)。 (v2.4~)
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="left">左端の幅。</param>
    /// <param name="top">上端の高さ。</param>
    /// <param name="right">右端の幅。</param>
    /// <param name="bottom">下端の高さ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Texture9Sliced Load9Sliced(
        string path,
        int left,
        int top,
        int right,
        int bottom,
        TextureOptions options,
        AssetScope scope
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(
            CacheKind.NineSlice,
            normalized,
            (left, top, right, bottom, options)
        );
        return GetOrLoad(
            key,
            owner,
            () =>
                ReadWith(
                    normalized,
                    stream =>
                        _app.TextureFactory.Load9Sliced(stream, left, top, right, bottom, options),
                    leaveOpen: false
                ),
            ReleaseDisposable
        );
    }

    /// <summary>
    /// フォントを読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <para>
    /// フォントのデータ (<see cref="IGlyphSource"/>) はパスごとに 1 つだけ読み込み、サイズやスタイルが違っても共有します。
    /// スコープが解放するのは、この <see cref="IGlyphSource"/> です。
    /// </para>
    /// <para>
    /// ストリームからフォントを読めない環境 (Web) では、実ファイルを持つソース (<see cref="IFileBackedAssetSource"/>) のファイルのパスから読みます。
    /// その場合のグリフソースは環境の側でキャッシュされるため、スコープを解放しても破棄されません。
    /// </para>
    /// </remarks>
    /// <param name="path">フォントファイルの論理パス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだフォント。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="NotSupportedException">ストリームからフォントを読めない環境で、ファイルのパスも得られない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Font LoadFont(string path, float size, AssetScope scope)
    {
        return LoadFont(path, size, FontStyle.Normal, true, scope);
    }

    /// <summary>
    /// フォントを読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadFont(string, float, AssetScope)" path="/remarks"/></remarks>
    /// <param name="path">フォントファイルの論理パス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだフォント。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="NotSupportedException">ストリームからフォントを読めない環境で、ファイルのパスも得られない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Font LoadFont(string path, float size, FontStyle style, AssetScope scope)
    {
        return LoadFont(path, size, style, true, scope);
    }

    /// <summary>
    /// フォントを読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadFont(string, float, AssetScope)" path="/remarks"/></remarks>
    /// <param name="path">フォントファイルの論理パス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだフォント。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="NotSupportedException">ストリームからフォントを読めない環境で、ファイルのパスも得られない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Font LoadFont(
        string path,
        float size,
        FontStyle style,
        bool isAntialiased,
        AssetScope scope
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var source = GetOrLoadGlyphSource(normalized, owner);
        return Font.FromGlyphSource(source, size, style, isAntialiased);
    }

    /// <summary>
    /// 音声を読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <para>
    /// 形式の判定とデコードは、DI に登録された <see cref="IAudioDecoder"/> で行います。利用者が登録したものが、標準のもの (wav / ogg) より先に判定されます。
    /// </para>
    /// <para>
    /// 返した <see cref="IAudioSource"/> はキャッシュされ、複数の呼び出し元で共有されます。
    /// <see cref="IDisposable"/> を実装していれば、スコープの解放で Dispose されます。
    /// </para>
    /// </remarks>
    /// <param name="path">音声ファイルの論理パス。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだ音声。Dispose しないでください (解放は Unload かスコープに任せます)。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="NotSupportedException">デコードできるデコーダーが無い場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public IAudioSource LoadAudio(string path, AssetScope scope)
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(CacheKind.Audio, normalized, null);
        return GetOrLoad(
            key,
            owner,
            () => ReadWith(normalized, stream => DecodeAudio(normalized, stream), leaveOpen: true),
            ReleaseDisposable
        );
    }

    /// <summary>
    /// 任意の型のアセットを、指定した処理で読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <para>
    /// キャッシュのキーは (<paramref name="path"/>, <paramref name="key"/>, <typeparamref name="T"/>) です。型付きのメソッド (<see cref="LoadTexture(string, AssetScope)"/> など) のキャッシュとは共有しません。
    /// 同じパスを別の読み方で読む場合は、<paramref name="key"/> を変えてください。
    /// </para>
    /// <para>
    /// ストリームは <paramref name="factory"/> が戻った後に閉じます。<paramref name="factory"/> の外にストリームを持ち出す場合は、
    /// <see cref="Load{T}(string, string, Func{Stream, T}, AssetScope, bool)"/> を使ってください。
    /// 結果が <see cref="IDisposable"/> を実装していれば、スコープの解放で Dispose されます。
    /// </para>
    /// </remarks>
    /// <typeparam name="T">アセットの型。</typeparam>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="key">読み方を区別するキー。</param>
    /// <param name="factory">ストリームからアセットを作る処理。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <returns>読み込んだアセット。Dispose しないでください (解放は Unload かスコープに任せます)。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public T Load<T>(string path, string key, Func<Stream, T> factory, AssetScope scope)
    {
        return Load(path, key, factory, scope, leaveOpen: false);
    }

    /// <summary>
    /// 任意の型のアセットを、指定した処理で読み込みます。ストリームの所有権を <paramref name="factory"/> に渡せます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="Load{T}(string, string, Func{Stream, T}, AssetScope)" path="/remarks/para[1]"/>
    /// <para>
    /// <paramref name="leaveOpen"/> が <see langword="true"/> の場合、<paramref name="factory"/> が正常に戻った後はストリームを閉じません。閉じる責任は <paramref name="factory"/> (と、その結果) に移ります。
    /// <paramref name="factory"/> が例外を投げた場合は、どちらの場合もストリームを閉じます。
    /// </para>
    /// </remarks>
    /// <typeparam name="T">アセットの型。</typeparam>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="key">読み方を区別するキー。</param>
    /// <param name="factory">ストリームからアセットを作る処理。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="leaveOpen"><see langword="true"/> なら、<paramref name="factory"/> が戻った後もストリームを閉じません。</param>
    /// <returns>読み込んだアセット。Dispose しないでください (解放は Unload かスコープに任せます)。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public T Load<T>(
        string path,
        string key,
        Func<Stream, T> factory,
        AssetScope scope,
        bool leaveOpen
    )
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        // 型を含めるので、型付きの読み込みとはキャッシュを共有しない
        var cacheKey = new CacheKey(CacheKind.Custom, normalized, (key, typeof(T)));
        return GetOrLoad(
            cacheKey,
            owner,
            () => ReadWith(normalized, factory, leaveOpen),
            ReleaseDisposable
        );
    }

    /// <summary>
    /// テクスチャを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <para>
    /// 同じキーの読み込みが実行中なら、それを共有します。スコープは呼び出した時点で決まり、その時点で所有者に加わります。
    /// 完了する前にスコープが解放されて所有者がいなくなった場合、結果は完了した時点で解放され、返す <see cref="Task"/> は <see cref="OperationCanceledException"/> で失敗します
    /// (<see cref="AssetManager"/> が破棄された場合は <see cref="ObjectDisposedException"/>)。待っている全員が、同じ例外で失敗します。
    /// ただし、同じ読み込みを別のスコープも待っていれば、解放されず、結果をそのスコープに返します。
    /// </para>
    /// <para>
    /// <paramref name="cancellationToken"/> は、呼び出し元が待つのをやめるためのものです。共有している読み込み自体は止まらず、結果はスコープに残ります。
    /// </para>
    /// <para>
    /// 引数とスコープの誤り (<see cref="ArgumentException"/>、<see cref="InvalidOperationException"/>、<see cref="ObjectDisposedException"/>) は、
    /// 返す <see cref="Task"/> ではなく、この呼び出しで例外になります。
    /// キャンセル済みのトークンは例外にせず、キャンセル済みの <see cref="Task"/> を返します (読み込みは始めません)。
    /// </para>
    /// </remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    public Task<Texture2D> LoadTextureAsync(
        string path,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        return LoadTextureAsync(path, TextureOptions.Default, scope, cancellationToken);
    }

    /// <summary>
    /// テクスチャを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    public Task<Texture2D> LoadTextureAsync(
        string path,
        TextureOptions options,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(CacheKind.Texture, normalized, options);
        return GetOrLoadAsync(
            key,
            owner,
            () =>
                ReadWithAsync(
                    normalized,
                    stream => _app.TextureFactory.LoadAsync(stream, options),
                    leaveOpen: false
                ),
            ReleaseDisposable,
            cancellationToken
        );
    }

    /// <summary>
    /// 画像を非同期に読み込み、格子状に切り抜きます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="horizontalCount">横方向のセルの数。</param>
    /// <param name="verticalCount">縦方向のセルの数。</param>
    /// <param name="size">セルの大きさ。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    public Task<Texture2D[]> LoadSpriteSheetAsync(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        return LoadSpriteSheetAsync(
            path,
            horizontalCount,
            verticalCount,
            size,
            TextureOptions.Default,
            scope,
            cancellationToken
        );
    }

    /// <summary>
    /// 画像を非同期に読み込み、格子状に切り抜きます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="horizontalCount">横方向のセルの数。</param>
    /// <param name="verticalCount">縦方向のセルの数。</param>
    /// <param name="size">セルの大きさ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    public Task<Texture2D[]> LoadSpriteSheetAsync(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        TextureOptions options,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(
            CacheKind.SpriteGrid,
            normalized,
            (horizontalCount, verticalCount, size, options)
        );
        return CloneCellsAsync(
            GetOrLoadAsync(
                key,
                owner,
                () =>
                    ReadWithAsync(
                        normalized,
                        stream =>
                            _app.TextureFactory.LoadSpriteSheetAsync(
                                stream,
                                horizontalCount,
                                verticalCount,
                                size,
                                options
                            ),
                        leaveOpen: false
                    ),
                ReleaseTextureArray,
                cancellationToken
            )
        );
    }

    /// <summary>
    /// 画像を非同期に読み込み、任意の矩形で切り抜きます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/>
    /// <para>矩形の並びは、呼び出した時点でコピーします。</para>
    /// </remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="rects">切り抜く領域 (ピクセル単位、左上が原点)。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    public Task<Texture2D[]> LoadSpriteSheetAsync(
        string path,
        AssetScope scope,
        IReadOnlyList<RectInt> rects,
        CancellationToken cancellationToken = default
    )
    {
        return LoadSpriteSheetAsync(path, TextureOptions.Default, scope, rects, cancellationToken);
    }

    /// <summary>
    /// 画像を非同期に読み込み、任意の矩形で切り抜きます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadSpriteSheetAsync(string, AssetScope, IReadOnlyList{RectInt}, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="rects">切り抜く領域 (ピクセル単位、左上が原点)。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>切り抜いたテクスチャの配列。呼び出しごとに新しい配列を返します。要素を Dispose しないでください。</returns>
    public Task<Texture2D[]> LoadSpriteSheetAsync(
        string path,
        TextureOptions options,
        AssetScope scope,
        IReadOnlyList<RectInt> rects,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(rects);

        // 呼び出した時点の並びを使う。呼び出し元が後から配列を書き換えても影響しないようにする
        var snapshot = rects.ToArray();
        return LoadSpriteRectsAsync(path, options, scope, snapshot, cancellationToken);
    }

    /// <summary>
    /// 9 スライステクスチャを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="left">左端の幅。</param>
    /// <param name="top">上端の高さ。</param>
    /// <param name="right">右端の幅。</param>
    /// <param name="bottom">下端の高さ。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    public Task<Texture9Sliced> Load9SlicedAsync(
        string path,
        int left,
        int top,
        int right,
        int bottom,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        return Load9SlicedAsync(
            path,
            left,
            top,
            right,
            bottom,
            TextureOptions.Default,
            scope,
            cancellationToken
        );
    }

    /// <summary>
    /// 9 スライステクスチャを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="left">左端の幅。</param>
    /// <param name="top">上端の高さ。</param>
    /// <param name="right">右端の幅。</param>
    /// <param name="bottom">下端の高さ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだテクスチャ。Dispose しないでください。</returns>
    public Task<Texture9Sliced> Load9SlicedAsync(
        string path,
        int left,
        int top,
        int right,
        int bottom,
        TextureOptions options,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(
            CacheKind.NineSlice,
            normalized,
            (left, top, right, bottom, options)
        );
        return GetOrLoadAsync(
            key,
            owner,
            () =>
                ReadWithAsync(
                    normalized,
                    stream =>
                        _app.TextureFactory.Load9SlicedAsync(
                            stream,
                            left,
                            top,
                            right,
                            bottom,
                            options
                        ),
                    leaveOpen: false
                ),
            ReleaseDisposable,
            cancellationToken
        );
    }

    /// <summary>
    /// フォントを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="LoadFont(string, float, AssetScope)" path="/remarks"/>
    /// <inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/>
    /// </remarks>
    /// <param name="path">フォントファイルの論理パス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだフォント。</returns>
    public Task<Font> LoadFontAsync(
        string path,
        float size,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        return LoadFontAsync(path, size, FontStyle.Normal, true, scope, cancellationToken);
    }

    /// <summary>
    /// フォントを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadFontAsync(string, float, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">フォントファイルの論理パス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだフォント。</returns>
    public Task<Font> LoadFontAsync(
        string path,
        float size,
        FontStyle style,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        return LoadFontAsync(path, size, style, true, scope, cancellationToken);
    }

    /// <summary>
    /// フォントを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="LoadFontAsync(string, float, AssetScope, CancellationToken)" path="/remarks"/></remarks>
    /// <param name="path">フォントファイルの論理パス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだフォント。</returns>
    public Task<Font> LoadFontAsync(
        string path,
        float size,
        FontStyle style,
        bool isAntialiased,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        // 同期版と同じキーにして、グリフソースを共有する
        var key = new CacheKey(CacheKind.FontSource, normalized, null);
        var glyphs = GetOrLoadAsync(
            key,
            owner,
            () =>
                Task.FromResult(
                    ReadWith(
                        normalized,
                        stream => CreateGlyphSource(normalized, stream),
                        leaveOpen: false
                    )
                ),
            ReleaseGlyphSource,
            cancellationToken
        );
        return CreateFontAsync(glyphs, size, style, isAntialiased);
    }

    /// <summary>
    /// 音声を非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="LoadAudio(string, AssetScope)" path="/remarks"/>
    /// <inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/>
    /// </remarks>
    /// <param name="path">音声ファイルの論理パス。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだ音声。Dispose しないでください (解放は Unload かスコープに任せます)。</returns>
    public Task<IAudioSource> LoadAudioAsync(
        string path,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(CacheKind.Audio, normalized, null);
        return GetOrLoadAsync(
            key,
            owner,
            () =>
                Task.FromResult(
                    ReadWith(normalized, stream => DecodeAudio(normalized, stream), leaveOpen: true)
                ),
            ReleaseDisposable,
            cancellationToken
        );
    }

    /// <summary>
    /// 任意の型のアセットを、指定した処理で非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="Load{T}(string, string, Func{Stream, T}, AssetScope)" path="/remarks"/>
    /// <inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/>
    /// <para>ストリームは、<paramref name="factory"/> が返した <see cref="Task"/> が完了した後に閉じます。</para>
    /// </remarks>
    /// <typeparam name="T">アセットの型。</typeparam>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="key">読み方を区別するキー。</param>
    /// <param name="factory">ストリームからアセットを作る処理。スレッドプールで呼ばれます。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだアセット。Dispose しないでください (解放は Unload かスコープに任せます)。</returns>
    public Task<T> LoadAsync<T>(
        string path,
        string key,
        Func<Stream, Task<T>> factory,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        return LoadAsync(path, key, factory, scope, leaveOpen: false, cancellationToken);
    }

    /// <summary>
    /// 任意の型のアセットを、指定した処理で非同期に読み込みます。ストリームの所有権を <paramref name="factory"/> に渡せます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="Load{T}(string, string, Func{Stream, T}, AssetScope, bool)" path="/remarks"/>
    /// <inheritdoc cref="LoadTextureAsync(string, AssetScope, CancellationToken)" path="/remarks"/>
    /// </remarks>
    /// <typeparam name="T">アセットの型。</typeparam>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="key">読み方を区別するキー。</param>
    /// <param name="factory">ストリームからアセットを作る処理。スレッドプールで呼ばれます。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="leaveOpen"><see langword="true"/> なら、<paramref name="factory"/> が完了した後もストリームを閉じません。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだアセット。Dispose しないでください (解放は Unload かスコープに任せます)。</returns>
    public Task<T> LoadAsync<T>(
        string path,
        string key,
        Func<Stream, Task<T>> factory,
        AssetScope scope,
        bool leaveOpen,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        // Load と同じく型を含めるので、型付きの読み込みとはキャッシュを共有しない
        var cacheKey = new CacheKey(CacheKind.Custom, normalized, (key, typeof(T)));
        return GetOrLoadAsync(
            cacheKey,
            owner,
            () => ReadWithAsync(normalized, factory, leaveOpen),
            ReleaseDisposable,
            cancellationToken
        );
    }

    /// <summary>
    /// 指定したパスのアセットから、指定したスコープを所有者から外します。所有者がいなくなったアセットは解放します。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// 同じパスから読んだすべてのアセット (種類やオプション、汎用の読み込みのキーが違うものを含む) が対象です。
    /// 読み込まれていないパスを指定しても、何もしません。
    /// </remarks>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="scope">外すスコープ。<see cref="AssetScope.Scene"/> は現在のシーンを指します。</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    /// <exception cref="AggregateException">解放の途中で例外が出た場合。残りのアセットは解放したうえで、例外をまとめて投げます。</exception>
    public void Unload(string path, AssetScope scope)
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        List<Entry> released = [];
        lock (_lock)
        {
            DetachOwners(key => key.Path == normalized, owner, released);
        }

        ReleaseEntries(released);
    }

    /// <summary>
    /// 指定したパスのアセットを、所有するスコープに関わらず解放します。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="Unload(string, AssetScope)" path="/remarks"/></remarks>
    /// <param name="path">ファイルの論理パス。</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    /// <exception cref="AggregateException">解放の途中で例外が出た場合。残りのアセットは解放したうえで、例外をまとめて投げます。</exception>
    public void Unload(string path)
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();

        List<Entry> released = [];
        lock (_lock)
        {
            DetachOwners(key => key.Path == normalized, owner: null, released);
        }

        ReleaseEntries(released);
    }

    /// <summary>
    /// 指定したスコープを、すべてのアセットの所有者から外します。所有者がいなくなったアセットは解放します。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// スコープ自体は残るので、この後も同じスコープで読み込めます。
    /// </remarks>
    /// <param name="scope">外すスコープ。<see cref="AssetScope.Scene"/> は現在のシーンを指します。</param>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    /// <exception cref="AggregateException">解放の途中で例外が出た場合。残りのアセットは解放したうえで、例外をまとめて投げます。</exception>
    public void Unload(AssetScope scope)
    {
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        List<Entry> released = [];
        lock (_lock)
        {
            DetachOwners(_ => true, owner, released);
        }

        ReleaseEntries(released);
    }

    /// <summary>
    /// 読み込んだすべてのアセットを解放し、<see cref="FileSystem"/> を破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
    /// <remarks>
    /// アプリの終了時に呼ばれます。アプリの Dispose と DI コンテナの破棄の両方から呼ばれるため、冪等です。
    /// 解放の途中で例外が出ても、残りのアセットと <see cref="FileSystem"/> は解放します。
    /// </remarks>
    /// <exception cref="AggregateException">解放の途中で例外が出た場合。すべての解放を終えてから、例外をまとめて投げます。</exception>
    public void Dispose()
    {
        // アプリの Dispose と DI コンテナの破棄の両方から呼ばれるため、1 回だけ実行する
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // 破棄済みになったので、ここからの解放はスレッドに関係なくその場で行われる
        List<Entry> entries = [];
        lock (_lock)
        {
            entries.AddRange(_cache.Values.Where(entry => entry.HasValue));
            _cache.Clear();
        }

        List<Exception> errors = [];
        try
        {
            try
            {
                ReleaseEntries(entries);
            }
            catch (AggregateException e)
            {
                errors.AddRange(e.InnerExceptions);
            }

            try
            {
                FlushPendingReleases();
            }
            catch (AggregateException e)
            {
                errors.AddRange(e.InnerExceptions);
            }
        }
        finally
        {
            TryRelease(() => FileSystem.Dispose(), errors);
        }

        ThrowIfAny(errors);
    }

    /// <summary>
    /// 解放の処理を 1 件実行します。例外は <paramref name="errors"/> に集めて、呼び出し元を止めません。
    /// </summary>
    /// <param name="release">解放の処理。</param>
    /// <param name="errors">例外を集めるリスト。</param>
    private static void TryRelease(Action release, List<Exception> errors)
    {
        try
        {
            release();
        }
        catch (Exception e)
        {
            errors.Add(e);
        }
    }

    /// <summary>
    /// 集めた例外が 1 つでもあれば、<see cref="AggregateException"/> にまとめて投げます。
    /// </summary>
    /// <param name="errors">集めた例外。</param>
    /// <exception cref="AggregateException"><paramref name="errors"/> に例外がある場合。</exception>
    private static void ThrowIfAny(List<Exception> errors)
    {
        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
    }

    /// <summary>
    /// <see cref="IDisposable"/> を実装している値を Dispose します。
    /// </summary>
    /// <param name="value">解放する値。</param>
    private static void ReleaseDisposable(object? value)
    {
        (value as IDisposable)?.Dispose();
    }

    /// <summary>
    /// テクスチャの配列の要素をすべて Dispose します。
    /// </summary>
    /// <param name="value">解放する <see cref="Texture2D"/> の配列。</param>
    private static void ReleaseTextureArray(object? value)
    {
        foreach (var texture in (Texture2D[])value!)
        {
            texture.Dispose();
        }
    }

    /// <summary>
    /// グリフソースの値を解放します。自分で作ったものだけを Dispose します。
    /// </summary>
    /// <param name="value">解放する <see cref="GlyphSourceValue"/>。</param>
    private static void ReleaseGlyphSource(object? value)
    {
        var loaded = (GlyphSourceValue)value!;
        if (loaded.IsOwned)
        {
            loaded.Source.Dispose();
        }
    }

    /// <summary>
    /// 読み込みの完了を待ち、キャッシュの配列をコピーして返します。
    /// </summary>
    /// <param name="cells">読み込んだテクスチャの配列を返す Task。</param>
    /// <returns>コピーしたテクスチャの配列。</returns>
    private static async Task<Texture2D[]> CloneCellsAsync(Task<Texture2D[]> cells)
    {
        var loaded = await cells.ConfigureAwait(false);
        return (Texture2D[])loaded.Clone();
    }

    /// <summary>
    /// グリフソースの読み込みの完了を待ち、サイズとスタイルを付けたフォントを返します。
    /// </summary>
    /// <param name="glyphs">グリフソースを返す Task。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>読み込んだフォント。</returns>
    private static async Task<Font> CreateFontAsync(
        Task<GlyphSourceValue> glyphs,
        float size,
        FontStyle style,
        bool isAntialiased
    )
    {
        var loaded = await glyphs.ConfigureAwait(false);
        return Font.FromGlyphSource(loaded.Source, size, style, isAntialiased);
    }

    /// <summary>
    /// 何もマウントされなかったときに、<see cref="AssetFileSystem.OpenRead"/> の例外に添える原因と対処を作ります。
    /// </summary>
    /// <param name="assembly">属性を探したアセンブリ。無い場合は <see langword="null"/>。</param>
    /// <returns>説明の文字列。</returns>
    private static string CreateEmptyHint(Assembly? assembly)
    {
        var name = assembly is null ? "(なし)" : $"'{assembly.GetName().Name}'";

        return $"アセンブリ {name} に Assets の属性が無く、明示的なマウントもありません。"
            + "ビルド時に csproj で PrometeAsset を宣言すると、属性が書き込まれて自動でマウントされます。"
            + "別のアセンブリのアセットを読むなら UseAssets(typeof(<そのアセンブリの型>).Assembly) を使ってください。"
            + "Promete.Web 2.4 より前と組み合わせる場合は、"
            + "UseAssets(typeof(Program).Assembly, o => o.MountFolder(\"assets\", \"assets\")) を使ってください。";
    }

    /// <summary>
    /// スコープを、所有者を表すオブジェクトに変換します。
    /// </summary>
    /// <param name="scope">変換するスコープ。</param>
    /// <returns>所有者を表すオブジェクト。</returns>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException">未知の <see cref="AssetScope"/> の値の場合。</exception>
    private object ResolveOwner(AssetScope scope)
    {
        return scope switch
        {
            AssetScope.Global => _globalOwner,
            AssetScope.Scene => ResolveSceneOwner(),
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
        };
    }

    /// <summary>
    /// <see cref="AssetScope.Scene"/> の所有者を求めます。生成中のシーンがあればそれを、無ければ現在のシーンを指します。
    /// </summary>
    /// <remarks>
    /// 生成中のシーンを優先するのは、コンストラクタ (フィールド初期化子や DI の解決を含む) で読んだものを、生成中のシーンに帰属させるためです。
    /// 遷移の途中では現在のシーンが古いことがあるため、生成中のものが無いときだけ現在のシーンを見ます。
    /// </remarks>
    /// <returns>所有者を表すオブジェクト。</returns>
    /// <exception cref="InvalidOperationException">メインスレッド以外から呼んだ場合、またはシーンが無い (スコープが解放済み) 場合。</exception>
    private object ResolveSceneOwner()
    {
        if (!_app.IsMainThread())
        {
            throw new InvalidOperationException(
                "AssetScope.Scene はメインスレッドからしか指定できません。"
            );
        }

        object? owner = null;
        lock (_lock)
        {
            if (_constructingScopes.TryPeek(out var constructing))
            {
                owner = constructing;
            }
            else if (
                _app.CurrentScene is { } current
                && _sceneScopes.TryGetValue(current, out var scope)
            )
            {
                owner = scope;
            }
        }

        return owner
            ?? throw new InvalidOperationException(
                "シーンが無いときに AssetScope.Scene は指定できません。シーンに関係しないアセットには AssetScope.Global を使ってください。"
            );
    }

    /// <summary>
    /// 破棄されていれば <see cref="ObjectDisposedException"/> をスローします。
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    /// <summary>
    /// キャッシュから値を取り出します。無ければ <paramref name="load"/> で読み込み、キャッシュに入れます。所有者は <paramref name="owner"/> に加わります。
    /// </summary>
    /// <remarks>
    /// 読み込みはロックの外で行います。同時に読み込んだ結果が複数あれば、先に入った値を使い、残りは解放します。
    /// </remarks>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="key">キャッシュのキー。</param>
    /// <param name="owner">所有者。</param>
    /// <param name="load">値を読み込む処理。</param>
    /// <param name="release">値を解放する処理。</param>
    /// <returns>キャッシュされた値。</returns>
    private T GetOrLoad<T>(CacheKey key, object owner, Func<T> load, Action<object?> release)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existing) && existing.HasValue)
            {
                existing.Owners.Add(owner);
                return (T)existing.Value!;
            }
        }

        var value = load();

        var disposed = false;
        var discard = false;
        var winner = value;
        lock (_lock)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                disposed = true;
            }
            else
            {
                if (!_cache.TryGetValue(key, out var entry))
                {
                    entry = new Entry(release);
                    _cache.Add(key, entry);
                }

                entry.Owners.Add(owner);
                if (entry.HasValue)
                {
                    // 別のスレッドが先に入れた値を使う
                    winner = (T)entry.Value!;
                    discard = true;
                }
                else
                {
                    entry.Value = value;
                    entry.HasValue = true;
                }
            }
        }

        if (disposed)
        {
            ReleaseValue(release, value);
            throw new ObjectDisposedException(nameof(AssetManager));
        }

        if (discard)
        {
            ReleaseValue(release, value);
        }

        return winner;
    }

    /// <summary>
    /// 非同期の読み込みを、キャッシュを通して行います。同じキーの値があればそれを、実行中の読み込みがあればそれを共有します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 所有者は呼び出した時点で加わります。新しく読む場合は、読み込み全体 (ストリームを開く処理を含む) をスレッドプールで行います。
    /// </para>
    /// <para>
    /// <paramref name="cancellationToken"/> は待つのをやめるためだけに使います。共有している読み込みは止めません。
    /// </para>
    /// </remarks>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="key">キャッシュのキー。</param>
    /// <param name="owner">所有者。</param>
    /// <param name="load">値を非同期に読み込む処理。スレッドプールで呼ばれます。</param>
    /// <param name="release">値を解放する処理。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだ値。</returns>
    private Task<T> GetOrLoadAsync<T>(
        CacheKey key,
        object owner,
        Func<Task<T>> load,
        Action<object?> release,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        Task<object?> pending;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

            if (_cache.TryGetValue(key, out var existing))
            {
                existing.Owners.Add(owner);
                if (existing.HasValue)
                {
                    return Task.FromResult((T)existing.Value!);
                }

                // 値の無いエントリは実行中の読み込み。完了を待つ
                pending = existing.Pending!;
            }
            else
            {
                var entry = new Entry(release);
                entry.Owners.Add(owner);
                _cache.Add(key, entry);
                var task = Task.Run(() => CompleteAsyncLoad(key, entry, load, release));
                entry.Pending = task;
                pending = task;
            }
        }

        return AwaitSharedAsync<T>(pending, cancellationToken);
    }

    /// <summary>
    /// 読み込みを行い、結果をキャッシュに入れて、待っている全員に返す値を決めます。読み込みの Task の中で実行します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 成功時、既に値が入っていれば (同期の読み込みが先に入れた) 自分の結果を解放して、その値を返します。
    /// 所有者がいなくなった場合や破棄された後に完了した場合は、キャッシュに入れずに解放し、待っている全員を例外で失敗させます (破棄が原因なら <see cref="ObjectDisposedException"/>、それ以外は <see cref="OperationCanceledException"/>)。
    /// </para>
    /// <para>
    /// 失敗時、値が入っていればその値を返し、無ければキャッシュから外して例外を投げ直します。
    /// </para>
    /// </remarks>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="key">キャッシュのキー。</param>
    /// <param name="entry">この読み込みのエントリ。</param>
    /// <param name="load">値を非同期に読み込む処理。</param>
    /// <param name="release">値を解放する処理。</param>
    /// <returns>待っている全員に返す値。</returns>
    private async Task<object?> CompleteAsyncLoad<T>(
        CacheKey key,
        Entry entry,
        Func<Task<T>> load,
        Action<object?> release
    )
    {
        T value;
        try
        {
            value = await load().ConfigureAwait(false);
        }
        catch
        {
            object? existingValue = null;
            var hasExisting = false;
            lock (_lock)
            {
                entry.Pending = null;
                if (IsCurrent(key, entry) && entry.HasValue)
                {
                    hasExisting = true;
                    existingValue = entry.Value;
                }
                else
                {
                    RemoveIfCurrent(key, entry);
                }
            }

            if (hasExisting)
            {
                return existingValue;
            }

            throw;
        }

        object? winner = value;
        var shouldRelease = false;
        Exception? orphanFailure = null;
        lock (_lock)
        {
            entry.Pending = null;
            if (IsCurrent(key, entry) && entry.HasValue)
            {
                // 同期の読み込みが先に値を入れていたので、そちらを使う
                winner = entry.Value;
                shouldRelease = true;
            }
            else if (
                IsCurrent(key, entry)
                && entry.Owners.Count > 0
                && Volatile.Read(ref _disposed) == 0
            )
            {
                entry.Value = value;
                entry.HasValue = true;
            }
            else
            {
                // 所有者がいない、または破棄された後の完了。キャッシュに入れずに解放し、待っている全員を失敗させる
                RemoveIfCurrent(key, entry);
                shouldRelease = true;
                orphanFailure =
                    Volatile.Read(ref _disposed) != 0
                        ? new ObjectDisposedException(nameof(AssetManager))
                        : new OperationCanceledException(
                            "読み込みの完了前に、所有するスコープがすべて解放されたため、結果を返せません。"
                        );
            }
        }

        if (shouldRelease)
        {
            ReleaseValue(release, value);
        }

        if (orphanFailure is not null)
        {
            throw orphanFailure;
        }

        return winner;
    }

    /// <summary>
    /// 実行中の読み込みの完了を待ち、結果を返します。待つのをやめるのは <paramref name="cancellationToken"/> のときだけで、読み込み自体は止めません。
    /// </summary>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="pending">実行中の読み込み。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>読み込んだ値。</returns>
    private async Task<T> AwaitSharedAsync<T>(
        Task<object?> pending,
        CancellationToken cancellationToken
    )
    {
        var value = await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
        return (T)value!;
    }

    /// <summary>
    /// エントリがキャッシュに今も入っているかどうかを表します。ロックの中で呼びます。
    /// </summary>
    /// <param name="key">キャッシュのキー。</param>
    /// <param name="entry">調べるエントリ。</param>
    /// <returns>同じキーのエントリとして今も入っていれば <see langword="true"/>。</returns>
    private bool IsCurrent(CacheKey key, Entry entry)
    {
        return _cache.TryGetValue(key, out var current) && ReferenceEquals(current, entry);
    }

    /// <summary>
    /// エントリが今もキャッシュに入っていれば外します。ロックの中で呼びます。
    /// </summary>
    /// <param name="key">キャッシュのキー。</param>
    /// <param name="entry">外すエントリ。</param>
    private void RemoveIfCurrent(CacheKey key, Entry entry)
    {
        if (IsCurrent(key, entry))
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// 条件に合うエントリから所有者を外します。所有者がいなくなった値のあるエントリはキャッシュから外し、<paramref name="released"/> に集めます。
    /// </summary>
    /// <remarks>
    /// 値の無いエントリ (実行中の非同期) はキャッシュに残します。完了時の処理が、所有者が空なら解放します。ロックの中で呼びます。
    /// </remarks>
    /// <param name="match">対象のキーを選ぶ条件。</param>
    /// <param name="owner">外す所有者。<see langword="null"/> なら、すべての所有者を外します。</param>
    /// <param name="released">キャッシュから外したエントリを集めるリスト。</param>
    private void DetachOwners(Func<CacheKey, bool> match, object? owner, List<Entry> released)
    {
        foreach (var key in _cache.Keys.Where(match).ToList())
        {
            var entry = _cache[key];
            if (owner is null)
            {
                entry.Owners.Clear();
            }
            else
            {
                entry.Owners.Remove(owner);
            }

            if (entry.HasValue && entry.Owners.Count == 0)
            {
                _cache.Remove(key);
                released.Add(entry);
            }
        }
    }

    /// <summary>
    /// キャッシュから外したエントリの値を解放します。ロックの外で呼びます。
    /// </summary>
    /// <param name="entries">解放するエントリ。</param>
    private void ReleaseEntries(List<Entry> entries)
    {
        List<Exception> errors = [];
        foreach (var entry in entries)
        {
            TryRelease(() => ReleaseValue(entry.Release, entry.Value), errors);
        }

        ThrowIfAny(errors);
    }

    /// <summary>
    /// ストリームを開き、<paramref name="factory"/> で読み込みます。ストリームは、例外のときと <paramref name="leaveOpen"/> が <see langword="false"/> のときに閉じます。
    /// </summary>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="path">正規化した論理パス。</param>
    /// <param name="factory">ストリームから値を作る処理。</param>
    /// <param name="leaveOpen">正常に戻った後もストリームを閉じないなら <see langword="true"/>。</param>
    /// <returns>読み込んだ値。</returns>
    private T ReadWith<T>(string path, Func<Stream, T> factory, bool leaveOpen)
    {
        var stream = FileSystem.OpenRead(path);
        T value;
        try
        {
            value = factory(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        if (!leaveOpen)
        {
            stream.Dispose();
        }

        return value;
    }

    /// <summary>
    /// ストリームを非同期に読み込みます。ストリームは、<paramref name="factory"/> の Task が完了した後に閉じます。
    /// </summary>
    /// <remarks>
    /// 呼び出し元 (<see cref="GetOrLoadAsync{T}"/>) がスレッドプールで呼ぶので、ストリームを開く処理もスレッドプールで行われます。
    /// </remarks>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="path">正規化した論理パス。</param>
    /// <param name="factory">ストリームから値を作る非同期の処理。</param>
    /// <param name="leaveOpen">正常に完了した後もストリームを閉じないなら <see langword="true"/>。</param>
    /// <returns>読み込んだ値。</returns>
    private async Task<T> ReadWithAsync<T>(
        string path,
        Func<Stream, Task<T>> factory,
        bool leaveOpen
    )
    {
        var stream = FileSystem.OpenRead(path);
        T value;
        try
        {
            value = await factory(stream).ConfigureAwait(false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        if (!leaveOpen)
        {
            stream.Dispose();
        }

        return value;
    }

    /// <summary>
    /// 値を解放します。メインスレッドならその場で、それ以外では次のフレームにメインスレッドで行います。破棄された後は、スレッドに関係なくその場で行います。
    /// </summary>
    /// <param name="release">値を解放する処理。</param>
    /// <param name="value">解放する値。</param>
    private void ReleaseValue(Action<object?> release, object? value)
    {
        if (Volatile.Read(ref _disposed) != 0 || _app.IsMainThread())
        {
            release(value);
            return;
        }

        _pendingReleases.Enqueue(() => release(value));

        // 予約は 1 フレームに 1 回だけ入れる
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
        {
            _app.NextFrame(FlushPendingReleases);
        }
    }

    /// <summary>
    /// 予約された解放を、メインスレッドで順に実行します。
    /// </summary>
    private void FlushPendingReleases()
    {
        Interlocked.Exchange(ref _flushScheduled, 0);
        List<Exception> errors = [];
        while (_pendingReleases.TryDequeue(out var release))
        {
            TryRelease(release, errors);
        }

        ThrowIfAny(errors);
    }

    /// <summary>
    /// 先頭のバイト列で、登録されたデコーダーを順に判定し、最初に判定が通ったものでデコードします。
    /// </summary>
    /// <remarks>
    /// デコーダーに渡したストリームは、閉じる責任がデコーダーに移るため、ここでは閉じません。
    /// 判定やデコードが例外になった場合と、どのデコーダーも判定が通らなかった場合は、呼び出し元の <see cref="ReadWith{T}"/> がストリームを閉じます。
    /// </remarks>
    /// <param name="normalized">正規化した論理パス。</param>
    /// <param name="stream">先頭に位置したストリーム。</param>
    /// <returns>デコードした音声。</returns>
    /// <exception cref="NotSupportedException">どのデコーダーも判定が通らなかった場合。</exception>
    private IAudioSource DecodeAudio(string normalized, Stream stream)
    {
        var header = new byte[AudioHeaderLength];
        var length = 0;
        while (length < header.Length)
        {
            var read = stream.Read(header, length, header.Length - length);
            if (read == 0)
            {
                break;
            }

            length += read;
        }

        stream.Seek(0, SeekOrigin.Begin);

        var headerSpan = header.AsSpan(0, length);
        foreach (var decoder in _audioDecoders)
        {
            if (decoder.CanDecode(normalized, headerSpan))
            {
                return decoder.Decode(stream);
            }
        }

        throw new NotSupportedException(
            $"この形式の音声をデコードできるデコーダーがありません: '{normalized}'"
        );
    }

    /// <summary>
    /// フォントのグリフソースを、キャッシュを通して読み込みます。パスごとに 1 つだけ読み、サイズやスタイルが違っても共有します。
    /// </summary>
    /// <param name="normalized">正規化した論理パス。</param>
    /// <param name="owner">所有者。</param>
    /// <returns>グリフソース。</returns>
    private IGlyphSource GetOrLoadGlyphSource(string normalized, object owner)
    {
        var key = new CacheKey(CacheKind.FontSource, normalized, null);
        var loaded = GetOrLoad(
            key,
            owner,
            () =>
                ReadWith(
                    normalized,
                    stream => CreateGlyphSource(normalized, stream),
                    leaveOpen: false
                ),
            ReleaseGlyphSource
        );
        return loaded.Source;
    }

    /// <summary>
    /// ストリームからグリフソースを作ります。ストリームから読めない環境では、ファイルのパスから読みます。
    /// </summary>
    /// <remarks>
    /// ファイルのパスから読んだグリフソースは provider がキャッシュしているため、解放の対象にしません。
    /// サイズは、グリフソースだけを作るために使うので、既定の値を渡します。
    /// </remarks>
    /// <param name="normalized">正規化した論理パス。</param>
    /// <param name="stream">フォントのデータを含むストリーム。</param>
    /// <returns>グリフソースと、解放の対象かどうか。</returns>
    /// <exception cref="NotSupportedException">ストリームからもファイルのパスからも読めない場合。</exception>
    private GlyphSourceValue CreateGlyphSource(string normalized, Stream stream)
    {
        try
        {
            var font = _app.FontProvider.FromStream(
                stream,
                GlyphSourceProbeSize,
                FontStyle.Normal,
                true
            );
            return new GlyphSourceValue(font.Source, IsOwned: true);
        }
        catch (NotSupportedException ex)
        {
            if (!FileSystem.TryGetFilePath(normalized, out var fullPath))
            {
                throw new NotSupportedException(
                    $"フォント '{normalized}' はストリームからもファイルのパスからも読めません。",
                    ex
                );
            }

            var font = _app.FontProvider.FromFile(
                fullPath,
                GlyphSourceProbeSize,
                FontStyle.Normal,
                true,
                0
            );
            return new GlyphSourceValue(font.Source, IsOwned: false);
        }
    }

    /// <summary>
    /// 矩形の並びで切り抜いたテクスチャを、キャッシュを通して読み込みます。呼び出し元が渡した配列のコピーを使います。
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="rects">切り抜く領域のコピー。キーと読み込みの両方に使います。</param>
    /// <returns>切り抜いたテクスチャの配列 (呼び出しごとに新しい配列)。</returns>
    private Texture2D[] LoadSpriteRects(
        string path,
        TextureOptions options,
        AssetScope scope,
        RectInt[] rects
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(CacheKind.SpriteRects, normalized, new RectsKey(rects, options));
        var cells = GetOrLoad(
            key,
            owner,
            () =>
                ReadWith(
                    normalized,
                    stream => _app.TextureFactory.LoadSpriteSheet(stream, options, rects),
                    leaveOpen: false
                ),
            ReleaseTextureArray
        );
        return (Texture2D[])cells.Clone();
    }

    /// <summary>
    /// 矩形の並びで切り抜いたテクスチャを、キャッシュを通して非同期に読み込みます。呼び出し元が渡した配列のコピーを使います。
    /// </summary>
    /// <param name="path">画像ファイルの論理パス。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <param name="scope">アセットを所有させるスコープ。</param>
    /// <param name="rects">切り抜く領域のコピー。キーと読み込みの両方に使います。</param>
    /// <param name="cancellationToken">待つのをやめるためのトークン。</param>
    /// <returns>切り抜いたテクスチャの配列 (呼び出しごとに新しい配列)。</returns>
    private Task<Texture2D[]> LoadSpriteRectsAsync(
        string path,
        TextureOptions options,
        AssetScope scope,
        RectInt[] rects,
        CancellationToken cancellationToken
    )
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();
        var owner = ResolveOwner(scope);

        var key = new CacheKey(CacheKind.SpriteRects, normalized, new RectsKey(rects, options));
        return CloneCellsAsync(
            GetOrLoadAsync(
                key,
                owner,
                () =>
                    ReadWithAsync(
                        normalized,
                        stream =>
                            _app.TextureFactory.LoadSpriteSheetAsync(
                                stream,
                                options,
                                CancellationToken.None,
                                rects
                            ),
                        leaveOpen: false
                    ),
                ReleaseTextureArray,
                cancellationToken
            )
        );
    }

    /// <summary>
    /// キャッシュのキーです。(種類, 正規化した論理パス, オプション) の組で同一性を決めます。
    /// </summary>
    private readonly record struct CacheKey(CacheKind Kind, string Path, object? Options);

    /// <summary>
    /// フォントのグリフソースの値です。自分で作ったものかどうかを持ちます。
    /// </summary>
    /// <param name="Source">グリフソース。</param>
    /// <param name="IsOwned">解放の対象なら <see langword="true"/>。provider がキャッシュしているものは <see langword="false"/>。</param>
    private sealed record GlyphSourceValue(IGlyphSource Source, bool IsOwned);

    /// <summary>
    /// シーンのスコープです。中身は持たず、参照の同一性だけで所有者を表します。
    /// </summary>
    private sealed class SceneScope { }

    /// <summary>
    /// シーンの生成と破棄を受け取り、シーンごとの <see cref="SceneScope"/> を作って解放するフックです。
    /// </summary>
    private sealed class SceneScopeHook(AssetManager manager) : ISceneLifecycleHook
    {
        /// <summary>
        /// 生成を始めるシーンのスコープを、生成中のスタックに積みます。
        /// </summary>
        public void OnSceneConstructing()
        {
            lock (manager._lock)
            {
                manager._constructingScopes.Push(new SceneScope());
            }
        }

        /// <summary>
        /// 生成が終わったシーンに、生成中のスコープを結び付けます。
        /// </summary>
        /// <param name="scene">生成されたシーン。</param>
        public void OnSceneConstructed(Scene scene)
        {
            lock (manager._lock)
            {
                if (manager._constructingScopes.TryPop(out var scope))
                {
                    manager._sceneScopes[scene] = scope;
                }
            }
        }

        /// <summary>
        /// 生成に失敗したシーンのスコープを、その場で解放します。
        /// </summary>
        public void OnSceneConstructionFailed()
        {
            List<Entry> released = [];
            lock (manager._lock)
            {
                if (manager._constructingScopes.TryPop(out var scope))
                {
                    manager.DetachOwners(_ => true, scope, released);
                }
            }

            manager.ReleaseEntries(released);
        }

        /// <summary>
        /// 使われなくなったシーンのスコープを外し、そのスコープだけが所有していたアセットを解放します。
        /// </summary>
        /// <param name="scene">使われなくなったシーン。</param>
        public void OnSceneReleased(Scene scene)
        {
            List<Entry> released = [];
            lock (manager._lock)
            {
                if (manager._sceneScopes.Remove(scene, out var scope))
                {
                    manager.DetachOwners(_ => true, scope, released);
                }
            }

            manager.ReleaseEntries(released);
        }
    }

    /// <summary>
    /// 矩形の並びとオプションのキーです。矩形は順序と各要素の値 (左上と大きさ) で比べます。
    /// </summary>
    private sealed class RectsKey(RectInt[] rects, TextureOptions options) : IEquatable<RectsKey>
    {
        private readonly RectInt[] _rects = rects;
        private readonly TextureOptions _options = options;

        /// <summary>
        /// 別のキーと等しいかどうかを判定します。
        /// </summary>
        /// <param name="other">比較する相手。</param>
        /// <returns>矩形の並びとオプションが等しければ <see langword="true"/>。</returns>
        public bool Equals(RectsKey? other)
        {
            if (
                other is null
                || _rects.Length != other._rects.Length
                || !_options.Equals(other._options)
            )
            {
                return false;
            }

            for (var i = 0; i < _rects.Length; i++)
            {
                var a = _rects[i];
                var b = other._rects[i];
                if (
                    a.Left != b.Left
                    || a.Top != b.Top
                    || a.Width != b.Width
                    || a.Height != b.Height
                )
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is RectsKey other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = default(HashCode);
            hash.Add(_options);
            foreach (var rect in _rects)
            {
                hash.Add(rect.Left);
                hash.Add(rect.Top);
                hash.Add(rect.Width);
                hash.Add(rect.Height);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// キャッシュの 1 件です。値と、それを所有するスコープの集合を持ちます。
    /// </summary>
    private sealed class Entry(Action<object?> release)
    {
        /// <summary>
        /// 値を所有しているスコープの集合です。参照の同一性で比べます。
        /// </summary>
        public HashSet<object> Owners { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// 値が入っているかどうかを表します。
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>
        /// 読み込んだ値です。<see cref="HasValue"/> が <see langword="true"/> のときだけ有効です。
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 実行中の非同期の読み込みです。完了の処理が終わるまで設定されます。結果は値として <see cref="object"/> で返します。
        /// </summary>
        public Task<object?>? Pending { get; set; }

        /// <summary>
        /// 値を解放する処理です。
        /// </summary>
        public Action<object?> Release { get; } = release;
    }
}
