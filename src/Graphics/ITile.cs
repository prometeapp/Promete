using System.Drawing;

namespace Promete.Graphics;

/// <summary>
/// <see cref="Tilemap"/> が扱えるタイルを定義します。
/// </summary>
public interface ITile
{
	// void Draw(Tilemap map, VectorInt tileLocation, Vector locationToDraw, Color? color);

	/// <summary>
	/// この <see cref="ITile"/> を破棄します。
	/// </summary>
	void Destroy();
}
