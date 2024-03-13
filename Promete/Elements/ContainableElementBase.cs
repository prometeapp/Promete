using System.Collections.Generic;
using System.Linq;

namespace Promete.Elements;

public abstract class ContainableElementBase : ElementBase
{
	protected internal bool isTrimmable;
	protected readonly List<ElementBase> children = [];

	protected internal ElementBase[] sortedChildren = [];

	private bool isSortingRequested;

	/// <summary>
	/// 要素のソートを要求します。要求された場合、次のUpdateフレームでソートが行われます。
	/// </summary>
	public void RequestSorting()
	{
		isSortingRequested = true;
	}

	internal override void Update()
	{
		base.Update();
		for (var i = 0; i < sortedChildren.Length; i++)
		{
			children[i].Parent = this;
			children[i].Update();
		}

		// 破棄された子要素を削除
		var hasDestroyedChildren = children.RemoveAll(e => e.IsDestroyed) > 0;

		// ソートが要求されているか、子要素が削除された場合、ソートを行う
		if (!hasDestroyedChildren && !isSortingRequested) return;
		sortedChildren = children.OrderBy(c => c.ZIndex).ToArray();
		isSortingRequested = false;
	}
}
