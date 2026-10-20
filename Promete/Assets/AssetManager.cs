using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Promete.Assets;

/// <summary>
/// アセットの読み込みを管理するプラグインです。<see cref="AssetsAppBuilderExtension"/> の <c>UseAssets</c> で登録します。
/// </summary>
/// <remarks>
/// <see cref="AssetsOptions"/> で指定したマウントは、このクラスのコンストラクタで行います。
/// プラグインの <c>OnStart</c> は登録した順に呼ばれるので、<c>OnStart</c> でマウントすると、先に登録された他のプラグインが
/// <c>OnStart</c> の中で読み込めないためです。
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
            catch { }

            throw;
        }
    }

    /// <summary>
    /// アセットを読み出す仮想ファイルシステムを取得します。
    /// </summary>
    public AssetFileSystem FileSystem { get; }

    /// <summary>
    /// <see cref="FileSystem"/> を破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
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
