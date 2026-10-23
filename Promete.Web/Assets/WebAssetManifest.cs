using System.Text.Json;

namespace Promete.Web.Assets;

/// <summary>
/// Web 形式のマニフェスト (<c>promete-assets.json</c>) を読みます。
/// </summary>
/// <remarks>
/// <para>
/// 形式 (UTF-8 の JSON) は <c>Promete.Web.targets</c> が書き出し、<c>promete.js</c> が取得して仮想ファイルシステムのルートに置きます。
/// 3 者の契約は <c>PrometeWebAssetsContract</c> (今は 1) の中に閉じます。
/// </para>
/// <code>
/// {
///   "version": 1,
///   "entries": [
///     { "kind": "file", "path": "/assets/x.png", "url": "assets/x.png", "logicalPath": "assets/x.png" },
///     { "kind": "font", "path": "/assets/a.ttf", "url": "assets/a.ttf", "logicalPath": "assets/a.ttf" },
///     { "kind": "package", "path": "/MyGame.main.prmtpak", "url": "MyGame.main.prmtpak", "package": "main" },
///     { "kind": "embedded-package", "assembly": "MyGame", "resource": "MyGame.main.prmtpak", "package": "main" }
///   ]
/// }
/// </code>
/// <para>
/// <c>version</c> (整数) と <c>entries</c> (配列) は必須。各エントリの <c>kind</c> (文字列) は必須で、それ以外のプロパティは任意です。
/// 知らないプロパティ (<c>url</c> など、JavaScript だけが使うもの) は読み飛ばします。<c>version</c> が 1 以外なら <see cref="NotSupportedException"/>。
/// </para>
/// </remarks>
internal static class WebAssetManifest
{
    /// <summary>
    /// 読める形式のバージョン。
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// ばらのファイル (フォント以外)。
    /// </summary>
    public const string KindFile = "file";

    /// <summary>
    /// ばらのフォントのファイル。<c>promete.js</c> が <c>FontFace</c> にも登録します。
    /// </summary>
    public const string KindFont = "font";

    /// <summary>
    /// 出力先に置いたアセットパッケージ。<c>promete.js</c> が仮想ファイルシステムに置き、中のフォントを登録します。
    /// </summary>
    public const string KindPackage = "package";

    /// <summary>
    /// アセンブリに埋め込んだアセットパッケージ。取得はしませんが、<c>promete.js</c> が中のフォントを登録します。
    /// </summary>
    public const string KindEmbeddedPackage = "embedded-package";

    /// <summary>
    /// マニフェストの中身を解析します。
    /// </summary>
    /// <param name="json">マニフェストの中身 (UTF-8)。先頭の BOM は読み飛ばします。</param>
    /// <param name="description">例外のメッセージに使う、マニフェストの説明。</param>
    /// <returns>エントリの一覧 (書かれた順)。</returns>
    /// <exception cref="InvalidDataException">JSON として正しくない場合、必須のプロパティが無い場合、型が違う場合。</exception>
    /// <exception cref="NotSupportedException"><c>version</c> が 1 以外の場合。</exception>
    public static IReadOnlyList<WebAssetManifestEntry> Parse(
        ReadOnlySpan<byte> json,
        string description
    )
    {
        if (json.Length >= 3 && json[0] == 0xEF && json[1] == 0xBB && json[2] == 0xBF)
        {
            json = json.Slice(3);
        }

        try
        {
            var reader = new Utf8JsonReader(json, isFinalBlock: true, state: default);
            return ParseRoot(ref reader, description);
        }
        catch (JsonException e)
        {
            throw Invalid(description, "JSON として正しくありません", e);
        }
    }

    /// <summary>
    /// 最上位のオブジェクトを読み、<c>version</c> を確かめて <c>entries</c> を取り出します。
    /// </summary>
    private static IReadOnlyList<WebAssetManifestEntry> ParseRoot(
        ref Utf8JsonReader reader,
        string description
    )
    {
        Advance(ref reader, description);
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw Invalid(description, "最上位がオブジェクトではありません");
        }

        var hasVersion = false;
        List<WebAssetManifestEntry>? entries = null;
        while (true)
        {
            Advance(ref reader, description);
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var name = reader.GetString()!;
            Advance(ref reader, description);
            if (name == "version")
            {
                if (hasVersion)
                {
                    throw Invalid(description, "version が重複しています");
                }

                hasVersion = true;
                ReadVersion(ref reader, description);
            }
            else if (name == "entries")
            {
                if (entries is not null)
                {
                    throw Invalid(description, "entries が重複しています");
                }

                entries = ReadEntries(ref reader, description);
            }
            else
            {
                reader.Skip();
            }
        }

        if (!hasVersion)
        {
            throw Invalid(description, "version がありません");
        }

        if (entries is null)
        {
            throw Invalid(description, "entries がありません");
        }

        return entries;
    }

    /// <summary>
    /// <c>version</c> の値を確かめます。1 以外なら <see cref="NotSupportedException"/> を投げます。
    /// </summary>
    private static void ReadVersion(ref Utf8JsonReader reader, string description)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var version))
        {
            throw Invalid(description, "version は整数である必要があります");
        }

        if (version != FormatVersion)
        {
            throw new NotSupportedException(
                $"マニフェストの version {version} には対応していません ({description})。Promete を更新するか、ビルドし直してください。"
            );
        }
    }

    /// <summary>
    /// <c>entries</c> の配列を読みます。現在のトークンは配列の開始を指している必要があります (読み終えると終了のトークン)。
    /// </summary>
    private static List<WebAssetManifestEntry> ReadEntries(
        ref Utf8JsonReader reader,
        string description
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw Invalid(description, "entries は配列である必要があります");
        }

        var entries = new List<WebAssetManifestEntry>();
        while (true)
        {
            Advance(ref reader, description);
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return entries;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw Invalid(description, "entries の要素はオブジェクトである必要があります");
            }

            entries.Add(ReadEntry(ref reader, description));
        }
    }

    /// <summary>
    /// エントリのオブジェクトを読みます。現在のトークンはオブジェクトの開始を指している必要があります。
    /// </summary>
    private static WebAssetManifestEntry ReadEntry(ref Utf8JsonReader reader, string description)
    {
        string? kind = null;
        string? path = null;
        string? logicalPath = null;
        string? package = null;
        string? assembly = null;
        string? resource = null;

        while (true)
        {
            Advance(ref reader, description);
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var name = reader.GetString()!;
            Advance(ref reader, description);
            switch (name)
            {
                case "kind":
                    kind = ReadString(ref reader, name, description);
                    break;
                case "path":
                    path = ReadString(ref reader, name, description);
                    break;
                case "logicalPath":
                    logicalPath = ReadString(ref reader, name, description);
                    break;
                case "package":
                    package = ReadString(ref reader, name, description);
                    break;
                case "assembly":
                    assembly = ReadString(ref reader, name, description);
                    break;
                case "resource":
                    resource = ReadString(ref reader, name, description);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (kind is null)
        {
            throw Invalid(description, "エントリに kind がありません");
        }

        return new WebAssetManifestEntry(kind, path, logicalPath, package, assembly, resource);
    }

    /// <summary>
    /// 現在のトークンが文字列であることを確かめて、その値を返します。
    /// </summary>
    private static string ReadString(ref Utf8JsonReader reader, string name, string description)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw Invalid(description, $"'{name}' は文字列である必要があります");
        }

        return reader.GetString()!;
    }

    /// <summary>
    /// 次のトークンに進みます。JSON が途中で終わっている場合は <see cref="InvalidDataException"/> を投げます。
    /// </summary>
    private static void Advance(ref Utf8JsonReader reader, string description)
    {
        if (!reader.Read())
        {
            throw Invalid(description, "途中で終わっています");
        }
    }

    /// <summary>
    /// 形式が正しくないことを表す <see cref="InvalidDataException"/> を作ります。
    /// </summary>
    private static InvalidDataException Invalid(
        string description,
        string detail,
        Exception? inner = null
    ) => new($"マニフェストの形式が正しくありません ({description}): {detail}", inner);
}
