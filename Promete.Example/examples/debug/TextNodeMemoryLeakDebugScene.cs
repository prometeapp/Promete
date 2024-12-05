using System.Diagnostics;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

[Demo("/debug/text_node_memory_leak", "Textノードのメモリリーク問題を再現します。")]
public class TextNodeMemoryLeakDebugScene : Scene
{
    private readonly Keyboard _keyboard;
    private readonly Process _process;
    private readonly Text _textNode;

    public TextNodeMemoryLeakDebugScene(Keyboard keyboard)
    {
        Root =
        [
            _textNode = new Text("")
                .Location(32, 32)
        ];

        _keyboard = keyboard;
        _process = Process.GetCurrentProcess();
    }

    public override void OnUpdate()
    {
        _textNode.Content = $"Memory: {_process.PrivateMemorySize64 / 1024f / 1024:F2} MB";
        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }
}
