using System;
using Silk.NET.OpenAL;

namespace Promete.Audio.Internal;

struct ALSource(AL al) : IDisposable
{
	public uint Handle { get; } = al.GenSource();

	public void Dispose()
	{
		al.DeleteSource(Handle);
	}
}
