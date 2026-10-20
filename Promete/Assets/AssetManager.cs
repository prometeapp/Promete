using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
/// </remarks>
public sealed class AssetManager : IDisposable
{
    private int _disposed;

    /// <summary>
    /// <see cref="AssetManager"/> の新しいインスタンスを初期化し、設定に従ってソースをマウントします。
    /// </summary>
    /// <param name="assembly">Assets の属性を探すアセンブリ。無い場合は <see langword="null"/>。</param>
    /// <param name="options">明示的なマウントの設定。</param>
    /// <exception cref="DirectoryNotFoundException">マウントするフォルダが存在しない場合。</exception>
    internal AssetManager(Assembly? assembly, AssetsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // assembly は Phase 4 で属性を読むために使う。Phase 1 では属性がまだ無いので参照しない
        FileSystem = new AssetFileSystem();
        try
        {
            foreach (var (factory, priority) in options.Mounts)
            {
                FileSystem.Mount(factory(), priority);
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
    /// <returns>読み込んだ音声。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="NotSupportedException">デコードできるデコーダーが無い場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public IAudioSource LoadAudio(string path, AssetScope scope)
    {
        throw new NotImplementedException();
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
    /// <returns>読み込んだアセット。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException"><see cref="AssetScope.Scene"/> を、メインスレッド以外から、またはシーンが無いときに指定した場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public T Load<T>(string path, string key, Func<Stream, T> factory, AssetScope scope)
    {
        throw new NotImplementedException();
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
    /// <returns>読み込んだアセット。</returns>
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// テクスチャを非同期に読み込みます。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// <para>
    /// 同じキーの読み込みが実行中なら、それを共有します。スコープは呼び出した時点で決まり、その時点で所有者に加わります。
    /// 完了する前にスコープが解放されて所有者がいなくなった場合、結果は完了した時点で解放されます (返された値は使えません)。
    /// </para>
    /// <para>
    /// <paramref name="cancellationToken"/> は、呼び出し元が待つのをやめるためのものです。共有している読み込み自体は止まらず、結果はスコープに残ります。
    /// </para>
    /// <para>
    /// 引数とスコープの誤り (<see cref="ArgumentException"/>、<see cref="InvalidOperationException"/>、<see cref="ObjectDisposedException"/>) は、
    /// 返す <see cref="Task"/> ではなく、この呼び出しで例外になります。
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
    /// <returns>読み込んだ音声。</returns>
    public Task<IAudioSource> LoadAudioAsync(
        string path,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
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
    /// <returns>読み込んだアセット。</returns>
    public Task<T> LoadAsync<T>(
        string path,
        string key,
        Func<Stream, Task<T>> factory,
        AssetScope scope,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
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
    /// <returns>読み込んだアセット。</returns>
    public Task<T> LoadAsync<T>(
        string path,
        string key,
        Func<Stream, Task<T>> factory,
        AssetScope scope,
        bool leaveOpen,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
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
    public void Unload(string path, AssetScope scope)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 指定したパスのアセットを、所有するスコープに関わらず解放します。 (v2.4~)
    /// </summary>
    /// <remarks><inheritdoc cref="Unload(string, AssetScope)" path="/remarks"/></remarks>
    /// <param name="path">ファイルの論理パス。</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public void Unload(string path)
    {
        throw new NotImplementedException();
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
    public void Unload(AssetScope scope)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 読み込んだすべてのアセットを解放し、<see cref="FileSystem"/> を破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
    /// <remarks>
    /// アプリの終了時に呼ばれます。アプリの Dispose と DI コンテナの破棄の両方から呼ばれるため、冪等です。
    /// </remarks>
    public void Dispose()
    {
        // アプリの Dispose と DI コンテナの破棄の両方から呼ばれるため、1 回だけ実行する
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        FileSystem.Dispose();
    }
}
