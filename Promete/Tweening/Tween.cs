using Promete.Coroutines;
using Promete.Elements;

namespace Promete.Tweening;

public class Tween
{
	private readonly Coroutine coroutine;
	private readonly TweenManager tween;
	private readonly ElementBase el;

	internal Tween(Coroutine coroutine, TweenManager tween)
	{
		this.coroutine = coroutine;
		this.tween = tween;
		this.el = el;
	}
}
