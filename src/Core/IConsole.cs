using System.Drawing;

namespace Promete
{
	/// <summary>
	/// Provides Console API.
	/// </summary>
	public interface IConsole
	{
		/// <summary>
		/// Get or set a current position of this console.
		/// </summary>
		VectorInt Cursor { get; set; }

		/// <summary>
		/// Get or set font size to render this console.
		/// </summary>
		int FontSize { get; set; }

		/// <summary>
		/// Get or set a font path to render this console.
		/// </summary>
		/// <value>Path to the font. If <c>null</c>, default font is used.</value>
		string? FontPath { get; set; }

		/// <summary>
		/// Get or set a text color to render this console.
		/// </summary>
		/// <value></value>
		Color TextColor { get; set; }

		/// <summary>
		/// Print a provided object to the current position of this console.
		/// </summary>
		/// <param name="obj">A object to print. It will be converted to string by using ToString method.</param>
		void Print(object? obj);

		/// <summary>
		/// Clear this console.
		/// </summary>
		void Cls();
	}
}
