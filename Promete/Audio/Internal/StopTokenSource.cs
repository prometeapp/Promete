using System;

namespace Promete.Audio.Internal;

internal class StopTokenSource : IDisposable
{
	public bool IsStopRequested { get; private set; }

	public void Stop()
	{
		IsStopRequested = true;
	}

	public void Dispose()
	{
		IsStopRequested = true;
	}
}
