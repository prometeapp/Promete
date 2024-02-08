using System.Collections;
using System.Collections.Generic;

namespace Promete.Elements;

/// <summary>
/// 全ての Element を入れ子できる Element。
/// </summary>
public class Container : CustomElement, IEnumerable<ElementBase>
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
	/// <param name="name">この Container の名前。</param>
	/// <param name="location">この Container の位置。</param>
	/// <param name="scale">この Container のスケール。</param>
	/// <param name="size">この Container のサイズ。</param>
	public Container(bool isTrimmable = false)
	{
		IsTrimmable = isTrimmable;
	}

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
}
