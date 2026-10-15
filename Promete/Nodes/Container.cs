using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Promete.Graphics.Rendering;

#pragma warning disable CS0618 // 型またはメンバーが旧型式です

namespace Promete.Nodes;

/// <summary>
/// 全ての <see cref="Node" /> を入れ子にできる <see cref="Node" /> です。
/// </summary>
public class Container : ContainableNode, IEnumerable<Node>
{
    private bool _autoSize;
    private bool _isLayoutDirty;
    private bool _isLayouting;

    /// <summary>
    /// Initializes a new instance of the <see cref="Container"/> class.
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
    /// 子ノードの位置とサイズに応じて、<see cref="Node.Size"/> を自動で設定するかどうかを取得または設定します。 (v2.4~)
    /// </summary>
    /// <remarks>
    /// 有効にすると、コンテナの原点から、子ノードが占める範囲の右下端までがサイズになります。
    /// 子ノードの回転・スケール・ピボットも考慮した外接矩形で計算します。<br />
    /// 子ノードがなければサイズは (0, 0) です。非表示の子ノードも計算に含まれます。<br />
    /// 有効な間は、<see cref="Node.Size"/>, <see cref="Node.Width"/>, <see cref="Node.Height"/> への代入は無視されます。<br />
    /// 子ノードの位置やサイズが変化すると自動的に再計算されます。カスタムノードを作る場合は、<see cref="Node.NotifyLayoutChanged"/> を参照してください。
    /// </remarks>
    public virtual bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize == value)
                return;
            _autoSize = value;
            _isLayoutDirty = true;
            NotifyLayoutChanged();
        }
    }

    /// <summary>
    /// このノードのサイズを取得または設定します。 <see cref="AutoSize"/> が有効な場合、設定は無視されます。
    /// </summary>
    public override VectorInt Size
    {
        get
        {
            FlushLayout();
            return base.Size;
        }
        set
        {
            if (AutoSize)
                return;
            base.Size = value;
        }
    }

    /// <summary>
    /// 範囲外に出た子ノードを描画するかどうかを取得または設定します。
    /// </summary>
    public bool IsTrimmable
    {
        get => base.IsTrimmable;
        set => base.IsTrimmable = value;
    }

    /// <summary>
    /// 指定されたインデックスの子ノードを取得します。
    /// </summary>
    /// <param name="index">取得する子ノードのインデックス</param>
    public Node this[int index] => children[index];

    internal override void Update()
    {
        FlushLayout();
        base.Update();
    }

    internal override void BeforeRender()
    {
        FlushLayout();
        base.BeforeRender();
    }

    internal override void OnChildLayoutChanged(Node? child)
    {
        if (!AutoSize || _isLayouting)
            return;
        _isLayoutDirty = true;

        // 自分のサイズが変わる可能性があるので、祖先の AutoSize にも伝える
        NotifyLayoutChanged();
    }

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (IsTrimmable)
        {
            queue.PushTrim(this, ctx);
            base.Collect(queue, ctx);
            queue.PopTrim();
        }
        else
        {
            base.Collect(queue, ctx);
        }
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

    /// <summary>
    /// 子ノードの変化に伴うレイアウトの再計算を、必要であれば行います。
    /// </summary>
    internal void FlushLayout()
    {
        if (!_isLayoutDirty || _isLayouting)
            return;

        _isLayouting = true;
        try
        {
            _isLayoutDirty = false;
            if (AutoSize)
                OnLayout();
        }
        finally
        {
            _isLayouting = false;
        }
    }

    /// <summary>
    /// <see cref="AutoSize"/> が有効なとき、子ノードの変化に応じてレイアウトを再計算します。
    /// 既定では、子ノードの外接矩形から <see cref="Node.Size"/> を設定します。
    /// </summary>
    internal virtual void OnLayout()
    {
        var right = 0f;
        var bottom = 0f;
        foreach (var child in children)
        {
            GetBounds(child, out _, out var max);
            right = MathF.Max(right, max.X);
            bottom = MathF.Max(bottom, max.Y);
        }

        base.Size = (CeilingWithTolerance(right), CeilingWithTolerance(bottom));
    }

    /// <summary>
    /// 指定したノードが、親の座標系で占める外接矩形 (回転・スケール・ピボット込み) を求めます。
    /// </summary>
    internal static void GetBounds(Node node, out Vector2 min, out Vector2 max)
    {
        var matrix = node.CreateLocalMatrix();
        var size = node.Size;
        Span<Vector2> corners =
        [
            Vector2.Transform(new Vector2(0, 0), matrix),
            Vector2.Transform(new Vector2(size.X, 0), matrix),
            Vector2.Transform(new Vector2(0, size.Y), matrix),
            Vector2.Transform(new Vector2(size.X, size.Y), matrix),
        ];

        min = corners[0];
        max = corners[0];
        foreach (var corner in corners[1..])
        {
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }
    }

    /// <summary>
    /// 回転による浮動小数点誤差で 1px 余分に大きくならないよう、わずかな誤差を許容して切り上げます。
    /// </summary>
    internal static int CeilingWithTolerance(float value) => (int)MathF.Ceiling(value - 1e-3f);
}
