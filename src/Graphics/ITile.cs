using System.Drawing;

namespace Promete
{
	/// <summary>
	/// <see cref="Tilemap"/> が扱えるタイルを定義します。
	/// </summary>
	public interface ITile
	{
		/// <summary>
		/// この <see cref="ITile"/> をレンダリングします。
		/// </summary>
		void Draw(Tilemap map, VectorInt tileLocation, Vector locationToDraw, Color? color);

		/// <summary>
		/// この <see cref="ITile"/> を破棄します。
		/// </summary>
		void Destroy();
	}
}
