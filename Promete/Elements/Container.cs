using System.Collections;
using System.Collections.Generic;

namespace Promete.Elements;

/// <summary>
/// 全ての Element を入れ子できる Element。
/// </summary>
public class Container : ContainableElementBase, IEnumerable<ElementBase>
{
	public int Count => children.Count;

	public ElementBase this[int index] => children[index];

	public bool IsTrimmable
	{
		get => isTrimmable;
		set => isTrimmable = value;
	}

	/// <summary>
	/// Container の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="isTrimmable">範囲外に出た子要素を描画しないかどうか。</param>
	public Container(bool isTrimmable = false)
	{
		IsTrimmable = isTrimmable;
	}

	public new void Insert(int index, ElementBase item)
	{
		base.Insert(index, item);
	}

	public void RemoveAt(int index)
	{
		Remove(this[index]);
	}

	public new void Add(ElementBase item)
	{
		base.Add(item);
	}

	public void AddRange(IEnumerable<ElementBase> elements)
	{
		foreach (var el in elements)
			Add(el);
	}

	public void AddRange(params ElementBase[] elements)
		=> AddRange((IEnumerable<ElementBase>)elements);

	public new void Clear()
	{
		base.Clear();
	}

	public bool Contains(ElementBase item)
	{
		return children.Contains(item);
	}

	public new bool Remove(ElementBase item)
	{
		return base.Remove(item);
	}

	public IEnumerator<ElementBase> GetEnumerator()
	{
		return children.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return children.GetEnumerator();
	}
}
