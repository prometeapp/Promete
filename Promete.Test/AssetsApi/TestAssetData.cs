using System.Text;
using Promete.Graphics.Imaging;

namespace Promete.Test.AssetsApi;

/// <summary>
/// テストで読み込ませるファイルのデータを作るヘルパー。
/// </summary>
internal static class TestAssetData
{
    /// <summary>
    /// 指定したサイズの PNG を作る。<paramref name="seed"/> を変えると画素が変わる。
    /// </summary>
    public static byte[] Png(int width = 4, int height = 4, byte seed = 0)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i + seed);
        using var stream = new MemoryStream();
        PngEncoder.Encode(new RgbaImage(width, height, pixels), stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 16 bit PCM の WAVE を作る。
    /// </summary>
    public static byte[] Wav(int channels = 1, int sampleRate = 8000, int frames = 16)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
        {
            var dataSize = frames * channels * 2;
            writer.Write("RIFF"u8);
            writer.Write(36 + dataSize);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(dataSize);
            for (var i = 0; i < frames * channels; i++)
                writer.Write((short)(i * 100));
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Promete.Test/assets のファイル (出力先にコピーされている) を読む。
    /// </summary>
    public static byte[] File(string name) =>
        System.IO.File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "assets", name));

    /// <summary>
    /// UTF-8 のテキスト。
    /// </summary>
    public static byte[] Text(string text) => Encoding.UTF8.GetBytes(text);
}

/// <summary>
/// Dispose された回数を記録する、汎用の読み込み (<c>AssetManager.Load</c>) のテスト用の値。
/// </summary>
internal sealed class TrackedDisposable(string content) : IDisposable
{
    private int _disposeCount;

    public string Content { get; } = content;

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool IsDisposed => DisposeCount > 0;

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}
