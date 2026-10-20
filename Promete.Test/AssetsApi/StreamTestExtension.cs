using System.Text;

namespace Promete.Test.AssetsApi;

/// <summary>
/// テスト用のストリームのヘルパー。
/// </summary>
internal static class StreamTestExtension
{
    /// <summary>
    /// 現在位置から最後までを UTF-8 の文字列として読む。
    /// </summary>
    public static string ReadAllText(this Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
        return reader.ReadToEnd();
    }
}
