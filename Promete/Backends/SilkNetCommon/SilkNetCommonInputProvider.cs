using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Promete.Backends.SilkNetCommon;

public class InputProvider(IWindow window)
{
    private IInputContext? _cache;

    public IInputContext CreateInput()
    {
        if (_cache != null) return _cache;
        return _cache = window.CreateInput();
    }
}
