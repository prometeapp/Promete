using System.Threading.Tasks;

namespace Promete.Coroutines;

public class WaitForTask : YieldInstruction
{
	public override bool KeepWaiting =>
		t != null
			? !(t.IsCanceled || t.IsCompleted || t.IsCompletedSuccessfully || t.IsFaulted)
			: vt is ValueTask v &&
			  !(v.IsCanceled || v.IsCompletedSuccessfully || v.IsCompletedSuccessfully || v.IsFaulted);

	public WaitForTask(Task task) => t = task;

	public WaitForTask(ValueTask task) => vt = task;

	private readonly Task? t;
	private readonly ValueTask? vt;
}
