using System.Collections.Generic;
using System.Drawing;

namespace Promete.Elements;

/// <summary>
/// Setup API向けのElementの拡張
/// </summary>
public static class SetupApiExtension
{
	public static T Location<T>(this T el, Vector vec) where T : ElementBase
	{
		el.Location = vec;
		return el;
	}
	public static T Location<T>(this T el, float x, float y) where T : ElementBase
	{
		el.Location = (x, y);
		return el;
	}

	public static T Angle<T>(this T el, float angle) where T : ElementBase
	{
		el.Angle = angle;
		return el;
	}

	public static T Scale<T>(this T el, Vector vec) where T : ElementBase
	{
		el.Scale = vec;
		return el;
	}

	public static T Scale<T>(this T el, float x, float y) where T : ElementBase
	{
		el.Scale = (x, y);
		return el;
	}

	public static T Size<T>(this T el, VectorInt vec) where T : ElementBase
	{
		el.Size = vec;
		return el;
	}

	public static T Size<T>(this T el, int width, int height) where T : ElementBase
	{
		el.Size = (width, height);
		return el;
	}

	public static T Name<T>(this T el, string name) where T : ElementBase
	{
		el.Name = name;
		return el;
	}

	public static T Children<T>(this T el, params ElementBase[] children) where T : Container
	{
		el.AddRange(children);
		return el;
	}

	public static T Children<T>(this T el, IEnumerable<ElementBase> children) where T : Container
	{
		el.AddRange(children);
		return el;
	}
}
