using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Promete.AssetPacker;

namespace Promete.AssetPacker.Tasks;

/// <summary>
/// <c>PrometeAsset</c> の item を検証し、論理パスとメタデータを正規化します。
/// </summary>
/// <remarks>
/// <para>出力の各 item (<see cref="ResolvedAssets"/>) は、ItemSpec が実ファイルの絶対パスで、次のメタデータを持ちます。</para>
/// <list type="bullet">
/// <item><c>LogicalPath</c>: 正規化した論理パス (<c>/</c> 区切り、NFC)。</item>
/// <item><c>Package</c>: パッケージ名。省略時は <c>main</c>。</item>
/// <item><c>Compress</c>: <c>auto</c> / <c>true</c> / <c>false</c> (小文字)。省略時は <c>auto</c>。</item>
/// <item><c>Obfuscate</c>: <c>true</c> / <c>false</c> (小文字)。省略時は <c>false</c>。Phase 5 で使います。</item>
/// <item><c>SourceItemSpec</c>: 元の item の ItemSpec。</item>
/// </list>
/// <para>出力は <c>LogicalPath</c> の序数順に並べます (指紋のファイルの中身を、item の並び順に依存させないため)。</para>
/// <para>エラーはすべて報告してから <see langword="false"/> を返します。エラーのコードは <c>PROMETE1001</c>〜 (<see cref="ErrorCodes"/>)。</para>
/// </remarks>
public sealed class ResolvePrometeAssets : Task
{
    /// <summary>
    /// <c>PrometeAsset</c> の item。ItemSpec はプロジェクトのディレクトリからの相対パス (Include に書いたもの) か絶対パスです。
    /// </summary>
    [Required]
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>
    /// プロジェクトのディレクトリ (<c>$(MSBuildProjectDirectory)</c>)。相対の ItemSpec の基準です。
    /// </summary>
    [Required]
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 検証と正規化を終えた item。
    /// </summary>
    [Output]
    public ITaskItem[] ResolvedAssets { get; private set; } = [];

    /// <summary>
    /// パックの処理のバージョン。<c>&lt;形式のメジャー&gt;.&lt;形式のマイナー&gt;+&lt;この Task のアセンブリのバージョン&gt;</c> の形です。
    /// インクリメンタルビルドの指紋に含め、Promete を更新したときにパッケージを作り直させます。
    /// </summary>
    [Output]
    public string PackerVersion { get; private set; } = string.Empty;

    /// <inheritdoc />
    public override bool Execute()
    {
        PackerVersion =
            $"{AssetPackageWriter.FormatMajorVersion}.{AssetPackageWriter.FormatMinorVersion}+{typeof(ResolvePrometeAssets).Assembly.GetName().Version}";

        var succeeded = true;
        var resolved = new List<ITaskItem>();
        var sourceByLogicalPath = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in Assets)
        {
            var output = ResolveItem(item);
            if (output is null)
            {
                succeeded = false;
                continue;
            }

            var logicalPath = output.GetMetadata("LogicalPath");
            if (sourceByLogicalPath.TryGetValue(logicalPath, out var firstSpec))
            {
                LogError(
                    ErrorCodes.DuplicateLogicalPath,
                    $"論理パス '{logicalPath}' が重複しています ('{firstSpec}' と '{item.ItemSpec}')。TargetPath を変えてください。"
                );
                succeeded = false;
                continue;
            }

            sourceByLogicalPath.Add(logicalPath, item.ItemSpec);
            resolved.Add(output);
        }

        if (!CheckPackageNameCase(resolved))
        {
            succeeded = false;
        }

        resolved.Sort(
            (a, b) =>
                string.CompareOrdinal(a.GetMetadata("LogicalPath"), b.GetMetadata("LogicalPath"))
        );
        ResolvedAssets = resolved.ToArray();
        return succeeded;
    }

    /// <summary>
    /// 大文字・小文字だけが違うパッケージ名が混ざっていないかを確かめます。綴りの組ごとに 1 回だけ報告します。
    /// </summary>
    /// <param name="resolved">検証を通った item。</param>
    /// <returns>違う綴りが無ければ <see langword="true"/>。</returns>
    private bool CheckPackageNameCase(List<ITaskItem> resolved)
    {
        var firstSpellingByFold = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var reportedPairs = new HashSet<string>(StringComparer.Ordinal);
        var succeeded = true;
        foreach (var item in resolved)
        {
            var package = item.GetMetadata("Package");
            if (!firstSpellingByFold.TryGetValue(package, out var first))
            {
                firstSpellingByFold.Add(package, package);
                continue;
            }

            if (string.Equals(first, package, StringComparison.Ordinal))
            {
                continue;
            }

            if (reportedPairs.Add(first + "\n" + package))
            {
                LogError(
                    ErrorCodes.PackageNameCaseConflict,
                    $"パッケージ名 '{first}' と '{package}' は大文字・小文字だけが違います。綴りを揃えてください。"
                );
            }

            succeeded = false;
        }

        return succeeded;
    }

    /// <summary>
    /// パスに <c>..</c> の要素があるかを取得します。
    /// </summary>
    /// <param name="path"><c>/</c> 区切りのパス。</param>
    /// <returns><c>..</c> の要素があれば <see langword="true"/>。</returns>
    private static bool HasParentSegment(string path)
    {
        foreach (var segment in path.Split('/'))
        {
            if (segment == "..")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// パッケージ名が英数字 (ASCII)・<c>_</c>・<c>-</c> だけでできているかを取得します。
    /// </summary>
    /// <param name="name">調べるパッケージ名。</param>
    /// <returns>使える名前なら <see langword="true"/>。</returns>
    private static bool IsValidPackageName(string name)
    {
        foreach (var c in name)
        {
            var valid =
                (c >= 'a' && c <= 'z')
                || (c >= 'A' && c <= 'Z')
                || (c >= '0' && c <= '9')
                || c == '_'
                || c == '-';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 1 つの item を検証し、出力の item を作ります。エラーがあれば報告して <see langword="null"/> を返します。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>出力の item。エラーがあれば <see langword="null"/>。</returns>
    private ITaskItem? ResolveItem(ITaskItem item)
    {
        // エラーがあっても他の項目を調べて、すべて報告する
        var logicalPath = ResolveLogicalPath(item);
        var fullPath = ResolveFullPath(item);
        var package = ResolvePackage(item);
        var compress = ResolveCompress(item);
        var obfuscate = ResolveObfuscate(item);
        if (
            logicalPath is null
            || fullPath is null
            || package is null
            || compress is null
            || obfuscate is null
        )
        {
            return null;
        }

        var output = new TaskItem(fullPath);
        output.SetMetadata("LogicalPath", logicalPath);
        output.SetMetadata("Package", package);
        output.SetMetadata("Compress", compress);
        output.SetMetadata("Obfuscate", obfuscate);
        output.SetMetadata("SourceItemSpec", item.ItemSpec);
        return output;
    }

    /// <summary>
    /// item の論理パスを決めて正規化します。<c>TargetPath</c> が無ければ ItemSpec を使います。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>正規化した論理パス。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolveLogicalPath(ITaskItem item)
    {
        var targetPath = item.GetMetadata("TargetPath");
        return targetPath.Length == 0
            ? ResolveDefaultLogicalPath(item)
            : ResolveExplicitLogicalPath(item, targetPath);
    }

    /// <summary>
    /// <c>TargetPath</c> が無いときの論理パス (ItemSpec) を正規化します。絶対パスと、プロジェクトの外を指すパスは使えません。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>正規化した論理パス。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolveDefaultLogicalPath(ITaskItem item)
    {
        var path = item.ItemSpec.Replace('\\', '/');
        if (Path.IsPathRooted(path) || HasParentSegment(path))
        {
            LogError(
                ErrorCodes.DefaultTargetPathUnavailable,
                $"'{item.ItemSpec}' の論理パスを Include から決められません。Include が絶対パスか、'..' を含んでいます。TargetPath メタデータで論理パスを指定してください。"
            );
            return null;
        }

        return NormalizeLogicalPath(item, path);
    }

    /// <summary>
    /// 明示された <c>TargetPath</c> を検査して正規化します。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <param name="targetPath">明示された <c>TargetPath</c>。</param>
    /// <returns>正規化した論理パス。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolveExplicitLogicalPath(ITaskItem item, string targetPath)
    {
        var path = targetPath.Replace('\\', '/');
        if (
            path.IndexOf(':') >= 0
            || path.StartsWith("//", StringComparison.Ordinal)
            || HasParentSegment(path)
        )
        {
            LogError(
                ErrorCodes.InvalidTargetPath,
                $"TargetPath '{targetPath}' は使えません。'..' を含むこと、ドライブ名 (':')、'//' で始まることはできません。"
            );
            return null;
        }

        return NormalizeLogicalPath(item, path);
    }

    /// <summary>
    /// 論理パスを <see cref="AssetPackagePath"/> で正規化し、UTF-8 で 65535 バイト以内かを確かめます。
    /// </summary>
    /// <param name="item">エラーを報告するための item。</param>
    /// <param name="path"><c>/</c> 区切りのパス。</param>
    /// <returns>正規化した論理パス。エラーがあれば <see langword="null"/>。</returns>
    private string? NormalizeLogicalPath(ITaskItem item, string path)
    {
        string normalized;
        try
        {
            normalized = AssetPackagePath.Normalize(path);
        }
        catch (ArgumentException ex)
        {
            LogError(
                ErrorCodes.InvalidTargetPath,
                $"'{item.ItemSpec}' の論理パスが正しくありません: {ex.Message}"
            );
            return null;
        }

        if (Encoding.UTF8.GetByteCount(normalized) > 65535)
        {
            LogError(
                ErrorCodes.InvalidTargetPath,
                $"論理パス '{normalized}' が長すぎます (UTF-8 で 65535 バイト以内にしてください)。"
            );
            return null;
        }

        return normalized;
    }

    /// <summary>
    /// item の実ファイルのフルパスを求め、存在するかを確かめます。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>実ファイルのフルパス。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolveFullPath(ITaskItem item)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(
                Path.Combine(ProjectDirectory, item.ItemSpec.Replace('\\', '/'))
            );
        }
        catch (Exception ex)
            when (ex is ArgumentException || ex is NotSupportedException || ex is IOException)
        {
            LogError(
                ErrorCodes.AssetFileNotFound,
                $"ファイルのパス '{item.ItemSpec}' が使えません: {ex.Message}"
            );
            return null;
        }

        if (!File.Exists(fullPath))
        {
            LogError(
                ErrorCodes.AssetFileNotFound,
                $"アセットのファイルが見つかりません: '{fullPath}'"
            );
            return null;
        }

        return fullPath;
    }

    /// <summary>
    /// <c>Package</c> を決めて検証します。省略時は <c>main</c>。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>パッケージ名。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolvePackage(ITaskItem item)
    {
        var package = item.GetMetadata("Package");
        if (package.Length == 0)
        {
            return "main";
        }

        if (!IsValidPackageName(package))
        {
            LogError(
                ErrorCodes.InvalidPackageName,
                $"Package '{package}' は使えません。英数字 (ASCII)・'_'・'-' だけを使ってください。"
            );
            return null;
        }

        return package;
    }

    /// <summary>
    /// <c>Compress</c> を検証して小文字にします。省略時は <c>auto</c>。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>正規化した値。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolveCompress(ITaskItem item)
    {
        var value = item.GetMetadata("Compress").Trim().ToLowerInvariant();
        if (value.Length == 0)
        {
            return "auto";
        }

        if (value is "auto" or "true" or "false")
        {
            return value;
        }

        LogError(
            ErrorCodes.InvalidCompress,
            $"Compress '{item.GetMetadata("Compress")}' は使えません。auto / true / false のどれかを指定してください。"
        );
        return null;
    }

    /// <summary>
    /// <c>Obfuscate</c> を検証して小文字にします。省略時は <c>false</c>。
    /// </summary>
    /// <param name="item">検証する item。</param>
    /// <returns>正規化した値。エラーがあれば <see langword="null"/>。</returns>
    private string? ResolveObfuscate(ITaskItem item)
    {
        var value = item.GetMetadata("Obfuscate").Trim().ToLowerInvariant();
        if (value.Length == 0)
        {
            return "false";
        }

        if (value is "true" or "false")
        {
            return value;
        }

        LogError(
            ErrorCodes.InvalidObfuscate,
            $"Obfuscate '{item.GetMetadata("Obfuscate")}' は使えません。true / false のどちらかを指定してください。"
        );
        return null;
    }

    /// <summary>
    /// エラーを報告します。メッセージは書式として扱われないように <c>{0}</c> の引数で渡します。
    /// </summary>
    /// <param name="code">エラーのコード (<see cref="ErrorCodes"/>)。</param>
    /// <param name="message">メッセージ。</param>
    private void LogError(string code, string message) =>
        Log.LogError(null, code, null, null, 0, 0, 0, 0, "{0}", message);
}
