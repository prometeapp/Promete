using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Promete.Assets.Infrastructure;

/// <summary>
/// デスクトップ形式のマニフェスト (Folder モードで出力先に置く <c>&lt;AssemblyName&gt;.promete-assets.json</c>) を読みます。
/// </summary>
/// <remarks>
/// <para>形式 (UTF-8 の JSON。BOM は無くてもあってもよい):</para>
/// <code>
/// { "version": 1, "files": { "assets/x.png": "C:\\path\\to\\assets\\x.png", ... } }
/// </code>
/// <para>
/// <c>version</c> は必須の整数で、1 以外は <see cref="NotSupportedException"/>。<c>files</c> は必須のオブジェクトで、キーは論理パス、
/// 値は実ファイルの絶対パス (文字列) です。知らないプロパティは読み飛ばします。キーの重複は不正です。
/// 書き出すのは MSBuild Task (Promete.AssetPacker.Tasks の <c>WritePrometeAssetManifest</c>) です。
/// </para>
/// <para>NativeAOT で動かすため、リフレクションに基づく <c>JsonSerializer</c> は使わず、<c>Utf8JsonReader</c> で読みます。</para>
/// </remarks>
internal static class DesktopAssetManifest
{
    /// <summary>
    /// 読める形式のバージョン。
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// マニフェストのファイル名の末尾 (<c>&lt;AssemblyName&gt;</c> の後ろ)。
    /// </summary>
    public const string FileNameSuffix = ".promete-assets.json";

    /// <summary>
    /// マニフェストのファイルを読みます。
    /// </summary>
    /// <param name="path">マニフェストのファイルのパス。</param>
    /// <returns>論理パスから実ファイルの絶対パスへの対応。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが無い場合。</exception>
    /// <exception cref="InvalidDataException">形式が正しくない場合。メッセージにファイルのパスを含みます。</exception>
    /// <exception cref="NotSupportedException"><c>version</c> が 1 以外の場合。</exception>
    public static IReadOnlyDictionary<string, string> Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileNotFoundException($"マニフェストが見つかりません: '{path}'", path, e);
        }

        return Parse(bytes, $"'{path}'");
    }

    /// <summary>
    /// マニフェストの中身 (UTF-8 の JSON) を解析します。
    /// </summary>
    /// <param name="json">マニフェストの中身。先頭の UTF-8 の BOM は読み飛ばします。</param>
    /// <param name="description">例外のメッセージに使う、マニフェストの説明。</param>
    /// <returns>論理パスから実ファイルの絶対パスへの対応 (キーは序数比較)。</returns>
    /// <exception cref="InvalidDataException">JSON として正しくない場合、必須のプロパティが無い場合、型が違う場合、キーが重複する場合。</exception>
    /// <exception cref="NotSupportedException"><c>version</c> が 1 以外の場合。</exception>
    public static IReadOnlyDictionary<string, string> Parse(
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
            return ParseObject(ref reader, description);
        }
        catch (JsonException e)
        {
            throw Invalid(description, "JSON として正しくありません", e);
        }
    }

    /// <summary>
    /// 最上位のオブジェクトを読み、<c>version</c> と <c>files</c> を取り出します。
    /// </summary>
    private static IReadOnlyDictionary<string, string> ParseObject(
        ref Utf8JsonReader reader,
        string description
    )
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw Invalid(description, "最上位がオブジェクトではありません");
        }

        var hasVersion = false;
        Dictionary<string, string>? files = null;
        while (true)
        {
            if (!reader.Read())
            {
                throw Invalid(description, "途中で終わっています");
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var name = reader.GetString()!;
            if (name == "version")
            {
                if (hasVersion)
                {
                    throw Invalid(description, "version が重複しています");
                }

                hasVersion = true;
                ReadVersion(ref reader, description);
            }
            else if (name == "files")
            {
                if (files is not null)
                {
                    throw Invalid(description, "files が重複しています");
                }

                files = ReadFiles(ref reader, description);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        if (!hasVersion)
        {
            throw Invalid(description, "version がありません");
        }

        if (files is null)
        {
            throw Invalid(description, "files がありません");
        }

        return files;
    }

    /// <summary>
    /// <c>version</c> の値を読み、対応していないバージョンなら <see cref="NotSupportedException"/> を投げます。
    /// </summary>
    private static void ReadVersion(ref Utf8JsonReader reader, string description)
    {
        if (
            !reader.Read()
            || reader.TokenType != JsonTokenType.Number
            || !reader.TryGetInt32(out var version)
        )
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
    /// <c>files</c> のオブジェクトを読み、論理パスから実ファイルのパスへの辞書を作ります。
    /// </summary>
    private static Dictionary<string, string> ReadFiles(
        ref Utf8JsonReader reader,
        string description
    )
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw Invalid(description, "files はオブジェクトである必要があります");
        }

        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        while (true)
        {
            if (!reader.Read())
            {
                throw Invalid(description, "途中で終わっています");
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return files;
            }

            var key = reader.GetString()!;
            if (files.ContainsKey(key))
            {
                throw Invalid(description, $"論理パス '{key}' が重複しています");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw Invalid(description, $"'{key}' の値は文字列である必要があります");
            }

            files.Add(key, reader.GetString()!);
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
