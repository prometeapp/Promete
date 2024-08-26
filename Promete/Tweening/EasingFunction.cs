namespace Promete.Tweening;

using System;

/// <summary>
/// https://easings.net/ にあるイージング関数を提供します。
/// </summary>
public static class EasingFunction
{
	public static float Ease(float x, EasingFunctionType t) => t switch
	{
		EasingFunctionType.Linear => x,
		EasingFunctionType.EaseInSine => EaseInSine(x),
		EasingFunctionType.EaseOutSine => EaseOutSine(x),
		EasingFunctionType.EaseInOutSine => EaseInOutSine(x),
		EasingFunctionType.EaseInQuad => EaseInQuad(x),
		EasingFunctionType.EaseOutQuad => EaseOutQuad(x),
		EasingFunctionType.EaseInOutQuad => EaseInOutQuad(x),
		EasingFunctionType.EaseInCubic => EaseInCubic(x),
		EasingFunctionType.EaseOutCubic => EaseOutCubic(x),
		EasingFunctionType.EaseInOutCubic => EaseInOutCubic(x),
		EasingFunctionType.EaseInQuart => EaseInQuart(x),
		EasingFunctionType.EaseOutQuart => EaseOutQuart(x),
		EasingFunctionType.EaseInOutQuart => EaseInOutQuart(x),
		EasingFunctionType.EaseInQuint => EaseInQuint(x),
		EasingFunctionType.EaseOutQuint => EaseOutQuint(x),
		EasingFunctionType.EaseInOutQuint => EaseInOutQuint(x),
		EasingFunctionType.EaseInExpo => EaseInExpo(x),
		EasingFunctionType.EaseOutExpo => EaseOutExpo(x),
		EasingFunctionType.EaseInOutExpo => EaseInOutExpo(x),
		EasingFunctionType.EaseInCirc => EaseInCirc(x),
		EasingFunctionType.EaseOutCirc => EaseOutCirc(x),
		EasingFunctionType.EaseInOutCirc => EaseInOutCirc(x),
		EasingFunctionType.EaseInBack => EaseInBack(x),
		EasingFunctionType.EaseOutBack => EaseOutBack(x),
		EasingFunctionType.EaseInOutBack => EaseInOutBack(x),
		EasingFunctionType.EaseInElastic => EaseInElastic(x),
		EasingFunctionType.EaseOutElastic => EaseOutElastic(x),
		EasingFunctionType.EaseInOutElastic => EaseInOutElastic(x),
		EasingFunctionType.EaseInBounce => EaseInBounce(x),
		EasingFunctionType.EaseOutBounce => EaseOutBounce(x),
		EasingFunctionType.EaseInOutBounce => EaseInOutBounce(x),
		_ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
	};

	// Sine
	public static float EaseInSine(float x) => 1 - MathF.Cos((x * MathF.PI) / 2);
	public static float EaseOutSine(float x) => MathF.Sin((x * MathF.PI) / 2);
	public static float EaseInOutSine(float x) => -(MathF.Cos(MathF.PI * x) - 1) / 2;

	// Quad
	public static float EaseInQuad(float x) => x * x;
	public static float EaseOutQuad(float x) => 1 - (1 - x) * (1 - x);
	public static float EaseInOutQuad(float x) => x < 0.5f ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;

	// Cubic
	public static float EaseInCubic(float x) => x * x * x;
	public static float EaseOutCubic(float x) => 1 - MathF.Pow(1 - x, 3);
	public static float EaseInOutCubic(float x) => x < 0.5f ? 4 * x * x * x : 1 - MathF.Pow(-2 * x + 2, 3) / 2;

	// Quart
	public static float EaseInQuart(float x) => x * x * x * x;
	public static float EaseOutQuart(float x) => 1 - MathF.Pow(1 - x, 4);
	public static float EaseInOutQuart(float x) => x < 0.5f ? 8 * x * x * x * x : 1 - MathF.Pow(-2 * x + 2, 4) / 2;

	// Quint
	public static float EaseInQuint(float x) => x * x * x * x * x;
	public static float EaseOutQuint(float x) => 1 - MathF.Pow(1 - x, 5);
	public static float EaseInOutQuint(float x) => x < 0.5f ? 16 * x * x * x * x * x : 1 - MathF.Pow(-2 * x + 2, 5) / 2;

	// Expo
	public static float EaseInExpo(float x) => x == 0 ? 0 : MathF.Pow(2, 10 * x - 10);
	public static float EaseOutExpo(float x) => x == 1 ? 1 : 1 - MathF.Pow(2, -10 * x);

	public static float EaseInOutExpo(float x) => x == 0 ? 0 :
		x == 1 ? 1 :
		x < 0.5f ? MathF.Pow(2, 20 * x - 10) / 2 : (2 - MathF.Pow(2, -20 * x + 10)) / 2;

	// Circ
	public static float EaseInCirc(float x) => 1 - MathF.Sqrt(1 - MathF.Pow(x, 2));
	public static float EaseOutCirc(float x) => MathF.Sqrt(1 - MathF.Pow(x - 1, 2));
	public static float EaseInOutCirc(float x) => x < 0.5f
		? (1 - MathF.Sqrt(1 - MathF.Pow(2 * x, 2))) / 2
		: (MathF.Sqrt(1 - MathF.Pow(-2 * x + 2, 2)) + 1) / 2;

	// Back
	public static float EaseInBack(float x)
	{
		var c1 = 1.70158f;
		var c3 = c1 + 1;
		return c3 * x * x * x - c1 * x * x;
	}
	public static float EaseOutBack(float x)
	{
		var c1 = 1.70158f;
		var c3 = c1 + 1;
		return 1 + c3 * MathF.Pow(x - 1, 3) + c1 * MathF.Pow(x - 1, 2);
	}
	public static float EaseInOutBack(float x)
	{
		var c1 = 1.70158f;
		var c2 = c1 * 1.525f;
		return x < 0.5f
			? (MathF.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
			: (MathF.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
	}

	// Elastic
	public static float EaseInElastic(float x)
	{
		var c4 = (2 * MathF.PI) / 3;
		return x switch
		{
			0 => 0,
			1 => 1,
			_ => -MathF.Pow(2, 10 * x - 10) * MathF.Sin((x * 10 - 10.75f) * c4)
		};
	}
	public static float EaseOutElastic(float x)
	{
		var c4 = (2 * MathF.PI) / 3;
		return x switch
		{
			0 => 0,
			1 => 1,
			_ => MathF.Pow(2, -10 * x) * MathF.Sin((x * 10 - 0.75f) * c4) + 1
		};
	}
	public static float EaseInOutElastic(float x)
	{
		var c5 = (2 * MathF.PI) / 4.5f;
		return x switch
		{
			0 => 0,
			1 => 1,
			_ => x < 0.5f
				? -(MathF.Pow(2, 20 * x - 10) * MathF.Sin((20 * x - 11.125f) * c5)) / 2
				: (MathF.Pow(2, -20 * x + 10) * MathF.Sin((20 * x - 11.125f) * c5)) / 2 + 1
		};
	}

	// Bounce
	public static float EaseInBounce(float x) => 1 - EaseOutBounce(1 - x);
	public static float EaseOutBounce(float x)
	{
		var n1 = 7.5625f;
		var d1 = 2.75f;
		if (x < 1 / d1)
			return n1 * x * x;
		if (x < 2 / d1)
			return n1 * (x -= 1.5f / d1) * x + 0.75f;
		if (x < 2.5 / d1)
			return n1 * (x -= 2.25f / d1) * x + 0.9375f;
		return n1 * (x -= 2.625f / d1) * x + 0.984375f;
	}
	public static float EaseInOutBounce(float x) => x < 0.5f
		? (1 - EaseOutBounce(1 - 2 * x)) / 2
		: (1 + EaseOutBounce(2 * x - 1)) / 2;
}

public enum EasingFunctionType
{
	Linear,
	EaseInSine,
	EaseOutSine,
	EaseInOutSine,
	EaseInQuad,
	EaseOutQuad,
	EaseInOutQuad,
	EaseInCubic,
	EaseOutCubic,
	EaseInOutCubic,
	EaseInQuart,
	EaseOutQuart,
	EaseInOutQuart,
	EaseInQuint,
	EaseOutQuint,
	EaseInOutQuint,
	EaseInExpo,
	EaseOutExpo,
	EaseInOutExpo,
	EaseInCirc,
	EaseOutCirc,
	EaseInOutCirc,
	EaseInBack,
	EaseOutBack,
	EaseInOutBack,
	EaseInElastic,
	EaseOutElastic,
	EaseInOutElastic,
	EaseInBounce,
	EaseOutBounce,
	EaseInOutBounce,
}
