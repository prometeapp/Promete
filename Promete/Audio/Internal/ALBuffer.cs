using System;
using Silk.NET.OpenAL;

namespace Promete.Audio.Internal;

struct ALBuffer(AL al) : IDisposable
{
	public uint Handle { get; } = al.GenBuffer();

	public void Dispose()
	{
		al.DeleteBuffer(Handle);
	}
}
