using System.Linq;

namespace Promete.Windowing;

/// <summary>
/// Arguments for file-dropped-event.
/// </summary>
public struct FileDroppedEventArgs(string[] pathes)
{
    /// <summary>
    /// Get pathes of dropped files.
    /// </summary>
    public string[] Pathes { get; set; } = pathes;

    /// <summary>
    /// Get path of a dropped file.
    /// </summary>
    public string Path => Pathes.First();
}
