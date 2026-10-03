using Promete.Backends.SilkNetCommon;
using Promete.Windowing.Headless;
using Silk.NET.Input;

namespace Promete.Backends.Headless;

public class HeadlessInputProvider : InputProvider
{
    private readonly DummyInputContext _dummy = new();

    public override IInputContext CreateInput() => _dummy;
}
