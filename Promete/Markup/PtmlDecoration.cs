namespace Promete.Markup;

/// <summary>
///     PTMLの装飾情報を表すレコード。
/// </summary>
/// <param name="Start">この装飾を開始する文字を含む、開始インデックス。</param>
/// <param name="End">この装飾を終了する文字を含まない、終了インデックス。</param>
/// <param name="TagName">PTMLタグ名。</param>
/// <param name="Attribute">PTMLタグの属性。</param>
public record struct PtmlDecoration(int Start, int End, string TagName, string Attribute);
