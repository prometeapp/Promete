using System.Collections.Generic;

namespace Promete.Elements;

public class CustomElement(
	string name = "",
	Vector? location = default,
	Vector? scale = default,
	VectorInt? size = default
) : ElementBase(name, location, scale, size)
{
	protected internal bool isTrimmable;
	protected internal readonly List<ElementBase> children = [];

	internal override void Update()
	{
		base.Update();
		for (var i = 0; i < children.Count; i++)
		{
			children[i].Update();
		}
	}
}
