using System.Linq;

namespace Promete.Windowing;

/// <summary>
/// ファイルがドロップされた際のイベント引数を表します。
/// </summary>
public struct FileDroppedEventArgs(string[] pathes)
{
    /// <summary>
    /// ドロップされたファイルのパスを取得します。
    /// </summary>
    public string[] Pathes { get; set; } = pathes;

    /// <summary>
    /// ドロップされた最初のファイルのパスを取得します。
    /// </summary>
    public string Path => Pathes.First();
}
