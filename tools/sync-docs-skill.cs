// Promete.Docs のガイドを、Agent Skill「promete」の references に同期する。
//
//   dotnet run tools/sync-docs-skill.cs              references と SKILL.md の索引を更新する
//   dotnet run tools/sync-docs-skill.cs -- --check   同期済みか確認し、差分があれば終了コード 1 を返す
//
// 改行は LF に揃えて比較・出力する。作業ツリーの改行が環境 (core.autocrlf) によって
// 異なっても、判定結果が変わらないようにするため。

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

const string IndexBegin = "<!-- BEGIN GENERATED INDEX -->";
const string IndexEnd = "<!-- END GENERATED INDEX -->";

// サイドバー (Promete.Docs/astro.config.mjs) と同じ並び順・見出し
(string Directory, string Label)[] categories =
[
    ("intro", "入門編"),
    ("manual", "コア"),
    ("graphics", "グラフィック"),
    ("text", "テキスト"),
    ("input", "入力"),
    ("audio", "オーディオ"),
    ("math", "数学"),
    ("other", "その他"),
    ("plugins", "プラグイン"),
    ("extends", "エンジンを拡張する"),
];

var repoRoot = Path.GetFullPath(Path.Combine(GetScriptDirectory(), ".."));
var guideDir = Path.Combine(repoRoot, "Promete.Docs", "src", "content", "docs", "guide");
var skillDir = Path.Combine(repoRoot, "plugins", "promete", "skills", "promete");
var referencesDir = Path.Combine(skillDir, "references");
var skillMdPath = Path.Combine(skillDir, "SKILL.md");
var checkOnly = args.Contains("--check");

var pages = Directory
    .EnumerateFiles(guideDir, "*", SearchOption.AllDirectories)
    .Where(path => path.EndsWith(".md") || path.EndsWith(".mdx"))
    .Select(path => LoadPage(guideDir, path))
    .ToList();

// skillDir からの相対パス → 期待されるファイル内容
var expected = new Dictionary<string, string>();
foreach (var page in pages)
{
    expected[$"references/{page.RelativePath}"] = page.Content;
}

var skillMd = ReadText(skillMdPath);
expected["SKILL.md"] = ReplaceIndex(skillMd, BuildIndex(pages));

var actualReferences = Directory.Exists(referencesDir)
    ? Directory
        .EnumerateFiles(referencesDir, "*", SearchOption.AllDirectories)
        .Select(path => ToSlash(Path.GetRelativePath(skillDir, path)))
        .ToHashSet()
    : [];

var stale = expected
    .Where(pair =>
    {
        var path = Path.Combine(skillDir, pair.Key);
        return !File.Exists(path) || ReadText(path) != pair.Value;
    })
    .Select(pair => pair.Key)
    .ToList();
var extra = actualReferences.Where(path => !expected.ContainsKey(path)).ToList();

if (stale.Count == 0 && extra.Count == 0)
{
    Console.WriteLine("スキルはドキュメントと同期済みです。");
    return 0;
}

if (checkOnly)
{
    Console.Error.WriteLine("スキルがドキュメントと同期されていません。");
    foreach (var path in stale)
        Console.Error.WriteLine($"  更新が必要: {path}");
    foreach (var path in extra)
        Console.Error.WriteLine($"  削除が必要: {path}");
    Console.Error.WriteLine(
        "`dotnet run tools/sync-docs-skill.cs` を実行してコミットしてください。"
    );
    return 1;
}

foreach (var path in stale)
{
    var fullPath = Path.Combine(skillDir, path);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, expected[path], new UTF8Encoding(false));
    Console.WriteLine($"更新: {path}");
}

foreach (var path in extra)
{
    File.Delete(Path.Combine(skillDir, path));
    Console.WriteLine($"削除: {path}");
}

RemoveEmptyDirectories(referencesDir);
return 0;

// ガイドの 1 ページを読み込み、索引に使う frontmatter を取り出す
static Page LoadPage(string guideDir, string path)
{
    var content = ReadText(path);
    var frontmatter = Regex
        .Match(content, @"\A---\n(.*?)\n---", RegexOptions.Singleline)
        .Groups[1]
        .Value;

    string? Field(string pattern) =>
        Regex.Match(frontmatter, pattern, RegexOptions.Multiline) is { Success: true } m
            ? m.Groups[1].Value.Trim().Trim('"', '\'')
            : null;

    var relativePath = ToSlash(Path.GetRelativePath(guideDir, path));
    return new Page(
        relativePath,
        Field(@"^title:\s*(.+)$") ?? Path.GetFileNameWithoutExtension(path),
        Field(@"^description:\s*(.+)$"),
        int.TryParse(Field(@"^\s+order:\s*(-?\d+)\s*$"), out var order) ? order : int.MaxValue,
        content
    );
}

// カテゴリごとのページ一覧を Markdown で組み立てる
string BuildIndex(List<Page> pages)
{
    var known = categories.Select(c => c.Directory).ToHashSet();

    // サイドバーに無いディレクトリも黙って落とさず、末尾に並べる
    var unknown = pages
        .Select(p => p.Category)
        .Where(c => !known.Contains(c))
        .Distinct()
        .Order()
        .Select(c => (Directory: c, Label: c));

    var sb = new StringBuilder();
    foreach (var (directory, label) in categories.Concat(unknown))
    {
        var items = pages
            .Where(p => p.Category == directory)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.RelativePath, StringComparer.Ordinal)
            .ToList();
        if (items.Count == 0)
            continue;

        sb.Append($"\n### {label}\n\n");
        foreach (var item in items)
        {
            sb.Append($"- [{item.Title}](references/{item.RelativePath})");
            if (item.Description != null)
                sb.Append($": {item.Description}");
            sb.Append('\n');
        }
    }

    return sb.Append('\n').ToString();
}

static string ReplaceIndex(string skillMd, string index)
{
    var begin = skillMd.IndexOf(IndexBegin, StringComparison.Ordinal);
    var end = skillMd.IndexOf(IndexEnd, StringComparison.Ordinal);
    if (begin < 0 || end < begin)
        throw new InvalidOperationException(
            $"SKILL.md に {IndexBegin} と {IndexEnd} が見つかりません。"
        );

    begin += IndexBegin.Length;
    return skillMd[..begin] + index + skillMd[end..];
}

static void RemoveEmptyDirectories(string directory)
{
    foreach (var child in Directory.EnumerateDirectories(directory))
    {
        RemoveEmptyDirectories(child);
        if (!Directory.EnumerateFileSystemEntries(child).Any())
            Directory.Delete(child);
    }
}

static string ReadText(string path) => File.ReadAllText(path).ReplaceLineEndings("\n");

static string ToSlash(string path) => path.Replace('\\', '/');

static string GetScriptDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

/// <summary>
/// ガイドの 1 ページ。
/// </summary>
/// <param name="RelativePath">ガイドのルートからの相対パス (区切りは <c>/</c>)。</param>
/// <param name="Title">frontmatter の title。</param>
/// <param name="Description">frontmatter の description。</param>
/// <param name="Order">frontmatter の sidebar.order。無ければ末尾に並べる。</param>
/// <param name="Content">ファイルの内容 (改行は LF)。</param>
internal sealed record Page(
    string RelativePath,
    string Title,
    string? Description,
    int Order,
    string Content
)
{
    /// <summary>
    /// ページが属するカテゴリ (ガイド直下のディレクトリ名)。
    /// </summary>
    public string Category => RelativePath.Split('/')[0];
}
