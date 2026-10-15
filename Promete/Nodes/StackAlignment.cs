namespace Promete.Nodes;

/// <summary>
/// <see cref="StackContainer"/> で、並べる方向と直交する方向の揃え位置を表します。
/// </summary>
public enum StackAlignment
{
    /// <summary>
    /// 左端 (縦に並べる場合) または上端 (横に並べる場合) に揃えます。
    /// </summary>
    Start,

    /// <summary>
    /// 中央に揃えます。
    /// </summary>
    Center,

    /// <summary>
    /// 右端 (縦に並べる場合) または下端 (横に並べる場合) に揃えます。
    /// </summary>
    End,
}
