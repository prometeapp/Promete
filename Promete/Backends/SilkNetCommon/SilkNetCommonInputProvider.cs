using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Promete.Backends.SilkNetCommon;

public class InputProvider
{
    private IInputContext? _cache;
    private readonly IWindow _window = null!;

    public InputProvider(IWindow window)
    {
        _window = window;
    }

    protected InputProvider() { }

    public virtual IInputContext CreateInput()
    {
        if (_cache != null)
            return _cache;
        return _cache = _window.CreateInput();
    }
}
