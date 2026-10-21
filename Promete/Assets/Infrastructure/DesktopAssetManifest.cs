using System;
using System.Collections.Generic;
using System.IO;

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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
