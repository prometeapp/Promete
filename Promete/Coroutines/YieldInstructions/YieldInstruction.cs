namespace Promete.Coroutines;

/// <summary>
/// コルーチンを待機するためのイールド命令の基底クラスです。
/// </summary>
public abstract class YieldInstruction
{
    /// <summary>
    /// イールド命令が完了するまで待機するかどうかを示す値を取得します。
    /// </summary>
    public abstract bool KeepWaiting { get; }
}
