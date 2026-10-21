using System;
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
        throw new NotImplementedException();
    }
}
