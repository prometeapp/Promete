using Promete.Example.Kernel;
using Promete.Input;
using static Promete.Example.Kernel.DemoKernel;

namespace Promete.Example;

public class MainScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    public override void OnStart() { }

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
            if (CurrentIndex == CurrentFolder.Files.Count)
            {
                if (CurrentFolder.Parent == null)
                    return;

                CurrentFolder = CurrentFolder.Parent;
                CurrentIndex = 0;
            }
            else
            {
                var item = CurrentFolder.Files[CurrentIndex];
                switch (item)
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
        else if (keyboard.Escape.IsKeyDown)
        {
            if (CurrentFolder.Parent == null)
                return;

            CurrentFolder = CurrentFolder.Parent;
            CurrentIndex = 0;
        }
    }
}
