using System;

namespace Promete.Graphics;

public interface ITexture : IDisposable
{
	VectorInt Size { get; }

	bool IsDisposed { get; }
}
