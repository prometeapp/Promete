using System.Collections.Generic;

namespace Promete.Elements;

public abstract class ContainableElementBase : ElementBase
{
	protected internal bool isTrimmable;
	protected internal readonly List<ElementBase> children = [];

	internal override void Update()
	{
		base.Update();
		for (var i = 0; i < children.Count; i++)
		{
			children[i].Parent = this;
			children[i].Update();
		}

		// 破棄された子要素を削除
		children.RemoveAll(e => e.IsDestroyed);
	}
}
