using Promete.Coroutines;
using Promete.Elements;
using Promete.Windowing;

namespace Promete.Tweening;

public class TweenManager
{
	private readonly Coroutine _coroutine;

	public TweenManager(Coroutine coroutine, IWindow window)
	{
		_coroutine = coroutine;

		window.Update += Update;

		window.Destroy += () => { window.Update -= Update; };
	}

	private void Update()
	{
		// TODO: Tween更新処理を書く
	}
}
