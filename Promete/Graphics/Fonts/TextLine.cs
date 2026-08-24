namespace Promete.Graphics.Fonts;

/// <summary>
/// レイアウトされたテキストの 1 行を表します。
/// </summary>
/// <param name="StartIndex">この行が始まる、プレーンテキスト上のインデックス。</param>
/// <param name="Length">この行に含まれる文字数。</param>
/// <param name="Width">この行の幅。</param>
/// <param name="Top">テキスト全体の上端から、この行の上端までの距離。</param>
/// <param name="Height">この行の高さ。</param>
/// <param name="Baseline">この行の上端から、ベースラインまでの距離。</param>
public readonly record struct TextLine(
    int StartIndex,
    int Length,
    int Width,
    int Top,
    int Height,
    int Baseline
);
