using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Promete.Elements;

public abstract class ContainableElementBase : ElementBase
{
	private bool _isSortingRequested = true;
	protected internal bool isTrimmable;
	protected internal ElementBase[] sortedChildren = [];

	protected readonly ObservableCollection<ElementBase> children = [];

	protected ContainableElementBase()
	{

		children.CollectionChanged += (sender, args) => RequestSorting();
	}

	/// <summary>
	/// 要素のソートを要求します。要求された場合、次のUpdateフレームでソートが行われます。
	/// </summary>
	public void RequestSorting()
	{
		_isSortingRequested = true;
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
		for (var i = children.Count - 1; i >= 0; i--)
		{
			if (!children[i].IsDestroyed) continue;
			children.RemoveAt(i);
		}

		// ソートが要求されている場合、ソートを行う
		if (!_isSortingRequested) return;
		sortedChildren = children.OrderBy(c => c.ZIndex).ToArray();
		_isSortingRequested = false;
	}
}
