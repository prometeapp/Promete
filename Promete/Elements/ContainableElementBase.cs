using System;
using System.Collections.ObjectModel;
using System.Linq;
#pragma warning disable CS0618 // 型またはメンバーが旧型式です

namespace Promete.Elements;

public abstract class ContainableElementBase : ElementBase
{
	private bool _isSortingRequested = true;
	protected internal bool isTrimmable;
	protected internal ElementBase[] sortedChildren = [];

	[Obsolete("直接このフィールドは操作しないでください。代わりにAdd, Remove, Clear, Insertを使用してください。")]
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
			if (children.Count <= i) break;
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

	internal override void UpdateModelMatrix()
	{
		base.UpdateModelMatrix();
		foreach (var child in children)
		{
			child.UpdateModelMatrix();
		}
	}

	protected void Add(ElementBase el)
	{
		el.Parent?.Remove(el);

		el.Parent = this;
		children.Add(el);
		el.UpdateModelMatrix();
	}

	protected bool Remove(ElementBase el)
	{
		el.Parent = null;
		return children.Remove(el);
	}

	protected void Clear()
	{
		foreach (var el in children)
		{
			el.Parent = null;
		}
		children.Clear();
		sortedChildren = [];
	}

	protected void Insert(int index, ElementBase el)
	{
		el.Parent?.Remove(this);

		children.Insert(index, el);
		el.Parent = this;
		el.UpdateModelMatrix();
	}
}
