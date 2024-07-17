using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Promete.Windowing;

namespace Promete.Coroutines
{
	/// <summary>
	/// A coroutine manager class.
	/// </summary>
	public class CoroutineManager
	{
		private readonly IWindow _window;

		private readonly Dictionary<Coroutine, YieldInstruction?> coroutines = new();

		public CoroutineManager(PrometeApp app, IWindow window)
		{
			_window = window;
			_window.Update += Update;

			app.SceneWillChange += ClearAllNonKeepAliveCoroutines;
		}

		/// <summary>
		/// Start the specified coroutine.
		/// </summary>
		public Coroutine Start(IEnumerator coroutine)
		{
			var c = new Coroutine(coroutine);

			coroutines[c] = null;
			c.Start();
			return c;
		}

		/// <summary>
		/// Stop the specified coroutine.
		/// </summary>
		public void Stop(Coroutine coroutine)
		{
			coroutines.Remove(coroutine);
			coroutine.Stop();
		}

		/// <summary>
		/// Stop all running coroutines.
		/// </summary>
		public void Clear()
		{
			// Stop
			coroutines.Keys.ToList().ForEach(Stop);
		}

		private void Update()
		{
			foreach (var (coroutine, instruction) in coroutines.Select(c => (c.Key, c.Value)).ToArray())
			{
				if (instruction is { KeepWaiting: true }) continue;

				try
				{
					if (coroutine.MoveNext())
					{
						coroutines[coroutine] = ToYieldInstruction(coroutine.Current);
					}
					else
					{
						Stop(coroutine);
						coroutine.ThenAction?.Invoke();
					}
				}
				catch (Exception ex)
				{
					coroutine.Stop();
					coroutine.ErrorAction?.Invoke(ex);
				}
			}
		}

		private YieldInstruction ToYieldInstruction(object? obj)
		{
			return obj switch
			{
				YieldInstruction y => y,
				IEnumerator ie => Start(ie),
				Task t => new WaitForTask(t),
				ValueTask t => new WaitForTask(t),
				_ => new WaitUntilNextFrame(),
			};
		}

		private void ClearAllNonKeepAliveCoroutines()
		{
			var nonKeepAliveCoroutines = coroutines.Keys.Where(c => !c.IsKeepAlive).ToList();
			nonKeepAliveCoroutines.ForEach(Stop);
		}
	}
}
