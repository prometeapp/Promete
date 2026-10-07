using System.IO;

namespace Promete.Graphics.Fonts;

/// <summary>
/// <see cref="Font"/> の生成方法を提供するインターフェースです。
/// </summary>
/// <remarks>
/// <see cref="Font.FromFile"/> などの静的メソッドは、実行中のバックエンドが提供する実装 (<see cref="Backends.BackendBase.SetupFontProvider"/>) に処理を委ねます。
/// <para>
/// 実装は、同じパスに対して同じ <see cref="IGlyphSource"/> を返す必要があります。<see cref="Font"/> の等価性は、グリフの供給元の参照の一致で判定されるためです。
/// 環境が対応しない操作では、<see cref="System.NotSupportedException"/> をスローしてかまいません。
/// </para>
/// </remarks>
public interface IFontProvider
{
    /// <summary>
    /// フォントファイルからフォントを生成します。
    /// </summary>
    /// <param name="path">フォントファイルのパス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <param name="faceIndex">フォントファイル内のフェイスの番号。</param>
    /// <returns>生成したフォント。</returns>
    public Font FromFile(
        string path,
        float size,
        FontStyle style,
        bool isAntialiased,
        int faceIndex
    );

    /// <summary>
    /// 実行環境にインストールされているフォントを、ファミリー名を指定して読み込みます。
    /// </summary>
    /// <param name="familyName">フォントファミリー名。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成したフォント。</returns>
    public Font FromSystem(string familyName, float size, FontStyle style, bool isAntialiased);

    /// <summary>
    /// ストリームからフォントを生成します。
    /// </summary>
    /// <param name="stream">フォントのデータを含むストリーム。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成したフォント。</returns>
    public Font FromStream(Stream stream, float size, FontStyle style, bool isAntialiased);

    /// <summary>
    /// 実行環境の既定のフォントを生成します。
    /// </summary>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成したフォント。</returns>
    public Font GetDefault(float size, FontStyle style, bool isAntialiased);
}
