﻿using System;
using System.Collections.ObjectModel;
using System.Linq;

#pragma warning disable CS0618 // 型またはメンバーが旧型式です

namespace Promete.Nodes;

public abstract class ContainableNode : Node
{
    [Obsolete("直接このフィールドは操作しないでください。代わりにAdd, Remove, Clear, Insertを使用してください。")]
    protected readonly ObservableCollection<Node> children = [];

    private bool _isSortingRequested = true;
    protected internal bool isTrimmable;
    protected internal Node[] sortedChildren = [];

    protected ContainableNode()
    {
        children.CollectionChanged += (sender, args) => RequestSorting();
    }

    /// <summary>
    ///     ノードのソートを要求します。要求された場合、次のUpdateフレームでソートが行われます。
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

        // 破棄された子ノードを削除
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
        foreach (var child in children) child.UpdateModelMatrix();
    }

    protected void Add(Node node)
    {
        node.Parent?.Remove(node);

        node.Parent = this;
        children.Add(node);
        node.UpdateModelMatrix();
    }

    protected bool Remove(Node node)
    {
        node.Parent = null;
        return children.Remove(node);
    }

    protected void Clear()
    {
        foreach (var child in children) child.Parent = null;
        children.Clear();
        sortedChildren = [];
    }

    protected void Insert(int index, Node node)
    {
        node.Parent?.Remove(this);

        children.Insert(index, node);
        node.Parent = this;
        node.UpdateModelMatrix();
    }
}
