using System;
using System.Numerics;

namespace Promete.Graphics;

/// <summary>
/// テクスチャを抽象化します。
/// </summary>
public interface ITexture : IDisposable
{
	/// <summary>
	/// このテクスチャのサイズを取得します。
	/// </summary>
	Vector2 Size { get; }

	/// <summary>
	/// このテクスチャが破棄されているかどうかを取得します。
	/// </summary>
	bool IsDisposed { get; }
}
