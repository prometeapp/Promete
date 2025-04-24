using System.Collections;
using System.Collections.Generic;

#pragma warning disable CS0618 // 型またはメンバーが旧型式です

namespace Promete.Nodes;

/// <summary>
/// 全ての <see cref="Node" /> を入れ子にできる <see cref="Node" /> です。
/// </summary>
public class Container : ContainableNode, IEnumerable<Node>
{
    /// <summary>
    /// Container の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="isTrimmable">範囲外に出た子ノードを描画しないかどうか。</param>
    public Container(bool isTrimmable = false)
    {
        IsTrimmable = isTrimmable;
    }

    /// <summary>
    /// コンテナ内の子ノードの数を取得します。
    /// </summary>
    public int Count => children.Count;

    /// <summary>
    /// 指定されたインデックスの子ノードを取得します。
    /// </summary>
    /// <param name="index">取得する子ノードのインデックス</param>
    public Node this[int index] => children[index];

    /// <summary>
    /// 範囲外に出た子ノードを描画するかどうかを取得または設定します。
    /// </summary>
    public bool IsTrimmable
    {
        get => isTrimmable;
        set => isTrimmable = value;
    }

    /// <summary>
    /// コンテナ内の子ノードを列挙するための列挙子を取得します。
    /// </summary>
    /// <returns>子ノードの列挙子</returns>
    public IEnumerator<Node> GetEnumerator()
    {
        return children.GetEnumerator();
    }

    /// <summary>
    /// コンテナ内の子ノードを列挙するための列挙子を取得します。
    /// </summary>
    /// <returns>子ノードの列挙子</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return children.GetEnumerator();
    }

    /// <summary>
    /// 指定したインデックスに子ノードを挿入します。
    /// </summary>
    /// <param name="index">挿入するインデックス</param>
    /// <param name="node">挿入するノード</param>
    public new void Insert(int index, Node node)
    {
        base.Insert(index, node);
    }

    /// <summary>
    /// 指定したインデックスの子ノードを削除します。
    /// </summary>
    /// <param name="index">削除する子ノードのインデックス</param>
    public void RemoveAt(int index)
    {
        Remove(this[index]);
    }

    /// <summary>
    /// コンテナに子ノードを追加します。
    /// </summary>
    /// <param name="node">追加するノード</param>
    public new void Add(Node node)
    {
        base.Add(node);
    }

    /// <summary>
    /// コンテナに複数の子ノードを追加します。
    /// </summary>
    /// <param name="nodes">追加するノードのコレクション</param>
    public void AddRange(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            Add(node);
    }

    /// <summary>
    /// コンテナに複数の子ノードを追加します。
    /// </summary>
    /// <param name="nodes">追加するノード</param>
    public void AddRange(params Node[] nodes)
    {
        AddRange((IEnumerable<Node>)nodes);
    }

    /// <summary>
    /// コンテナから全ての子ノードを削除します。
    /// </summary>
    public new void Clear()
    {
        base.Clear();
    }

    /// <summary>
    /// 指定したノードがコンテナ内に存在するかどうかを確認します。
    /// </summary>
    /// <param name="node">確認するノード</param>
    /// <returns>ノードが存在する場合は true、それ以外は false</returns>
    public bool Contains(Node node)
    {
        return children.Contains(node);
    }

    /// <summary>
    /// コンテナから指定したノードを削除します。
    /// </summary>
    /// <param name="node">削除するノード</param>
    /// <returns>ノードが正常に削除された場合は true、それ以外は false</returns>
    public new bool Remove(Node node)
    {
        return base.Remove(node);
    }
}
