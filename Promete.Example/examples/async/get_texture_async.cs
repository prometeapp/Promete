using System.Collections;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.async;

[Demo("/async/get_texture_async.demo", "インターネット経由で画像を取得します")]
public class get_texture_async(ConsoleLayer console, Keyboard keyboard, CoroutineManager coroutine) : Scene
{
    public override void OnStart()
    {
        coroutine.Start(RunTask());
        console.Print("Fetching an image...");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }

    private IEnumerator RunTask()
    {
        var http = new HttpClient();
        var task = http.GetStreamAsync("https://placecats.com/300/200");
        yield return new WaitForTask(task);
        var texture = Window.TextureFactory.Load(task.Result);
        var sprite = new Sprite(texture)
            .Location(32, 96);
        Root.Add(sprite);

        console.Clear();
        console.Print("Press [ESC] to exit");
    }
}
