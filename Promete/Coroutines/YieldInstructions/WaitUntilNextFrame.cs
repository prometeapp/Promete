namespace Promete.Coroutines;

/// <summary>
///     A yield instruction to wait until the next frame.
/// </summary>
public class WaitUntilNextFrame : YieldInstruction
{
    public override bool KeepWaiting => false;
}
