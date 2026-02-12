using System.Collections;
using System.Drawing;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// コルーチン実行中にシーン切り替えを呼び出した後、後続処理が狂う？
/// https://github.com/prometeapp/Promete/issues/74
/// </summary>
[Demo("/debug/issue74", "Issue74: Debug Scene")]
public class Issue74DebugScene(CoroutineManager coroutine, ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        coroutine.Start(StartTask());
    }

    private IEnumerator StartTask()
    {
        var dog = new Dog();
        console.Print("Issue74DebugScene: StartTask started.");
        yield return new WaitForSeconds(1.0f);
        console.Print("Issue74DebugScene: Switching to Issue74NextScene.");
        App.LoadScene<MainScene>();
        console.Print("test");
        dog.Bark();
    }

    private class Dog
    {
        public void Bark()
        {
            Console.WriteLine("Woof!");
        }
    }
}


