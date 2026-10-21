using System.Collections;
using Microsoft.Build.Framework;

namespace Promete.Test.AssetsBuild;

/// <summary>
/// MSBuild の外で Task を動かすための <see cref="IBuildEngine"/>。報告されたエラー・警告・メッセージを記録する。
/// </summary>
internal sealed class FakeBuildEngine : IBuildEngine
{
    public List<BuildErrorEventArgs> Errors { get; } = [];

    public List<BuildWarningEventArgs> Warnings { get; } = [];

    public List<BuildMessageEventArgs> Messages { get; } = [];

    /// <summary>報告されたエラーのコード。</summary>
    public IEnumerable<string> ErrorCodes => Errors.Select(e => e.Code);

    public bool ContinueOnError => false;

    public int LineNumberOfTaskNode => 0;

    public int ColumnNumberOfTaskNode => 0;

    public string ProjectFileOfTaskNode => "test.csproj";

    public bool BuildProjectFile(
        string projectFileName,
        string[] targetNames,
        IDictionary globalProperties,
        IDictionary targetOutputs
    ) => throw new NotSupportedException();

    public void LogCustomEvent(CustomBuildEventArgs e) { }

    public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);

    public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);

    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
}
