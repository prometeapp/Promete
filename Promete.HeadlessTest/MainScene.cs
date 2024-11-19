using System.Collections;
using Promete.Coroutines;

namespace Promete.HeadlessTest;

public class MainScene(CoroutineManager coroutine) : Scene
{
	public override void OnStart()
	{
		Console.WriteLine("初期化したよ～ん");

		coroutine.Start(Task1())
			.Then(() => Console.WriteLine("タスク1が終わった"));

		coroutine.Start(Task2())
			.Error((e) => Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}\n{e.StackTrace}"));
	}

	public override void OnDestroy()
	{
		Console.WriteLine("終了したよ～ん");
	}

	private IEnumerator Task1()
	{
		for (var i = 1; i <= 10; i++)
		{
			Console.WriteLine($"{i}");
			yield return new WaitForSeconds(1);
		}
	}

	private IEnumerator Task2()
	{
		yield return new WaitForSeconds(3);
		throw new Exception("test");
	}
}
