namespace Promete.Graphics.Fonts;

/// <summary>
/// 実行環境にインストールされているフォントの情報を表します。
/// </summary>
/// <param name="FamilyName">フォントファミリー名。</param>
/// <param name="StyleName">スタイル名。</param>
/// <param name="Style">解決されたフォントスタイル。</param>
/// <param name="Path">フォントファイルのパス。</param>
/// <param name="FaceIndex">コレクション内におけるフェイスのインデックス。</param>
public readonly record struct SystemFontInfo(
    string FamilyName,
    string StyleName,
    FontStyle Style,
    string Path,
    int FaceIndex
);
