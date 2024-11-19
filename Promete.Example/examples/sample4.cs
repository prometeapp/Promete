using System.Drawing;
using System.Text;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;

namespace Promete.Example.examples;


[Demo("/sample4.demo", "簡易テキストエディタ")]
public class TextEditorScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
	private readonly StringBuilder buf = new();
	private Text? editorView;

	public override void OnStart()
	{
		console.Print("Promete Text Editor");
		console.Print("Press [ESC] to exit");

		editorView = new Text("", font: Font.GetDefault(16), color: Color.White)
			.Location(8, 64);
		Root.Add(editorView);

		// 実行前に残ったキー入力をクリア
		keyboard.GetString();
	}

	public override void OnUpdate()
	{
		editorView!.Content = buf.ToString() + '_';
		if ((keyboard.BackSpace.ElapsedFrameCount == 1 || keyboard.BackSpace.ElapsedTime > 0.5f && keyboard.BackSpace.ElapsedFrameCount % 3 == 0) && buf.Length > 0) buf.Length--;
		if (keyboard.Enter.ElapsedFrameCount == 1 || keyboard.Enter.ElapsedTime > 0.5f && keyboard.Enter.ElapsedFrameCount % 3 == 0) buf.Append('\n');

		if (keyboard.HasChar()) buf.Append(keyboard.GetString());

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}
}
