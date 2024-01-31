// SEE: https://qiita.com/NumAniCloud/items/6c99ab1d4ec8b8e1c8f8
using System.Collections.Concurrent;
using System.Threading;

namespace Promete.Internal
{
	/// <summary>
	/// Promete で内部的に使用する <see cref="SynchronizationContext"/> の実装を提供します。
	/// 本実装では、全ての非同期処理がゲームループの中で実行されるようになっており、シングルスレッドでの実行が保証されます。
	/// </summary>
	internal class PrSynchronizationContext : SynchronizationContext
	{
		readonly ConcurrentQueue<(SendOrPostCallback callback, object? state)> continuations = new();

		public override void Post(SendOrPostCallback d, object? state)
		{
			continuations.Enqueue((d, state));
		}

		public void Update()
		{
			while (continuations.TryDequeue(out var cont))
			{
				cont.callback(cont.state);
			}
		}
	}
}
