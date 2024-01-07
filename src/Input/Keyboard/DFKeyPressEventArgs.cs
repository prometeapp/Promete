using System;

namespace Promete
{
	/// <summary>
	/// Keyboard pressed event argument.
	/// </summary>
	public struct DFKeyPressEventArgs
	{
		public char KeyChar { get; }

		internal DFKeyPressEventArgs(char ch) => KeyChar = ch;
	}
}
