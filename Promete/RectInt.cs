namespace Promete;

/// <summary>
/// 位置とサイズからなる矩形を表します。構成する数値が整数なバージョン。
/// </summary>
public struct RectInt
{
    /// <summary>
    /// この矩形の位置を取得または設定します。
    /// </summary>
    public VectorInt Location { get; set; }

    /// <summary>
    /// この矩形のサイズを取得または設定します。
    /// </summary>
    public VectorInt Size { get; set; }

    /// <summary>
    /// この矩形の左端の位置を取得または設定します。
    /// </summary>
    public int Left
    {
        get => Location.X;
        set => Location = new VectorInt(value, Top);
    }

    /// <summary>
    /// この矩形の上端の位置を取得または設定します。
    /// </summary>
    public int Top
    {
        get => Location.Y;
        set => Location = new VectorInt(Left, value);
    }

    /// <summary>
    /// この矩形の右端の位置を取得または設定します。
    /// </summary>
    public int Right
    {
        get => Left + Width - 1;
        set => Left = value - Width + 1;
    }

    /// <summary>
    /// この矩形の下端の位置を取得または設定します。
    /// </summary>
    public int Bottom
    {
        get => Top + Height - 1;
        set => Top = value - Height + 1;
    }

    /// <summary>
    /// この矩形の幅を取得または設定します。
    /// </summary>
    public int Width
    {
        get => Size.X;
        set => Size = new VectorInt(value, Height);
    }

    /// <summary>
    /// この矩形の高さを取得または設定します。
    /// </summary>
    public int Height
    {
        get => Size.Y;
        set => Size = new VectorInt(Width, value);
    }

    /// <summary>
    /// <see cref="Rect" /> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="location"></param>
    /// <param name="size"></param>
    public RectInt(VectorInt location, VectorInt size)
    {
        Location = location;
        Size = size;
    }

    /// <summary>
    /// <see cref="Rect" /> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public RectInt(int left, int top, int width, int height)
        : this(new VectorInt(left, top), new VectorInt(width, height))
    {
    }


    /// <summary>
    /// この矩形と指定された矩形が重なっているかどうかを判定します。
    /// </summary>
    /// <param name="rect">判定する矩形。</param>
    /// <returns>重なっている場合は <see langword="true" />、それ以外の場合は <see langword="false" />。</returns>
    public bool Intersect(RectInt rect)
    {
        return Left < rect.Right && Right > rect.Left && Top < rect.Bottom && Bottom > rect.Top;
    }

    /// <summary>
    /// この矩形を指定されたオフセットで平行移動します。
    /// </summary>
    /// <param name="offset">平行移動するオフセット。</param>
    /// <returns>平行移動後の新しい <see cref="Rect" />。</returns>
    public Rect Translate(Vector offset)
    {
        return new Rect(Location + offset, Size);
    }

    /// <summary>
    /// <see cref="RectInt" /> を、<see cref="Rect" /> に変換します。
    /// </summary>
    public static implicit operator Rect(RectInt rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    /// <summary>
    /// タプルから <see cref="Rect" /> に変換します。
    /// </summary>
    public static implicit operator RectInt((int left, int top, int width, int height) tuple)
    {
        return new RectInt(tuple.left, tuple.top, tuple.width, tuple.height);
    }

    /// <summary>
    /// タプルから <see cref="Rect" /> に変換します。
    /// </summary>
    public static implicit operator RectInt((VectorInt location, VectorInt size) tuple)
    {
        return new RectInt(tuple.location, tuple.size);
    }
}
