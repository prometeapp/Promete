namespace Promete.Audio;

/// <summary>
/// オーディオソースの仕様を定義するインターフェース。Promete APIで扱えるオーディオソースを表します。
/// </summary>
public interface IAudioSource
{
    /// <summary>
    /// このオーディオソースのサンプル数を取得します。未指定の場合は <c>null</c> を返します。
    /// </summary>
    public int? Samples { get; }

    /// <summary>
    /// チャンネル数を取得します。
    /// </summary>
    public int Channels { get; }

    /// <summary>
    /// 量子化ビット数を取得します。
    /// </summary>
    public int Bits { get; }

    /// <summary>
    /// サンプリング周波数を取得します。
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// サンプルデータを配列に読み込みます。
    /// </summary>
    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset);
}
