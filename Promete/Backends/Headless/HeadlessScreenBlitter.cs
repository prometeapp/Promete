using System.Collections.Generic;
using Promete.Graphics;

namespace Promete.Backends.Headless;

public class HeadlessScreenBlitter(IRenderTextureProvider provider, IGameView view) : IScreenBlitter
{
    public RenderTexture ScreenRenderTexture { get; } = provider.Create(view.Size);

    public void BlitToScreen(IReadOnlyList<Material> materials) { }
}
