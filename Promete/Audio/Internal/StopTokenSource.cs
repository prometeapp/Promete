using System;

namespace Promete.Audio.Internal;

internal class StopTokenSource : IDisposable
{
    public bool IsStopRequested { get; private set; }

    public void Dispose()
    {
        IsStopRequested = true;
    }

    public void Stop()
    {
        IsStopRequested = true;
    }
}
