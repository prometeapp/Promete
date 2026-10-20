namespace Promete.Test.AssetsApi;

/// <summary>
/// テストの間だけ存在する一時ディレクトリ。Dispose で中身ごと削除する。
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "promete-assets-test-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>
    /// 一時ディレクトリからの相対パス (/ 区切り) にファイルを作る。親ディレクトリも作る。
    /// </summary>
    /// <returns>作ったファイルのフルパス。</returns>
    public string CreateFile(string relativePath, string content = "")
    {
        var fullPath = System.IO.Path.Combine(
            Path,
            relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar)
        );
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
            // 削除できなくてもテストの結果には影響しない
        }
        catch (UnauthorizedAccessException)
        {
            // 同上
        }
    }
}
