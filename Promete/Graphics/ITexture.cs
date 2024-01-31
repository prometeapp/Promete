using System;

namespace Promete.Graphics;

/// <summary>
/// テクスチャを抽象化します。
/// </summary>
public interface ITexture : IDisposable
{
	/// <summary>
	/// このテクスチャのサイズを取得します。
	/// </summary>
	VectorInt Size { get; }

	/// <summary>
	/// このテクスチャが破棄されているかどうかを取得します。
	/// </summary>
	bool IsDisposed { get; }
}
