using System.Drawing;
using Promete.Graphics;
using SixLabors.ImageSharp;
using Color = System.Drawing.Color;

namespace Promete.Test.Fakes;

/// <summary>
/// テスト用に、テクスチャの内容をメモリ上へ保持するテクスチャファクトリです。
/// </summary>
public class FakeTextureFactory : TextureFactoryBase
{
    private readonly Dictionary<int, byte[]> _textures = new();
    private int _nextHandle = 1;

    /// <summary>
    /// 破棄されたテクスチャのハンドルを取得します。
    /// </summary>
    public List<int> DisposedHandles { get; } = [];

    /// <summary>
    /// 生成されたテクスチャの数を取得します。
    /// </summary>
    public int CreatedCount => _nextHandle - 1;

    public override Texture2D Load(string path) => throw new NotSupportedException();

    public override Texture2D Load(Stream stream) => throw new NotSupportedException();

    public override Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    ) => throw new NotSupportedException();

    public override Texture2D[] LoadSpriteSheet(
        Stream stream,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    ) => throw new NotSupportedException();

    public override Texture2D Create(byte[] bitmap, VectorInt size)
    {
        var handle = _nextHandle++;
        var buffer = new byte[size.X * size.Y * 4];
        Array.Copy(bitmap, buffer, Math.Min(bitmap.Length, buffer.Length));
        _textures[handle] = buffer;
        return new Texture2D(handle, size, t => DisposedHandles.Add(t.Handle));
    }

    public override Texture2D Create(byte[,,] bitmap) => throw new NotSupportedException();

    public override Texture2D CreateSolid(Color color, VectorInt size) =>
        throw new NotSupportedException();

    public override void Update(Texture2D texture, VectorInt offset, VectorInt size, byte[] bitmap)
    {
        var destination = _textures[texture.Handle];
        var stride = texture.Size.X * 4;

        for (var y = 0; y < size.Y; y++)
        {
            Array.Copy(
                bitmap,
                y * size.X * 4,
                destination,
                ((offset.Y + y) * stride) + (offset.X * 4),
                size.X * 4
            );
        }
    }

    /// <summary>
    /// 指定したテクスチャの指定位置におけるアルファ値を取得します。
    /// </summary>
    public byte GetAlphaAt(int handle, VectorInt size, VectorInt position)
    {
        return _textures[handle][(((position.Y * size.X) + position.X) * 4) + 3];
    }

    internal override Texture2D LoadFromImageSharpImage(Image image) =>
        throw new NotSupportedException();
}
