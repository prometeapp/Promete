using System.Diagnostics;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.debug;

[Demo("/debug/text_element_memory_leak", "Text エレメントのメモリリーク問題を再現します。")]
public class TextElementMemoryLeakDebugScene : Scene
{
	private readonly Text _textElement;
	private readonly Process _process;
	private readonly Keyboard _keyboard;

	private HashSet<int> _hash = [];

	public TextElementMemoryLeakDebugScene(Keyboard keyboard)
	{
		Root =
		[
			_textElement = new Text("")
				.Location(32, 32),
		];

		_keyboard = keyboard;
		_process = Process.GetCurrentProcess();
	}

	public override void OnUpdate()
	{
		_textElement.Content = $"Memory: {_process.PrivateMemorySize64 / 1024f / 1024:F2} MB";
		if (_keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}
}
