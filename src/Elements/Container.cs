using System.Collections;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Promete.Elements;

public class Container : ElementBase, IEnumerable<ElementBase>
{
	public int Count => children.Count;

	public ElementBase this[int index] => children[index];

	public bool IsTrimmable { get; set; }

	public Container()
	{
	}

	public Container(bool isTrimmable) => IsTrimmable = isTrimmable;

	public void Insert(int index, ElementBase item)
	{
		children.Insert(index, item);
		item.Parent = this;
	}

	public void RemoveAt(int index)
	{
		Remove(this[index]);
	}

	public void Add(ElementBase item)
	{
		children.Add(item);
		item.Parent = this;
	}

	public void AddRange(IEnumerable<ElementBase> elements)
	{
		foreach (var el in elements)
			Add(el);
	}

	public void AddRange(params ElementBase[] elements)
		=> AddRange((IEnumerable<ElementBase>)elements);

	public void Clear()
	{
		children.ForEach(child => child.Parent = null);
		children.Clear();
	}

	public bool Contains(ElementBase item)
	{
		return children.Contains(item);
	}

	public bool Remove(ElementBase item)
	{
		item.Parent = null;
		return children.Remove(item);
	}

	public IEnumerator<ElementBase> GetEnumerator()
	{
		return children.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return children.GetEnumerator();
	}

	internal override void Update()
	{
		base.Update();
		for (var i = 0; i < children.Count; i++)
		{
			children[i].Update();
		}
	}

	private readonly List<ElementBase> children = new();
}
