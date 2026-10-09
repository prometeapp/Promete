; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PROMETE0001 | Promete.SceneGen | Warning | シーンが生成コードから参照できないため自動登録されない
PROMETE0002 | Promete.SceneGen | Warning | シーンに公開コンストラクタが無いため自動登録されない
PROMETE0003 | Promete.SceneGen | Warning | UseScenesFrom で指定していないアセンブリのシーンを読み込んでいる
PROMETE0004 | Promete.SceneGen | Info | 参照先のシーンが UseScenesFrom で指定されていない
PROMETE0005 | Promete.SceneGen | Warning | 自動登録されないシーンを読み込んでいる
