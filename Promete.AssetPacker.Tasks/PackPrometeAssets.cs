using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
        throw new NotImplementedException();
    }
}
