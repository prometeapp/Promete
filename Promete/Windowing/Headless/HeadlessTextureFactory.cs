using System.IO;
using Promete.Graphics;
using SixLabors.ImageSharp;
using Color = System.Drawing.Color;

namespace Promete.Windowing.Headless;

public class HeadlessTextureFactory : TextureFactory
{
    public override Texture2D Load(string path)
    {
        return default;
    }

    public override Texture2D Load(Stream stream)
    {
        return default;
    }

    public override Texture2D[] LoadSpriteSheet(string path, int horizontalCount, int verticalCount, VectorInt size)
    {
        return new Texture2D[horizontalCount * verticalCount];
    }

    public override Texture2D[] LoadSpriteSheet(Stream stream, int horizontalCount, int verticalCount, VectorInt size)
    {
        return new Texture2D[horizontalCount * verticalCount];
    }

    public override Texture2D Create(byte[] bitmap, VectorInt size)
    {
        return default;
    }

    public override Texture2D Create(byte[,,] bitmap)
    {
        return default;
    }

    public override Texture2D CreateSolid(Color color, VectorInt size)
    {
        return default;
    }

    internal override Texture2D LoadFromImageSharpImage(Image image)
    {
        return default;
    }
}
