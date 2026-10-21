using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Promete.Assets.Packaging;

namespace Promete.Assets;

/// <summary>
/// アセットパッケージ (Promete.AssetPacker の <c>AssetPackageWriter</c> で書き出したもの) を読む <see cref="IAssetSource"/> です。
/// </summary>
/// <remarks>
/// <para>
/// ファイル、埋め込みリソース、任意の <see cref="Stream"/> から開けます。開くときに読むのはヘッダと目次だけで、
/// 各エントリの中身は <see cref="OpenRead"/> したときに、必要な部分だけを読みます (パッケージ全体をメモリに載せません)。
/// </para>
/// <para>
/// <see cref="OpenRead"/> は常に seek できるストリームを返します。無圧縮のエントリは、パッケージの一部を直接読むストリームを、
/// 圧縮したエントリは、展開した <see cref="MemoryStream"/> を返します。
/// 複数のストリームを同時に開いて、別々のスレッドから読めます。
/// </para>
/// <para>
/// <see cref="Dispose"/> の後も、既に開いたストリームは閉じられるまで有効です。パッケージのファイルや元のストリームは、
/// このソースと、開いたストリームのすべてが閉じられた時点で閉じます。
/// </para>
/// <para>
/// パッケージが壊れている場合は <see cref="InvalidDataException"/> を、対応していない形式のバージョンや機能 (暗号化など) の場合は
/// <see cref="NotSupportedException"/> をスローします。
/// </para>
/// </remarks>
public sealed class PackageAssetSource : IAssetSource, IDisposable
{
    /// <summary>
    /// Deflate で圧縮したエントリの、格納サイズに対する元のサイズの上限の倍率。
    /// </summary>
    private const ulong MaxDeflateRatio = 1032;

    /// <summary>
    /// Deflate で圧縮したエントリの、元のサイズの上限に足す余裕 (バイト)。
    /// </summary>
    private const ulong MaxDeflateRatioSlack = 64;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly PackageStorage _storage;
    private readonly string _description;
    private readonly PackageIndex _index;
    private int _disposed;

    /// <summary>
    /// ファイルからパッケージを開きます。
    /// </summary>
    /// <param name="filePath">パッケージのファイルのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが存在しない場合。</exception>
    /// <exception cref="InvalidDataException">パッケージが壊れている場合。メッセージにファイルのパスを含みます。</exception>
    /// <exception cref="NotSupportedException">対応していない形式のバージョンや機能の場合。</exception>
    public PackageAssetSource(string filePath, string mountPoint = "")
        : this(OpenFile(filePath, mountPoint)) { }

    /// <summary>
    /// ストリームからパッケージを開きます。ストリームの現在位置を、パッケージの先頭として扱います。
    /// </summary>
    /// <remarks>
    /// seek できないストリームは、開くときに全体をメモリに読み込みます (<paramref name="leaveOpen"/> が <see langword="false"/> なら、読み込んだ直後に閉じます)。
    /// seek できるストリームは、このソースが読み出し位置を管理するので、開いた後は他から読み書きしないでください。
    /// </remarks>
    /// <param name="stream">パッケージを読むストリーム。読み取りができること。</param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> なら、<paramref name="stream"/> を閉じません。
    /// <see langword="false"/> なら、このソースと開いたストリームのすべてが閉じられた時点で閉じます。構築に失敗した場合はその場で閉じます。
    /// </param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> が読み取りをできない場合。<paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="InvalidDataException">パッケージが壊れている場合。</exception>
    /// <exception cref="NotSupportedException">対応していない形式のバージョンや機能の場合。</exception>
    public PackageAssetSource(Stream stream, bool leaveOpen = false, string mountPoint = "")
        : this(CreateStreamStorage(stream, leaveOpen), mountPoint, "(ストリーム)") { }

    /// <summary>
    /// 検証済みのファイルを、読み出し元と合わせてこのソースに渡します。
    /// </summary>
    /// <param name="file">開いたファイルの情報。</param>
    private PackageAssetSource(OpenedFile file)
        : this(file.Storage, file.MountPoint, file.Description) { }

    /// <summary>
    /// 読み出し元からパッケージを開きます。読み出し元の参照は、このソースが引き受けます (失敗したら返します)。
    /// </summary>
    /// <param name="storage">パッケージのバイト列の読み出し元。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <param name="description">例外のメッセージに使う、パッケージの説明 (ファイルのパスや埋め込みリソースの名前など)。</param>
    private PackageAssetSource(PackageStorage storage, string mountPoint, string description)
    {
        _storage = storage;
        _description = description;

        try
        {
            _index = ReadIndex(storage, AssetPath.NormalizeDirectory(mountPoint));
        }
        catch (InvalidDataException e)
        {
            storage.Release();
            throw new InvalidDataException(
                $"アセットパッケージ {description} は壊れています: {e.Message}",
                e
            );
        }
        catch (NotSupportedException e)
        {
            storage.Release();
            throw new NotSupportedException(
                $"アセットパッケージ {description} は読めません: {e.Message}",
                e
            );
        }
        catch
        {
            storage.Release();
            throw;
        }
    }

    /// <summary>
    /// アセンブリの埋め込みリソースからパッケージを開きます。
    /// </summary>
    /// <param name="assembly">リソースを持つアセンブリ。</param>
    /// <param name="resourceName">リソースの名前 (<c>LogicalName</c>)。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <returns>開いたソース。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="FileNotFoundException">リソースが見つからない場合。</exception>
    /// <exception cref="InvalidDataException">パッケージが壊れている場合。メッセージにリソースの名前を含みます。</exception>
    /// <exception cref="NotSupportedException">対応していない形式のバージョンや機能の場合。</exception>
    public static PackageAssetSource FromEmbeddedResource(
        Assembly assembly,
        string resourceName,
        string mountPoint = ""
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourceName);
        var normalizedMountPoint = AssetPath.NormalizeDirectory(mountPoint);

        var stream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"埋め込みリソース '{resourceName}' がアセンブリ '{assembly.GetName().Name}' に見つかりません。",
                resourceName
            );

        return new PackageAssetSource(
            CreateStreamStorage(stream, leaveOpen: false),
            normalizedMountPoint,
            $"埋め込みリソース '{resourceName}'"
        );
    }

    /// <summary>
    /// ファイルのパッケージを開く前の検証をし、読み出し元を作ります。
    /// </summary>
    /// <param name="filePath">パッケージのファイルのパス。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <returns>開いたファイルの情報。</returns>
    private static OpenedFile OpenFile(string filePath, string mountPoint)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var normalizedMountPoint = AssetPath.NormalizeDirectory(mountPoint);

        var fullPath = Path.GetFullPath(filePath, AppContext.BaseDirectory);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"アセットパッケージ '{fullPath}' が見つかりません。",
                fullPath
            );
        }

        return new OpenedFile(
            new FilePackageStorage(fullPath),
            normalizedMountPoint,
            $"'{fullPath}'"
        );
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public bool Exists(string path)
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();

        return _index.Entries.ContainsKey(normalized);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidDataException">圧縮したエントリのデータが壊れている場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Stream OpenRead(string path)
    {
        var normalized = AssetPath.Normalize(path);
        ThrowIfDisposed();

        if (!_index.Entries.TryGetValue(normalized, out var entry))
        {
            if (_index.CaseHints.TryGetValue(normalized, out var actual))
            {
                throw new AssetPathCaseMismatchException(normalized, actual);
            }

            throw new FileNotFoundException(
                $"アセット '{normalized}' が見つかりません。",
                normalized
            );
        }

        if (!entry.IsCompressed)
        {
            return new PackageEntryStream(AcquireStorage(), entry.Offset, entry.OriginalSize);
        }

        return ReadCompressed(normalized, entry);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        var normalizedDirectory = AssetPath.NormalizeDirectory(directory);

        // パターンの検証 (null と区切り文字を弾く)
        AssetPath.IsMatch(string.Empty, pattern);
        ThrowIfDisposed();

        var prefix = normalizedDirectory.Length == 0 ? string.Empty : normalizedDirectory + "/";

        // 結果は配列にして返す (遅延評価すると、破棄された後に読むことになる)
        return _index
            .Paths.Where(path =>
            {
                if (!path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return false;
                }

                var rest = path[prefix.Length..];
                if (!recursive && rest.Contains('/'))
                {
                    return false;
                }

                var fileName = rest[(rest.LastIndexOf('/') + 1)..];
                return AssetPath.IsMatch(fileName, pattern);
            })
            .ToArray();
    }

    /// <summary>
    /// このソースを破棄します。既に開いたストリームは、閉じられるまで有効です。2 回目以降の呼び出しは何もしません。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _storage.Release();
        }
    }

    /// <summary>
    /// ストリームから読み出し元を作ります。seek できないストリームは全体を読み込み、元のストリームはここで閉じます。
    /// </summary>
    /// <param name="stream">パッケージを読むストリーム。</param>
    /// <param name="leaveOpen">元のストリームを閉じないかどうか。</param>
    /// <returns>読み出し元。</returns>
    private static PackageStorage CreateStreamStorage(Stream stream, bool leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException(
                    "ストリームは読み取りができる必要があります。",
                    nameof(stream)
                );
            }

            if (stream.CanSeek)
            {
                return new StreamPackageStorage(stream, leaveOpen);
            }
        }
        catch
        {
            if (!leaveOpen)
            {
                stream.Dispose();
            }

            throw;
        }

        // seek できないストリームは、現在位置から末尾までを読み込んで、元のストリームは閉じる
        var buffer = new MemoryStream();
        try
        {
            stream.CopyTo(buffer);
        }
        catch
        {
            buffer.Dispose();
            if (!leaveOpen)
            {
                stream.Dispose();
            }

            throw;
        }

        if (!leaveOpen)
        {
            stream.Dispose();
        }

        buffer.Position = 0;
        return new StreamPackageStorage(buffer, false);
    }

    /// <summary>
    /// ヘッダと目次を読んで検証し、エントリの索引を作ります。
    /// </summary>
    /// <param name="storage">パッケージのバイト列の読み出し元。</param>
    /// <param name="mountPoint">正規化済みの接頭辞。</param>
    /// <returns>エントリの索引。</returns>
    private static PackageIndex ReadIndex(PackageStorage storage, string mountPoint)
    {
        var length = (ulong)storage.Length;
        if (length < AssetPackageFormat.HeaderSize)
        {
            throw new InvalidDataException("ヘッダより短いです。");
        }

        var header = new byte[AssetPackageFormat.HeaderSize];
        storage.ReadExactlyAt(0, header);

        var magic = AssetPackageFormat.GetMagic();
        if (!header.AsSpan(AssetPackageFormat.MagicOffset, magic.Length).SequenceEqual(magic))
        {
            throw new InvalidDataException(
                "マジックが一致しません。アセットパッケージではありません。"
            );
        }

        // マイナーバージョンは見ない (どのマイナーバージョンも読める)
        var major = BinaryPrimitives.ReadUInt16LittleEndian(
            header.AsSpan(AssetPackageFormat.MajorVersionOffset)
        );
        var minor = BinaryPrimitives.ReadUInt16LittleEndian(
            header.AsSpan(AssetPackageFormat.MinorVersionOffset)
        );
        if (major != AssetPackageFormat.MajorVersion)
        {
            throw new NotSupportedException(
                $"形式のバージョン {major}.{minor} には対応していません (対応しているのは {AssetPackageFormat.MajorVersion}.x)。"
            );
        }

        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(
            header.AsSpan(AssetPackageFormat.HeaderSizeOffset)
        );
        if (headerSize < AssetPackageFormat.HeaderSize || headerSize > length)
        {
            throw new InvalidDataException($"ヘッダの大きさ {headerSize} が範囲外です。");
        }

        var flags = BinaryPrimitives.ReadUInt32LittleEndian(
            header.AsSpan(AssetPackageFormat.HeaderFlagsOffset)
        );
        if ((flags & AssetPackageFormat.HeaderFlagTocEncrypted) != 0)
        {
            throw new NotSupportedException("目次が暗号化されています。暗号化は未対応です。");
        }

        if (flags != 0)
        {
            throw new NotSupportedException($"未知のヘッダのフラグ 0x{flags:X8} があります。");
        }

        var tocOffset = BinaryPrimitives.ReadUInt64LittleEndian(
            header.AsSpan(AssetPackageFormat.TocOffsetOffset)
        );
        var tocSize = BinaryPrimitives.ReadUInt64LittleEndian(
            header.AsSpan(AssetPackageFormat.TocSizeOffset)
        );
        if (tocOffset < headerSize || tocOffset > length)
        {
            throw new InvalidDataException("目次の位置が範囲外です。");
        }

        if (tocSize > length - tocOffset || tocSize > int.MaxValue)
        {
            throw new InvalidDataException("目次の大きさが範囲外です。");
        }

        var toc = new byte[(int)tocSize];
        storage.ReadExactlyAt((long)tocOffset, toc);

        return ReadToc(storage, toc, headerSize, length, mountPoint);
    }

    /// <summary>
    /// 目次を読んで検証し、エントリの索引を作ります。
    /// </summary>
    /// <param name="storage">パッケージのバイト列の読み出し元。</param>
    /// <param name="toc">目次のバイト列。</param>
    /// <param name="headerSize">ヘッダの大きさ。データはこれより後ろにあること。</param>
    /// <param name="length">パッケージ全体の長さ。</param>
    /// <param name="mountPoint">正規化済みの接頭辞。</param>
    /// <returns>エントリの索引。</returns>
    private static PackageIndex ReadToc(
        PackageStorage storage,
        byte[] toc,
        uint headerSize,
        ulong length,
        string mountPoint
    )
    {
        var position = 0;
        var entryCount = ReadUInt32(toc, ref position);

        // 巨大な件数で配列を確保しないように、目次に収まる件数かを先に確かめる
        var remaining = toc.Length - position;
        if (entryCount > (uint)(remaining / AssetPackageFormat.TocEntryFixedSize))
        {
            throw new InvalidDataException($"エントリの数 {entryCount} が目次に収まりません。");
        }

        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        var caseHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (uint i = 0; i < entryCount; i++)
        {
            var pathLength = ReadUInt16(toc, ref position);
            var pathStart = Take(toc, ref position, pathLength);
            var path = DecodePath(toc, pathStart, pathLength);
            if (!IsNormalized(path))
            {
                throw new InvalidDataException($"パス '{path}' が正規化されていません。");
            }

            var entryFlags = ReadUInt32(toc, ref position);
            var offset = ReadUInt64(toc, ref position);
            var storedSize = ReadUInt64(toc, ref position);
            var originalSize = ReadUInt64(toc, ref position);
            Take(toc, ref position, AssetPackageFormat.NonceSize);
            var extraLength = ReadUInt16(toc, ref position);

            // 拡張領域は読み飛ばす (マイナーバージョンで足されたもの)
            Take(toc, ref position, extraLength);

            if ((entryFlags & AssetPackageFormat.EntryFlagEncrypted) != 0)
            {
                throw new NotSupportedException(
                    $"パス '{path}' のエントリは暗号化されています。暗号化は未対応です。"
                );
            }

            if ((entryFlags & ~AssetPackageFormat.EntryFlagDeflate) != 0)
            {
                throw new NotSupportedException(
                    $"パス '{path}' のエントリに未知のフラグ 0x{entryFlags:X8} があります。"
                );
            }

            var compressed = (entryFlags & AssetPackageFormat.EntryFlagDeflate) != 0;

            if (offset < headerSize || offset > length || storedSize > length - offset)
            {
                throw new InvalidDataException($"パス '{path}' のデータが範囲外です。");
            }

            if (!compressed && storedSize != originalSize)
            {
                throw new InvalidDataException(
                    $"パス '{path}' は無圧縮ですが、格納サイズと元のサイズが違います。"
                );
            }

            // Deflate の圧縮率には上限があるので、格納サイズからあり得ない元のサイズを開く時点で拒否する
            // (storedSize * 上限 が ulong で桁あふれする場合は、上限を計算できないが、元のサイズがそれを超えることはない)
            if (
                compressed
                && storedSize <= (ulong.MaxValue - MaxDeflateRatioSlack) / MaxDeflateRatio
                && originalSize > storedSize * MaxDeflateRatio + MaxDeflateRatioSlack
            )
            {
                throw new InvalidDataException(
                    $"パス '{path}' の元のサイズ {originalSize} が、格納サイズ {storedSize} に対して大きすぎます。"
                );
            }

            if (compressed && originalSize > (ulong)Array.MaxLength)
            {
                throw new InvalidDataException(
                    $"パス '{path}' の元のサイズ {originalSize} が大きすぎます。"
                );
            }

            var key = mountPoint.Length == 0 ? path : mountPoint + "/" + path;
            if (entries.ContainsKey(key))
            {
                throw new InvalidDataException($"パス '{key}' が重複しています。");
            }

            entries.Add(
                key,
                new Entry((long)offset, (long)storedSize, (long)originalSize, compressed)
            );

            // 大文字・小文字の不一致のヒントは、最初に入ったものを残す
            caseHints.TryAdd(key, key);
        }

        var paths = entries.Keys.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        return new PackageIndex(entries, caseHints, paths);
    }

    /// <summary>
    /// 目次のバイト列から、指定した大きさを読み飛ばし、その先頭の位置を返します。
    /// </summary>
    /// <param name="bytes">目次のバイト列。</param>
    /// <param name="position">読み位置。進めた後の位置に更新されます。</param>
    /// <param name="count">読み飛ばすバイト数。</param>
    /// <returns>読み飛ばす前の位置。</returns>
    private static int Take(byte[] bytes, ref int position, int count)
    {
        if (count < 0 || bytes.Length - position < count)
        {
            throw new InvalidDataException("目次が途中で切れています。");
        }

        var start = position;
        position += count;
        return start;
    }

    /// <summary>
    /// 目次のバイト列から、リトルエンディアンの 2 バイト符号なし整数を読みます。
    /// </summary>
    /// <param name="bytes">目次のバイト列。</param>
    /// <param name="position">読み位置。進めた後の位置に更新されます。</param>
    /// <returns>読んだ値。</returns>
    private static ushort ReadUInt16(byte[] bytes, ref int position)
    {
        var start = Take(bytes, ref position, 2);
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(start, 2));
    }

    /// <summary>
    /// 目次のバイト列から、リトルエンディアンの 4 バイト符号なし整数を読みます。
    /// </summary>
    /// <param name="bytes">目次のバイト列。</param>
    /// <param name="position">読み位置。進めた後の位置に更新されます。</param>
    /// <returns>読んだ値。</returns>
    private static uint ReadUInt32(byte[] bytes, ref int position)
    {
        var start = Take(bytes, ref position, 4);
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(start, 4));
    }

    /// <summary>
    /// 目次のバイト列から、リトルエンディアンの 8 バイト符号なし整数を読みます。
    /// </summary>
    /// <param name="bytes">目次のバイト列。</param>
    /// <param name="position">読み位置。進めた後の位置に更新されます。</param>
    /// <returns>読んだ値。</returns>
    private static ulong ReadUInt64(byte[] bytes, ref int position)
    {
        var start = Take(bytes, ref position, 8);
        return BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(start, 8));
    }

    /// <summary>
    /// 目次のパスを UTF-8 として復号します。不正なバイト列は <see cref="InvalidDataException"/> にします。
    /// </summary>
    /// <param name="bytes">目次のバイト列。</param>
    /// <param name="start">パスの先頭の位置。</param>
    /// <param name="count">パスのバイト数。</param>
    /// <returns>復号したパス。</returns>
    private static string DecodePath(byte[] bytes, int start, int count)
    {
        try
        {
            return StrictUtf8.GetString(bytes, start, count);
        }
        catch (DecoderFallbackException e)
        {
            throw new InvalidDataException("パスが UTF-8 として不正です。", e);
        }
    }

    /// <summary>
    /// パスが <see cref="AssetPath.Normalize"/> の結果と同じかどうかを取得します。
    /// </summary>
    /// <param name="path">検査するパス。</param>
    /// <returns>正規化済みであれば <see langword="true"/>。</returns>
    private static bool IsNormalized(string path)
    {
        try
        {
            return AssetPath.Normalize(path) == path;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// 圧縮したエントリを展開して、展開したバイト列を読むストリームを返します。展開したデータはメモリに載せます。
    /// </summary>
    /// <param name="path">正規化済みの論理パス (例外のメッセージ用)。</param>
    /// <param name="entry">対象のエントリ。</param>
    /// <returns>展開したデータを読む <see cref="MemoryStream"/>。</returns>
    private Stream ReadCompressed(string path, Entry entry)
    {
        var buffer = new byte[entry.OriginalSize];
        try
        {
            using var raw = new PackageEntryStream(
                AcquireStorage(),
                entry.Offset,
                entry.StoredSize
            );
            using var inflater = new DeflateStream(raw, CompressionMode.Decompress);

            var total = 0;
            while (total < buffer.Length)
            {
                var read = inflater.Read(buffer, total, buffer.Length - total);
                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            // 目次の元のサイズより短い、または長いデータは壊れている
            if (total != buffer.Length || inflater.ReadByte() >= 0)
            {
                throw new InvalidDataException("展開したサイズが目次の元のサイズと一致しません。");
            }
        }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException(
                $"アセット '{path}' ({_description}) の圧縮データが壊れています: {e.Message}",
                e
            );
        }

        return new MemoryStream(buffer, 0, buffer.Length, writable: false, publiclyVisible: true);
    }

    /// <summary>
    /// 読み出し元の参照を 1 つ取得します。取得した参照は、<see cref="PackageEntryStream"/> が閉じるときに返します。
    /// </summary>
    /// <returns>読み出し元。</returns>
    /// <exception cref="ObjectDisposedException">このソースが破棄されている場合。</exception>
    private PackageStorage AcquireStorage()
    {
        var acquired = _storage.TryAddRef();
        if (acquired && Volatile.Read(ref _disposed) == 0)
        {
            return _storage;
        }

        // Dispose と競合した場合は、取った参照を返してから例外にする
        if (acquired)
        {
            _storage.Release();
        }

        throw new ObjectDisposedException(nameof(PackageAssetSource));
    }

    /// <summary>
    /// 破棄されていたら <see cref="ObjectDisposedException"/> をスローします。
    /// </summary>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    /// <summary>
    /// ファイルを検証して開いた結果です。
    /// </summary>
    /// <param name="Storage">ファイルの読み出し元。</param>
    /// <param name="MountPoint">正規化済みの接頭辞。</param>
    /// <param name="Description">例外のメッセージに使う、ファイルの説明。</param>
    private readonly record struct OpenedFile(
        FilePackageStorage Storage,
        string MountPoint,
        string Description
    );

    /// <summary>
    /// 目次の 1 件分の情報です。
    /// </summary>
    /// <param name="Offset">データの位置 (パッケージの先頭から)。</param>
    /// <param name="StoredSize">格納しているバイト数。</param>
    /// <param name="OriginalSize">展開後のバイト数。</param>
    /// <param name="IsCompressed">データが Deflate で圧縮されているかどうか。</param>
    private sealed record Entry(long Offset, long StoredSize, long OriginalSize, bool IsCompressed);

    /// <summary>
    /// 開くときに作る、エントリの索引です。
    /// </summary>
    /// <param name="Entries">論理パス (接頭辞付き) からエントリへの対応表。</param>
    /// <param name="CaseHints">大文字・小文字を無視したときの論理パスから、実際の論理パスへの対応表。</param>
    /// <param name="Paths">論理パスの序数順の一覧。</param>
    private sealed record PackageIndex(
        Dictionary<string, Entry> Entries,
        Dictionary<string, string> CaseHints,
        string[] Paths
    );
}
