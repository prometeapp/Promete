using Promete.Graphics;

namespace Promete.Test.Fakes;

/// <summary>
/// テスト用に、テクスチャの内容をメモリ上へ保持するテクスチャファクトリです。
/// </summary>
public class FakeTextureFactory : TextureFactoryBase
{
    private readonly Dictionary<int, byte[]> _textures = new();
    private readonly Dictionary<int, VectorInt> _sizes = new();
    private int _nextHandle = 1;

    /// <summary>
    /// 破棄されたテクスチャのハンドルを取得します。
    /// </summary>
    public List<int> DisposedHandles { get; } = [];

    /// <summary>
    /// 生成されたテクスチャの数を取得します。
    /// </summary>
    public int CreatedCount => _nextHandle - 1;

    /// <summary>
    /// 指定したテクスチャのサイズを取得します。
    /// </summary>
    public VectorInt GetSize(int handle) => _sizes[handle];

    /// <summary>
    /// 指定したテクスチャの指定位置におけるアルファ値を取得します。
    /// </summary>
    public byte GetAlphaAt(int handle, VectorInt size, VectorInt position)
    {
        return _textures[handle][(((position.Y * size.X) + position.X) * 4) + 3];
    }

    protected override int UploadTexture(ReadOnlySpan<byte> rgba, VectorInt size)
    {
        var handle = _nextHandle++;
        var buffer = new byte[size.X * size.Y * 4];
        rgba[..Math.Min(rgba.Length, buffer.Length)].CopyTo(buffer);
        _textures[handle] = buffer;
        _sizes[handle] = size;
        return handle;
    }

    protected override void UpdateTexture(
        int handle,
        VectorInt offset,
        VectorInt size,
        ReadOnlySpan<byte> rgba
    )
    {
        var destination = _textures[handle];
        var stride = _sizes[handle].X * 4;

        for (var y = 0; y < size.Y; y++)
        {
            rgba.Slice(y * size.X * 4, size.X * 4)
                .CopyTo(destination.AsSpan(((offset.Y + y) * stride) + (offset.X * 4)));
        }
    }

    protected override void DestroyTexture(int handle)
    {
        DisposedHandles.Add(handle);
    }
}
