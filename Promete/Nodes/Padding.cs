namespace Promete.Nodes;

/// <summary>
/// 上下左右の余白を表します。
/// </summary>
/// <param name="Left">左の余白。</param>
/// <param name="Top">上の余白。</param>
/// <param name="Right">右の余白。</param>
/// <param name="Bottom">下の余白。</param>
public readonly record struct Padding(int Left, int Top, int Right, int Bottom)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Padding"/> struct.
    /// 上下左右に同じ余白を持つ <see cref="Padding"/> を作成します。
    /// </summary>
    /// <param name="all">上下左右の余白。</param>
    public Padding(int all)
        : this(all, all, all, all) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Padding"/> struct.
    /// 左右と上下で余白を指定して <see cref="Padding"/> を作成します。
    /// </summary>
    /// <param name="horizontal">左右の余白。</param>
    /// <param name="vertical">上下の余白。</param>
    public Padding(int horizontal, int vertical)
        : this(horizontal, vertical, horizontal, vertical) { }

    /// <summary>
    /// 余白なしの <see cref="Padding"/> を取得します。
    /// </summary>
    public static Padding None => default;

    /// <summary>
    /// 左右の余白の合計を取得します。
    /// </summary>
    public int Horizontal => Left + Right;

    /// <summary>
    /// 上下の余白の合計を取得します。
    /// </summary>
    public int Vertical => Top + Bottom;

    /// <summary>
    /// 整数から、上下左右に同じ余白を持つ <see cref="Padding"/> へ変換します。
    /// </summary>
    /// <param name="all">上下左右の余白。</param>
    public static implicit operator Padding(int all) => new(all);
}
