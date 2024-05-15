using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.async;

[Demo("/async/run_task.demo", "重たいTaskを動かします。")]
public class run_task(ConsoleLayer console, Keyboard keyboard) : Scene
{
	public override void OnStart()
	{
		console.Print("Press [ESC] to exit");
		console.Print("Press [SPACE] to run task");
	}

	public override void OnUpdate()
	{
		if (keyboard.Escape.IsKeyDown)
		{
			App.LoadScene<MainScene>();
		}

		if (keyboard.Space.IsKeyDown)
		{
			_ = RunTaskAsync();
		}
	}

	private async Task RunTaskAsync()
	{
		console.Clear();
		await Task.WhenAll(
			Enumerable
				.Range(1, 5)
				.Select(i => Task.Run(() => HeavyWorker(i)))
		);
		Print("[MAIN]: Done all tasks!");
		Print("Press [ESC] to exit");
	}

	private async Task HeavyWorker(int id)
	{
		var duration = Random.Shared.Next(5000);
		Print($"[TASK-{id}]: Waiting for {duration}ms...");
		await Task.Delay(duration).ConfigureAwait(false);
		Print($"[TASK-{id}]: Done");
	}

	private void Print(string message)
	{
		App.NextFrame(() => console.Print(message));
	}
}

