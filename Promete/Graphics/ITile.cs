using Promete.Nodes;
using Promete.Windowing;

namespace Promete.Graphics;

/// <summary>
/// <see cref="Tilemap"/> が扱えるタイルを定義します。
/// </summary>
public interface ITile
{
	Texture2D GetTexture(Tilemap map, VectorInt tileLocation, IWindow window);

	/// <summary>
	/// この <see cref="ITile"/> を破棄します。
	/// </summary>
	void Destroy();
}
