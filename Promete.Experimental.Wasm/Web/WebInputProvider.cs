using Promete.Backends.SilkNetCommon;
using Silk.NET.Input;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// ブラウザの入力イベント (<see cref="WebInputContext"/>) を <see cref="InputProvider"/> として提供します。
/// </summary>
public sealed class WebInputProvider : InputProvider
{
    public override IInputContext CreateInput() => WebInputContext.Instance;
}
