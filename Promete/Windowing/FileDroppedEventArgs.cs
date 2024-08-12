using System.Linq;

namespace Promete
{
	/// <summary>
	/// Arguments for file-dropped-event.
	/// </summary>
	public struct FileDroppedEventArgs
	{
		/// <summary>
		/// Get pathes of dropped files.
		/// </summary>
		public string[] Pathes { get; set; }

		/// <summary>
		/// Get path of a dropped file.
		/// </summary>
		public string Path => Pathes.First();

		public FileDroppedEventArgs(string[] pathes) => Pathes = pathes;
	}
}
