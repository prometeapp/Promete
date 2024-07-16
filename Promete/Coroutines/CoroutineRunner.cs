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

		private readonly Dictionary<Coroutine, object?> coroutines = new();

		public CoroutineManager(IWindow window)
		{
			_window = window;
			_window.Update += Update;
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
			coroutines.Keys.ToList().ForEach(c => c.Stop());
			coroutines.Clear();
		}

		private void Update()
		{
			foreach (var (coroutine, obj) in coroutines.Select(c => (c.Key, c.Value)).ToArray())
			{
				var instruction = ToYieldInstruction(obj);
				if (instruction.KeepWaiting) continue;

				try
				{
					if (coroutine.MoveNext())
					{
						var cur = coroutine.Current;
						// IEnumerator が来たら再度コルーチン開始する
						cur = cur is IEnumerator ie ? Start(ie) : cur;
						coroutines[coroutine] = cur;
					}
					else
					{
						Stop(coroutine);
						coroutine.ThenAction?.Invoke(obj);
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
	}
}
