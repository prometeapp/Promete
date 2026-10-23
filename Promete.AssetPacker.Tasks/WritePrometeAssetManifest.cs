using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Promete.AssetPacker.Tasks;

/// <summary>
/// デスクトップ形式のマニフェスト (Folder モード) を書き出します。中身が変わらないときは書き込みません。
/// </summary>
/// <remarks>
/// <para>形式 (UTF-8、BOM なし):</para>
/// <code>
/// { "version": 1, "files": { "&lt;LogicalPath&gt;": "&lt;実ファイルの絶対パス&gt;", ... } }
/// </code>
/// <para>
/// キーは <c>LogicalPath</c> の序数順に並べます。文字列は JSON の規則でエスケープします (<c>"</c>、<c>\</c>、制御文字)。
/// 読むのは Promete の <c>DesktopAssetManifest</c> です。形式を変えるときは両方を直してください。
/// </para>
/// </remarks>
public sealed class WritePrometeAssetManifest : Task
{
    /// <summary>
    /// <see cref="ResolvePrometeAssets"/> が出力した item (ItemSpec が実ファイルの絶対パス、<c>LogicalPath</c> を持つ)。
    /// </summary>
    [Required]
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>
    /// 書き出すファイルのパス。ディレクトリが無ければ作ります。
    /// </summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>
    /// ファイルを書き込んだ (中身が変わった、またはファイルが無かった) 場合は <see langword="true"/>。
    /// </summary>
    [Output]
    public bool Written { get; private set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        Written = false;
        try
        {
            // 実行時のリーダーは重複したキーを不正とするので、壊れたマニフェストは書かず既存のファイルも残す
            var duplicate = FindDuplicateLogicalPath();
            if (duplicate != null)
            {
                LogError(
                    ErrorCodes.ManifestWriteFailed,
                    $"論理パス '{duplicate}' を持つ item が複数あるため、マニフェストを書き出せません"
                );
                return false;
            }

            var outputPath = Path.GetFullPath(OutputFile);
            var bytes = BuildManifest();
            if (File.Exists(outputPath) && IsSameContent(outputPath, bytes))
            {
                return true;
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, bytes);
            Written = true;
            return true;
        }
        catch (Exception ex)
            when (ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException
            )
        {
            LogError(
                ErrorCodes.ManifestWriteFailed,
                $"マニフェストを書き出せません '{OutputFile}': {ex.Message}"
            );
            return false;
        }
    }

    /// <summary>
    /// 論理パスが序数で重複している最初のものを探します。
    /// </summary>
    /// <returns>重複している論理パス。重複が無ければ <see langword="null"/>。</returns>
    private string? FindDuplicateLogicalPath()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Assets)
        {
            var logical = item.GetMetadata("LogicalPath");
            if (!seen.Add(logical))
            {
                return logical;
            }
        }

        return null;
    }

    /// <summary>
    /// 文字列を JSON の文字列リテラルにします。<c>"</c>、<c>\</c>、0x20 未満の文字をエスケープします。
    /// </summary>
    /// <param name="value">エスケープする文字列。</param>
    /// <returns>引用符で囲まれた JSON の文字列。</returns>
    private static string EscapeJson(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var c in value)
        {
            if (c == '"')
            {
                builder.Append("\\\"");
            }
            else if (c == '\\')
            {
                builder.Append("\\\\");
            }
            else if (c < 0x20)
            {
                builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(c);
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>
    /// 既存のファイルの中身が、書こうとしている内容と同じかを取得します。
    /// </summary>
    /// <param name="path">既存のファイルのフルパス。</param>
    /// <param name="bytes">書こうとしているバイト列。</param>
    /// <returns>同じなら <see langword="true"/>。</returns>
    private static bool IsSameContent(string path, byte[] bytes) =>
        File.ReadAllBytes(path).SequenceEqual(bytes);

    /// <summary>
    /// <see cref="Assets"/> から、論理パスの序数順に並べたマニフェストの UTF-8 (BOM なし) のバイト列を作ります。
    /// </summary>
    /// <returns>マニフェストのバイト列。</returns>
    private byte[] BuildManifest()
    {
        var entries = new List<KeyValuePair<string, string>>(Assets.Length);
        foreach (var item in Assets)
        {
            entries.Add(
                new KeyValuePair<string, string>(
                    item.GetMetadata("LogicalPath"),
                    Path.GetFullPath(item.ItemSpec)
                )
            );
        }

        // 入力の順序に依存しないよう、論理パス、実ファイルのパスの順に並べる
        entries.Sort(
            (a, b) =>
            {
                var byKey = string.CompareOrdinal(a.Key, b.Key);
                return byKey != 0 ? byKey : string.CompareOrdinal(a.Value, b.Value);
            }
        );

        var builder = new StringBuilder();
        builder.Append("{\n  \"version\": 1,\n  \"files\": {");
        for (var i = 0; i < entries.Count; i++)
        {
            builder.Append(i == 0 ? "\n" : ",\n");
            builder
                .Append("    ")
                .Append(EscapeJson(entries[i].Key))
                .Append(": ")
                .Append(EscapeJson(entries[i].Value));
        }

        builder.Append(entries.Count == 0 ? "}\n}\n" : "\n  }\n}\n");
        return new UTF8Encoding(false).GetBytes(builder.ToString());
    }

    /// <summary>
    /// エラーを報告します。メッセージは書式として扱われないように <c>{0}</c> の引数で渡します。
    /// </summary>
    /// <param name="code">エラーのコード (<see cref="ErrorCodes"/>)。</param>
    /// <param name="message">メッセージ。</param>
    private void LogError(string code, string message) =>
        Log.LogError(null, code, null, null, 0, 0, 0, 0, "{0}", message);
}
