using System;
using System.IO;
using System.Threading;

namespace Promete.Assets.Packaging;

/// <summary>
/// パッケージのバイト列を、位置を指定して読むための共有の読み出し元です。参照カウントで寿命を管理します。
/// </summary>
/// <remarks>
/// <para>
/// 作った時点で参照カウントは 1 (作った側の分) です。<see cref="TryAddRef"/> で増やし、<see cref="Release"/> で減らします。
/// 0 になった時点で <see cref="DisposeCore"/> を 1 回だけ呼び、以降は <see cref="TryAddRef"/> が失敗します。
/// </para>
/// <para>すべてのメンバーは、どのスレッドから同時に呼んでも安全です。</para>
/// </remarks>
internal abstract class PackageStorage
{
    private int _refCount = 1;

    /// <summary>
    /// 読み出し元の全体の長さ (バイト)。
    /// </summary>
    public abstract long Length { get; }

    /// <summary>
    /// 参照を 1 つ増やします。既に 0 になっていたら増やさずに <see langword="false"/> を返します。
    /// </summary>
    /// <returns>増やせた場合は <see langword="true"/>。</returns>
    public bool TryAddRef()
    {
        while (true)
        {
            var current = Volatile.Read(ref _refCount);
            if (current <= 0)
                return false;

            if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
                return true;
        }
    }

    /// <summary>
    /// 参照を 1 つ減らします。0 になったら <see cref="DisposeCore"/> を呼んで読み出し元を閉じます。
    /// </summary>
    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
            DisposeCore();
    }

    /// <summary>
    /// 指定した位置から読みます。どのスレッドから同時に呼んでも安全です。
    /// </summary>
    /// <param name="position">読み出し元の先頭からの位置。</param>
    /// <param name="buffer">読んだバイトを書き込む先。</param>
    /// <returns>読んだバイト数。末尾では 0。<paramref name="buffer"/> より少ないことがあります。</returns>
    public abstract int ReadAt(long position, Span<byte> buffer);

    /// <summary>
    /// 指定した位置から、<paramref name="buffer"/> がいっぱいになるまで読みます。
    /// </summary>
    /// <param name="position">読み出し元の先頭からの位置。</param>
    /// <param name="buffer">読んだバイトを書き込む先。</param>
    /// <exception cref="EndOfStreamException">末尾に達して、<paramref name="buffer"/> を埋められなかった場合。</exception>
    public void ReadExactlyAt(long position, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = ReadAt(position + total, buffer[total..]);
            if (read == 0)
                throw new EndOfStreamException();

            total += read;
        }
    }

    /// <summary>
    /// 読み出し元を閉じます。参照カウントが 0 になったときに 1 回だけ呼ばれます。
    /// </summary>
    protected abstract void DisposeCore();
}
