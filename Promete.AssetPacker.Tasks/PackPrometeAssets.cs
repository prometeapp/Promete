using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Promete.AssetPacker;

namespace Promete.AssetPacker.Tasks;

/// <summary>
/// <see cref="ResolvePrometeAssets"/> が出力した item を、<c>Package</c> メタデータごとにアセットパッケージへ書き出します。
/// </summary>
/// <remarks>
/// <para>
/// パッケージのファイル名は <c>&lt;FileNamePrefix&gt;&lt;Package&gt;&lt;FileExtension&gt;</c> です (例: <c>MyGame.main.prmtpak</c>)。
/// 呼ばれたら、すべてのパッケージを書き直します (作り直すかどうかの判断は、targets の Inputs / Outputs が行います)。
/// </para>
/// <para>
/// <c>Compress</c> は <c>auto</c> → <c>AssetCompression.Auto</c>、<c>true</c> → <c>Always</c>、<c>false</c> → <c>Never</c> に対応させます。
/// <c>Obfuscate</c> は Phase 4 では無視します。
/// </para>
/// </remarks>
public sealed class PackPrometeAssets : Task
{
    /// <summary>
    /// <see cref="ResolvePrometeAssets"/> が出力した item。
    /// </summary>
    [Required]
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>
    /// パッケージを書き出すディレクトリ。無ければ作ります。
    /// </summary>
    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// パッケージのファイル名の接頭辞 (<c>$(AssemblyName).</c>)。
    /// </summary>
    [Required]
    public string FileNamePrefix { get; set; } = string.Empty;

    /// <summary>
    /// パッケージのファイル名の拡張子 (<c>.prmtpak</c>)。
    /// </summary>
    [Required]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// 書き出したパッケージ。ItemSpec が絶対パスで、<c>PackageName</c> と <c>Location</c> (ディレクトリを除いたファイル名) を持ちます。パッケージ名の序数順です。
    /// </summary>
    [Output]
    public ITaskItem[] Packages { get; private set; } = [];

    /// <inheritdoc />
    public override bool Execute()
    {
        Packages = [];
        var groups = new SortedDictionary<string, List<ITaskItem>>(StringComparer.Ordinal);
        foreach (var item in Assets)
        {
            var package = item.GetMetadata("Package");
            if (!groups.TryGetValue(package, out var group))
            {
                group = new List<ITaskItem>();
                groups.Add(package, group);
            }

            group.Add(item);
        }

        var succeeded = true;
        var packages = new List<ITaskItem>();
        var outputDirectory = Path.GetFullPath(OutputDirectory);
        foreach (var pair in groups)
        {
            var fileName = FileNamePrefix + pair.Key + FileExtension;
            var outputPath = Path.Combine(outputDirectory, fileName);
            try
            {
                WritePackage(pair.Value, outputDirectory, outputPath);
            }
            catch (Exception ex) when (IsPackFailure(ex))
            {
                LogError(
                    ErrorCodes.PackFailed,
                    $"パッケージを書き出せません '{outputPath}': {ex.Message}"
                );
                succeeded = false;
                continue;
            }

            var output = new TaskItem(outputPath);
            output.SetMetadata("PackageName", pair.Key);
            output.SetMetadata("Location", fileName);
            packages.Add(output);
        }

        Packages = packages.ToArray();
        return succeeded;
    }

    /// <summary>
    /// 1 つのパッケージを書き出します。ディレクトリが無ければ作ります。
    /// </summary>
    /// <param name="items">パッケージに入れる item。</param>
    /// <param name="outputDirectory">書き出すディレクトリの絶対パス。</param>
    /// <param name="outputPath">書き出すファイルの絶対パス。</param>
    private static void WritePackage(
        List<ITaskItem> items,
        string outputDirectory,
        string outputPath
    )
    {
        var writer = new AssetPackageWriter();
        foreach (var item in items)
        {
            writer.AddFile(
                item.GetMetadata("LogicalPath"),
                item.ItemSpec,
                ToCompression(item.GetMetadata("Compress"))
            );
        }

        Directory.CreateDirectory(outputDirectory);
        writer.WriteTo(outputPath);
    }

    /// <summary>
    /// <c>Compress</c> メタデータの値を書き出しの圧縮の方法に変えます。
    /// </summary>
    /// <param name="compress"><c>auto</c> / <c>true</c> / <c>false</c>。</param>
    /// <returns>対応する圧縮の方法。</returns>
    private static AssetCompression ToCompression(string compress) =>
        compress switch
        {
            "true" => AssetCompression.Always,
            "false" => AssetCompression.Never,
            _ => AssetCompression.Auto,
        };

    /// <summary>
    /// 書き出しで起きた例外が、報告して続ける種類の失敗かを取得します。
    /// </summary>
    /// <param name="ex">発生した例外。</param>
    /// <returns>報告して続ける失敗なら <see langword="true"/>。</returns>
    private static bool IsPackFailure(Exception ex) =>
        ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException;

    /// <summary>
    /// エラーを報告します。メッセージは書式として扱われないように <c>{0}</c> の引数で渡します。
    /// </summary>
    /// <param name="code">エラーのコード。</param>
    /// <param name="message">メッセージ。</param>
    private void LogError(string code, string message) =>
        Log.LogError(null, code, null, null, 0, 0, 0, 0, "{0}", message);
}
