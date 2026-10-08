using System;
using System.IO;
using System.Linq;
using Promete.Graphics.Imaging;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace Promete.Graphics;

/// <summary>
/// テクスチャを生成するファクトリです。
/// </summary>
/// <remarks>
/// バックエンドは <see cref="UploadTexture"/>、<see cref="UpdateTexture"/>、<see cref="DestroyTexture"/> の 3 つを実装します。
/// 画像の読み込みや切り抜きなどの処理は、この基底クラスが提供します。
/// 必要に応じて、各 virtual メソッドをオーバーライドして最適化することもできます。
/// その場合、引数の検証などの基底クラスの処理は、オーバーライドした側の責任になります。
/// <see cref="TextureOptions"/> を受け取るオーバーロードが実処理を担い、受け取らないオーバーロードは既定の設定でそちらを呼びます。
/// </remarks>
public abstract class TextureFactoryBase
{
    private readonly Action<Texture2D> _destroy;

    /// <summary>
    /// <see cref="TextureFactoryBase"/> の新しいインスタンスを初期化します。
    /// </summary>
    protected TextureFactoryBase()
    {
        _destroy = texture => DestroyTexture(texture.Handle);
    }

    /// <summary>
    /// 指定したパスからテクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public virtual Texture2D Load(string path)
    {
        return Load(path, TextureOptions.Default);
    }

    /// <summary>
    /// 指定したパスからテクスチャを読み込みます。
    /// </summary>
    /// <param name="path">画像ファイルのパス。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D Load(string path, TextureOptions options)
    {
        return LoadFromImage(ImageDecoder.Decode(path), options);
    }

    /// <summary>
    /// 指定したストリームからテクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public virtual Texture2D Load(Stream stream)
    {
        return Load(stream, TextureOptions.Default);
    }

    /// <summary>
    /// 指定したストリームからテクスチャを読み込みます。
    /// </summary>
    /// <param name="stream">画像データのストリーム。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D Load(Stream stream, TextureOptions options)
    {
        return LoadFromImage(ImageDecoder.Decode(stream), options);
    }

    /// <summary>
    /// 指定したパスからテクスチャを読み込み、切り抜きます。
    /// </summary>
    /// <remarks>
    /// 返されるテクスチャは、すべて 1 枚のテクスチャを共有します。
    /// 共有されたテクスチャは、返された全要素を <see cref="Texture2D.Dispose"/> した時点で破棄されます。
    /// 同じ要素を複数回 <see cref="Texture2D.Dispose"/> しないでください。
    /// </remarks>
    /// <returns></returns>
    public virtual Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        return LoadSpriteSheet(path, horizontalCount, verticalCount, size, TextureOptions.Default);
    }

    /// <summary>
    /// 指定したパスからテクスチャを読み込み、切り抜きます。
    /// </summary>
    /// <remarks><inheritdoc cref="LoadSpriteSheet(string, int, int, VectorInt)"/></remarks>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        TextureOptions options
    )
    {
        return LoadSpriteSheet(
            ImageDecoder.Decode(path),
            horizontalCount,
            verticalCount,
            size,
            options
        );
    }

    /// <summary>
    /// 指定したストリームからテクスチャを読み込み、切り抜きます。
    /// </summary>
    /// <remarks><inheritdoc cref="LoadSpriteSheet(string, int, int, VectorInt)"/></remarks>
    /// <returns></returns>
    public virtual Texture2D[] LoadSpriteSheet(
        Stream stream,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        return LoadSpriteSheet(
            stream,
            horizontalCount,
            verticalCount,
            size,
            TextureOptions.Default
        );
    }

    /// <summary>
    /// 指定したストリームからテクスチャを読み込み、切り抜きます。
    /// </summary>
    /// <remarks><inheritdoc cref="LoadSpriteSheet(string, int, int, VectorInt)"/></remarks>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D[] LoadSpriteSheet(
        Stream stream,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        TextureOptions options
    )
    {
        return LoadSpriteSheet(
            ImageDecoder.Decode(stream),
            horizontalCount,
            verticalCount,
            size,
            options
        );
    }

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    /// <param name="bitmap">RGBA8888 形式のビットマップデータ。</param>
    /// <param name="size">テクスチャのサイズ。</param>
    /// <returns></returns>
    public virtual Texture2D Create(byte[] bitmap, VectorInt size)
    {
        return Create(bitmap, size, TextureOptions.Default);
    }

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    /// <param name="bitmap">RGBA8888 形式のビットマップデータ。</param>
    /// <param name="size">テクスチャのサイズ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D Create(byte[] bitmap, VectorInt size, TextureOptions options)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        if (bitmap.Length < size.X * size.Y * 4)
            throw new ArgumentException("ビットマップのデータがサイズに対して不足しています。", nameof(bitmap));

        var handle = UploadTexture(new TextureUploadRequest(bitmap, size, options));
        return new Texture2D(handle, size, _destroy);
    }

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    /// <param name="bitmap">[x, y, チャンネル(RGBA)] の順で並んだビットマップデータ。</param>
    /// <returns></returns>
    public virtual Texture2D Create(byte[,,] bitmap)
    {
        return Create(bitmap, TextureOptions.Default);
    }

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    /// <param name="bitmap">[x, y, チャンネル(RGBA)] の順で並んだビットマップデータ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D Create(byte[,,] bitmap, TextureOptions options)
    {
        var width = bitmap.GetLength(0);
        var height = bitmap.GetLength(1);
        var arr = new byte[width * height * 4];
        for (int y = 0, i = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        for (var j = 0; j < 4; j++)
            arr[i++] = bitmap[x, y, j];

        return Create(arr, (width, height), options);
    }

    /// <summary>
    /// 指定した色の単色テクスチャを生成します。
    /// </summary>
    /// <returns></returns>
    public virtual Texture2D CreateSolid(Color color, VectorInt size)
    {
        return CreateSolid(color, size, TextureOptions.Default);
    }

    /// <summary>
    /// 指定した色の単色テクスチャを生成します。
    /// </summary>
    /// <param name="color">塗りつぶす色。</param>
    /// <param name="size">テクスチャのサイズ。</param>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture2D CreateSolid(Color color, VectorInt size, TextureOptions options)
    {
        if (size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        var arr = new byte[size.X * size.Y * 4];
        for (var i = 0; i < arr.Length; i += 4)
        {
            arr[i + 0] = color.R;
            arr[i + 1] = color.G;
            arr[i + 2] = color.B;
            arr[i + 3] = color.A;
        }

        return Create(arr, size, options);
    }

    /// <summary>
    /// 既存のテクスチャの一部の領域を、指定したビットマップで書き換えます。
    /// グリフアトラスのように、1 枚のテクスチャへ内容を追記していく用途で使用します。
    /// </summary>
    /// <param name="texture">書き換え対象のテクスチャ。</param>
    /// <param name="offset">書き換える領域の左上位置。</param>
    /// <param name="size">書き換える領域のサイズ。</param>
    /// <param name="bitmap">RGBA8888 形式のビットマップデータ。</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="texture"/> が <see cref="Texture2D.IsSubTexture"/> である場合。
    /// </exception>
    public virtual void Update(Texture2D texture, VectorInt offset, VectorInt size, byte[] bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (texture.IsSubTexture)
            throw new InvalidOperationException("テクスチャの一部の領域を指すテクスチャは、書き換えできません。");
        if (size.X <= 0 || size.Y <= 0)
            return;

        if (
            offset.X < 0
            || offset.Y < 0
            || offset.X + size.X > texture.Size.X
            || offset.Y + size.Y > texture.Size.Y
        )
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (bitmap.Length < size.X * size.Y * 4)
            throw new ArgumentException("ビットマップのデータがサイズに対して不足しています。", nameof(bitmap));

        UpdateTexture(texture.Handle, offset, size, bitmap);
    }

    /// <summary>
    /// 指定したパスから 9 スライステクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public virtual Texture9Sliced Load9Sliced(string path, int left, int top, int right, int bottom)
    {
        return Load9Sliced(path, left, top, right, bottom, TextureOptions.Default);
    }

    /// <summary>
    /// 指定したパスから 9 スライステクスチャを読み込みます。
    /// </summary>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture9Sliced Load9Sliced(
        string path,
        int left,
        int top,
        int right,
        int bottom,
        TextureOptions options
    )
    {
        return Load9Sliced(ImageDecoder.Decode(path), left, top, right, bottom, options);
    }

    /// <summary>
    /// 指定したストリームから 9 スライステクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public virtual Texture9Sliced Load9Sliced(
        Stream stream,
        int left,
        int top,
        int right,
        int bottom
    )
    {
        return Load9Sliced(stream, left, top, right, bottom, TextureOptions.Default);
    }

    /// <summary>
    /// 指定したストリームから 9 スライステクスチャを読み込みます。
    /// </summary>
    /// <param name="options">サンプリングの設定。</param>
    /// <returns></returns>
    public virtual Texture9Sliced Load9Sliced(
        Stream stream,
        int left,
        int top,
        int right,
        int bottom,
        TextureOptions options
    )
    {
        return Load9Sliced(ImageDecoder.Decode(stream), left, top, right, bottom, options);
    }

    /// <summary>
    /// デコード済みの画像からテクスチャを生成します。
    /// </summary>
    internal Texture2D LoadFromImage(RgbaImage image, TextureOptions options = default)
    {
        return Create(image.Pixels, (image.Width, image.Height), options);
    }

    /// <summary>
    /// ビットマップを GPU へ転送し、新しいテクスチャのハンドルを返します。
    /// </summary>
    /// <remarks>
    /// 引数の検証は呼び出し側で済んでいます。
    /// 呼び出されるスレッドは規定しません。スレッドに制約のあるバックエンドは、実装側で保証してください。
    /// </remarks>
    /// <param name="request">転送要求。</param>
    /// <returns>バックエンドのテクスチャハンドル。</returns>
    protected abstract int UploadTexture(in TextureUploadRequest request);

    /// <summary>
    /// GPU 上のテクスチャの一部の領域を書き換えます。
    /// </summary>
    /// <remarks>
    /// 領域がテクスチャの範囲内にあること、<paramref name="rgba"/> が十分な長さであることは、呼び出し側で検証済みです。
    /// </remarks>
    /// <param name="handle">書き換え対象のテクスチャハンドル。</param>
    /// <param name="offset">書き換える領域の左上位置。</param>
    /// <param name="size">書き換える領域のサイズ。</param>
    /// <param name="rgba">RGBA8888 形式のビットマップデータ。</param>
    protected abstract void UpdateTexture(
        int handle,
        VectorInt offset,
        VectorInt size,
        ReadOnlySpan<byte> rgba
    );

    /// <summary>
    /// GPU 上のテクスチャを破棄します。
    /// </summary>
    /// <remarks>
    /// 同じハンドルに対して複数回呼ばれることはありません。
    /// </remarks>
    /// <param name="handle">破棄するテクスチャハンドル。</param>
    protected abstract void DestroyTexture(int handle);

    private Texture2D[] LoadSpriteSheet(
        RgbaImage bmp,
        int horizontalCount,
        int verticalCount,
        VectorInt size,
        TextureOptions options
    )
    {
        if (horizontalCount * size.X > bmp.Width)
            throw new ArgumentException(null, nameof(horizontalCount));
        if (verticalCount * size.Y > bmp.Height)
            throw new ArgumentException(null, nameof(verticalCount));

        var count = verticalCount * horizontalCount;
        if (count <= 0)
            return [];

        var width = (float)bmp.Width;
        var height = (float)bmp.Height;
        var handle = UploadTexture(
            new TextureUploadRequest(bmp.Pixels, (bmp.Width, bmp.Height), options)
        );

        // 全セルが 1 枚のテクスチャを共有するので、最後の 1 つが破棄された時点で解放する
        var remaining = count;
        void Release(Texture2D _)
        {
            if (--remaining == 0)
                DestroyTexture(handle);
        }

        var textures = new Texture2D[count];
        for (var y = 0; y < verticalCount; y++)
        {
            for (var x = 0; x < horizontalCount; x++)
            {
                var px = x * size.X;
                var py = y * size.Y;

                var uvStart = new Vector(px / width, py / height);
                var uvEnd = new Vector((px + size.X) / width, (py + size.Y) / height);

                textures[(y * horizontalCount) + x] = new Texture2D(
                    handle,
                    size,
                    Release,
                    uvStart,
                    uvEnd
                );
            }
        }

        return textures;
    }

    private Texture9Sliced Load9Sliced(
        RgbaImage img,
        int left,
        int top,
        int right,
        int bottom,
        TextureOptions options
    )
    {
        var size = (img.Width, img.Height);

        if (left > img.Width)
            throw new ArgumentException(null, nameof(left));
        if (top > img.Height)
            throw new ArgumentException(null, nameof(top));
        if (right > img.Width - left)
            throw new ArgumentException(null, nameof(right));
        if (bottom > img.Height - top)
            throw new ArgumentException(null, nameof(bottom));

        var atlas = new Rectangle[]
        {
            new(0, 0, left, top),
            new(left, 0, img.Width - left - right, top),
            new(img.Width - right, 0, right, top),
            new(0, top, left, img.Height - top - bottom),
            new(left, top, img.Width - left - right, img.Height - top - bottom),
            new(img.Width - right, top, right, img.Height - top - bottom),
            new(0, img.Height - bottom, left, bottom),
            new(left, img.Height - bottom, img.Width - left - right, bottom),
            new(img.Width - right, img.Height - bottom, right, bottom),
        };

        var texture = atlas
            .Select(rect =>
                LoadFromImage(img.Crop(rect.X, rect.Y, rect.Width, rect.Height), options)
            )
            .ToArray();

        return new Texture9Sliced(texture, size);
    }
}
