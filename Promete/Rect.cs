namespace Promete;

/// <summary>
/// 位置とサイズからなる矩形を表します。
/// </summary>
public struct Rect
{
    /// <summary>
    /// この矩形の位置を取得または設定します。
    /// </summary>
    public Vector Location { get; set; }

    /// <summary>
    /// この矩形のサイズを取得または設定します。
    /// </summary>
    public Vector Size { get; set; }

    /// <summary>
    /// この矩形の左端の位置を取得または設定します。
    /// </summary>
    public float Left
    {
        get => Location.X;
        set => Location = new Vector(value, Top);
    }

    /// <summary>
    /// この矩形の上端の位置を取得または設定します。
    /// </summary>
    public float Top
    {
        get => Location.Y;
        set => Location = new Vector(Left, value);
    }

    /// <summary>
    /// この矩形の右端の位置を取得または設定します。
    /// </summary>
    public float Right
    {
        get => Left + Width - 1;
        set => Left = value - Width + 1;
    }

    /// <summary>
    /// この矩形の下端の位置を取得または設定します。
    /// </summary>
    public float Bottom
    {
        get => Top + Height - 1;
        set => Top = value - Height + 1;
    }

    /// <summary>
    /// この矩形の幅を取得または設定します。
    /// </summary>
    public float Width
    {
        get => Size.X;
        set => Size = new Vector(value, Height);
    }

    /// <summary>
    /// この矩形の高さを取得または設定します。
    /// </summary>
    public float Height
    {
        get => Size.Y;
        set => Size = new Vector(Width, value);
    }

    /// <summary>
    /// <see cref="Rect" /> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="location">位置。</param>
    /// <param name="size">サイズ。</param>
    public Rect(Vector location, Vector size)
    {
        Location = location;
        Size = size;
    }

    /// <summary>
    /// <see cref="Rect" /> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="left">左の位置。</param>
    /// <param name="top">上の位置。</param>
    /// <param name="width">幅。</param>
    /// <param name="height">高さ。</param>
    public Rect(float left, float top, float width, float height)
        : this(new Vector(left, top), new Vector(width, height))
    {
    }

    public void Deconstruct(out float x, out float y, out float width, out float height)
    {
        x = Left;
        y = Top;
        width = Width;
        height = Height;
    }

    public void Deconstruct(out Vector location, out Vector size)
    {
        location = Location;
        size = Size;
    }

    /// <summary>
    /// この矩形と指定された矩形が重なっているかどうかを判定します。
    /// </summary>
    /// <param name="rect">判定する矩形。</param>
    /// <returns>重なっている場合は <see langword="true" />、それ以外の場合は <see langword="false" />。</returns>
    public bool Intersect(Rect rect)
    {
        return Left < rect.Right && rect.Left < Right && Top < rect.Bottom && rect.Top < Bottom;
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
    /// <see cref="Rect" /> を、明示的に <see cref="RectInt" /> に変換します。
    /// </summary>
    public static explicit operator RectInt(Rect rect)
    {
        return new RectInt((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
    }

    /// <summary>
    /// タプルから <see cref="Rect" /> に変換します。
    /// </summary>
    public static implicit operator Rect((float left, float top, float width, float height) tuple)
    {
        return new Rect(tuple.left, tuple.top, tuple.width, tuple.height);
    }

    /// <summary>
    /// タプルから <see cref="Rect" /> に変換します。
    /// </summary>
    public static implicit operator Rect((Vector location, Vector size) tuple)
    {
        return new Rect(tuple.location, tuple.size);
    }
}
