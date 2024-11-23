using System.Collections;
using System.Collections.Generic;
#pragma warning disable CS0618 // 型またはメンバーが旧型式です

namespace Promete.Nodes;

/// <summary>
/// 全ての <see cref="Node"/> を入れ子にできる <see cref="Node"/> です。
/// </summary>
public class Container : ContainableNode, IEnumerable<Node>
{
	public int Count => children.Count;

	public Node this[int index] => children[index];
	public bool IsTrimmable
	{
		get => isTrimmable;
		set => isTrimmable = value;
	}

	/// <summary>
	/// Container の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="isTrimmable">範囲外に出た子ノードを描画しないかどうか。</param>
	public Container(bool isTrimmable = false)
	{
		IsTrimmable = isTrimmable;
	}

	public new void Insert(int index, Node node)
	{
		base.Insert(index, node);
	}

	public void RemoveAt(int index)
	{
		Remove(this[index]);
	}

	public new void Add(Node node)
	{
		base.Add(node);
	}

	public void AddRange(IEnumerable<Node> nodes)
	{
		foreach (var node in nodes)
			Add(node);
	}

	public void AddRange(params Node[] nodes)
		=> AddRange((IEnumerable<Node>)nodes);

	public new void Clear()
	{
		base.Clear();
	}

	public bool Contains(Node node)
	{
		return children.Contains(node);
	}

	public new bool Remove(Node node)
	{
		return base.Remove(node);
	}

	public IEnumerator<Node> GetEnumerator()
	{
		return children.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return children.GetEnumerator();
	}
}
