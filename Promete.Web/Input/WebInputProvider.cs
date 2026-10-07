using Promete.Backends.SilkNetCommon;
using Silk.NET.Input;

namespace Promete.Web.Input;

/// <summary>
/// ブラウザの入力イベント (<see cref="WebInputContext"/>) を <see cref="InputProvider"/> として提供します。
/// </summary>
internal sealed class WebInputProvider : InputProvider
{
    public override IInputContext CreateInput() => WebInputContext.Instance;
}
