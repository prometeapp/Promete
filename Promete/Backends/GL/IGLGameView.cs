namespace Promete.Backends.GL;

/// <summary>
/// OpenGL 系バックエンドの <see cref="IGameView"/> が公開する、GL 描画に必要な情報です。
/// </summary>
/// <remarks>
/// GL ランナーはこのインターフェースだけに依存するため、ウィンドウの実装
/// (デスクトップの Silk.NET ウィンドウ、ブラウザの canvas など) を問いません。
/// </remarks>
public interface IGLGameView : IGameView
{
    /// <summary>
    /// 描画に用いる OpenGL (ES) のコンテキストを取得します。
    /// </summary>
    public Silk.NET.OpenGL.GL GL { get; }

    /// <summary>
    /// 既定のフレームバッファのサイズを、物理ピクセル単位で取得します。
    /// </summary>
    public VectorInt FramebufferSize { get; }
}
