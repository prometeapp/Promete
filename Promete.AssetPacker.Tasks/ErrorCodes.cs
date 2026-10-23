namespace Promete.AssetPacker.Tasks;

/// <summary>
/// Task が報告するエラーのコード。targets が直接出す警告・エラー (<c>PROMETE1010</c>〜<c>PROMETE1015</c>) は <c>Promete.targets</c> を参照してください。
/// </summary>
internal static class ErrorCodes
{
    /// <summary>
    /// <c>TargetPath</c> の既定値 (Include に書いたパス) が使えない。Include が絶対パスか、<c>..</c> を含む。
    /// </summary>
    public const string DefaultTargetPathUnavailable = "PROMETE1001";

    /// <summary>
    /// 明示した <c>TargetPath</c> が正しくない (<c>..</c> やドライブ名を含む、空になる、長すぎる)。
    /// </summary>
    public const string InvalidTargetPath = "PROMETE1002";

    /// <summary>
    /// 正規化すると同じ論理パスになる item が複数ある。
    /// </summary>
    public const string DuplicateLogicalPath = "PROMETE1003";

    /// <summary>
    /// <c>Compress</c> の値が <c>auto</c> / <c>true</c> / <c>false</c> のどれでもない。
    /// </summary>
    public const string InvalidCompress = "PROMETE1004";

    /// <summary>
    /// <c>Package</c> の名前が正しくない (英数字・<c>_</c>・<c>-</c> 以外を含む、または空)。
    /// </summary>
    public const string InvalidPackageName = "PROMETE1005";

    /// <summary>
    /// <c>Obfuscate</c> の値が <c>true</c> / <c>false</c> のどちらでもない。
    /// </summary>
    public const string InvalidObfuscate = "PROMETE1006";

    /// <summary>
    /// item のファイルが見つからない。
    /// </summary>
    public const string AssetFileNotFound = "PROMETE1007";

    /// <summary>
    /// 大文字・小文字だけが違うパッケージ名が混ざっている (例: <c>Main</c> と <c>main</c>)。MSBuild の重複除去と書き出しが大文字・小文字を区別しないため、アセットが黙って失われる。
    /// </summary>
    public const string PackageNameCaseConflict = "PROMETE1008";

    /// <summary>
    /// パッケージの書き出しに失敗した。
    /// </summary>
    public const string PackFailed = "PROMETE1020";

    /// <summary>
    /// マニフェストの書き出しに失敗した。
    /// </summary>
    public const string ManifestWriteFailed = "PROMETE1021";
}
