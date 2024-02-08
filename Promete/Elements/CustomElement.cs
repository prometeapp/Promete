using System.Collections.Generic;

namespace Promete.Elements;

public class CustomElement : ElementBase
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
