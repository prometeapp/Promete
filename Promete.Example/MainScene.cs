using Promete.Example.Kernel;
using Promete.Input;
using static Promete.Example.Kernel.DemoKernel;

namespace Promete.Example;

public class MainScene(Keyboard keyboard, Mouse mouse, ConsoleLayer console) : Scene
{
    // 項目が始まる行。タイトルとディレクトリの各行は、末尾の改行のぶん 2 行ずつ使う
    private const int HeaderLines = 4;

    private VectorInt _lastMousePosition;

    public override void OnStart()
    {
        _lastMousePosition = mouse.Position;
    }

    public override void OnUpdate()
    {
        OutputUI();
        HandleInput();
    }

    private void OutputUI()
    {
        console.Clear();
        var rendererName = DemoKernel.UseVulkan ? "Vulkan" : "OpenGL";
        console.Print($"Promete Demo ({rendererName})\n");
        console.Print($"現在のディレクトリ: /{CurrentFolder.GetFullPath()}\n");
        View.Title = $"Promete Demo ({rendererName}) - {CurrentFolder.GetFullPath()}";

        for (var i = 0; i < CurrentFolder.Files.Count; i++)
        {
            var item = CurrentFolder.Files[i];
            var label = item is SceneFile file ? $"{file.Name} - {file.Description}" : item.Name;
            console.Print($"{(i == CurrentIndex ? ">" : " ")} {label}");
        }

        console.Print($"{(CurrentIndex == CurrentFolder.Files.Count ? ">" : " ")} もどる");
    }

    private void HandleInput()
    {
        HandleMouse();

        if (keyboard.Up.IsKeyDown)
        {
            CurrentIndex--;
            if (CurrentIndex < 0)
                CurrentIndex = 0;
        }
        else if (keyboard.Down.IsKeyDown)
        {
            CurrentIndex++;
            if (CurrentIndex > CurrentFolder.Files.Count)
                CurrentIndex = CurrentFolder.Files.Count;
        }
        else if (keyboard.Enter.IsKeyDown)
        {
            Activate();
        }
        else if (keyboard.Escape.IsKeyDown)
        {
            if (CurrentFolder.Parent == null)
                return;

            CurrentFolder = CurrentFolder.Parent;
            CurrentIndex = 0;
        }
    }

    /// <summary>マウスやタッチで、項目の選択と決定を行う。</summary>
    private void HandleMouse()
    {
        var position = mouse.Position;
        var moved = position != _lastMousePosition;
        _lastMousePosition = position;

        var clicked = mouse[MouseButtonType.Left].IsButtonDown;
        if (!moved && !clicked)
            return;

        var lineHeight = console.Font.Metrics.LineHeight;
        if (lineHeight <= 0)
            return;

        var index = (int)(position.Y / lineHeight) - HeaderLines;
        if (index < 0 || index > CurrentFolder.Files.Count)
            return;

        // カーソルが乗った項目を選択する。タッチでは、押した瞬間に移動と決定が同時に起きる
        CurrentIndex = index;
        if (clicked)
            Activate();
    }

    /// <summary>選択中の項目を開く。</summary>
    private void Activate()
    {
        if (CurrentIndex == CurrentFolder.Files.Count)
        {
            if (CurrentFolder.Parent == null)
                return;

            CurrentFolder = CurrentFolder.Parent;
            CurrentIndex = 0;
            return;
        }

        switch (CurrentFolder.Files[CurrentIndex])
        {
            case Folder folder:
                CurrentFolder = folder;
                CurrentIndex = 0;
                break;
            case SceneFile file:
                App.LoadScene(file.Scene);
                break;
        }
    }
}
