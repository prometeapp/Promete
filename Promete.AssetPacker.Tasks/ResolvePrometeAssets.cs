using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
        throw new NotImplementedException();
    }
}
