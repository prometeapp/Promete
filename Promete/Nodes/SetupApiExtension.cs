using System.Collections.Generic;

namespace Promete.Nodes;

/// <summary>
/// Setup API向けの <see cref="Node" /> の拡張
/// </summary>
public static class SetupApiExtension
{
    /// <summary>
    /// ノードの位置をベクトルで設定します
    /// </summary>
    /// <returns></returns>
    public static T Location<T>(this T node, Vector vec)
        where T : Node
    {
        node.Location = vec;
        return node;
    }

    /// <summary>
    /// ノードの位置を X, Y 座標で設定します
    /// </summary>
    /// <returns></returns>
    public static T Location<T>(this T node, float x, float y)
        where T : Node
    {
        node.Location = (x, y);
        return node;
    }

    /// <summary>
    /// ノードの角度を設定します
    /// </summary>
    /// <returns></returns>
    public static T Angle<T>(this T node, Angle angle)
        where T : Node
    {
        node.Angle = angle;
        return node;
    }

    /// <summary>
    /// ノードのスケールをベクトルで設定します
    /// </summary>
    /// <returns></returns>
    public static T Scale<T>(this T node, Vector vec)
        where T : Node
    {
        node.Scale = vec;
        return node;
    }

    /// <summary>
    /// ノードのスケールを X, Y 値で設定します
    /// </summary>
    /// <returns></returns>
    public static T Scale<T>(this T node, float x, float y)
        where T : Node
    {
        node.Scale = (x, y);
        return node;
    }

    /// <summary>
    /// ノードのサイズをベクトルで設定します
    /// </summary>
    /// <returns></returns>
    public static T Size<T>(this T node, VectorInt vec)
        where T : Node
    {
        node.Size = vec;
        return node;
    }

    /// <summary>
    /// ノードのサイズを幅と高さで設定します
    /// </summary>
    /// <returns></returns>
    public static T Size<T>(this T node, int width, int height)
        where T : Node
    {
        node.Size = (width, height);
        return node;
    }

    /// <summary>
    /// ノードの名前を設定します
    /// </summary>
    /// <returns></returns>
    public static T Name<T>(this T node, string name)
        where T : Node
    {
        node.Name = name;
        return node;
    }

    /// <summary>
    /// コンテナに子ノードを追加します
    /// </summary>
    /// <returns></returns>
    public static T Children<T>(this T node, params Node[] children)
        where T : Container
    {
        node.AddRange(children);
        return node;
    }

    /// <summary>
    /// コンテナに子ノードのコレクションを追加します
    /// </summary>
    /// <returns></returns>
    public static T Children<T>(this T node, IEnumerable<Node> children)
        where T : Container
    {
        node.AddRange(children);
        return node;
    }

    /// <summary>
    /// ノードのZ軸インデックスを設定します
    /// </summary>
    /// <returns></returns>
    public static T ZIndex<T>(this T node, int zIndex)
        where T : Node
    {
        node.ZIndex = zIndex;
        return node;
    }

    /// <summary>
    /// ノードのピボット位置をベクトルで設定します
    /// </summary>
    /// <returns></returns>
    public static T Pivot<T>(this T node, Vector pivot)
        where T : Node
    {
        node.Pivot = pivot;
        return node;
    }

    /// <summary>
    /// ノードのピボット位置を X, Y 座標で設定します
    /// </summary>
    /// <returns></returns>
    public static T Pivot<T>(this T node, float x, float y)
        where T : Node
    {
        node.Pivot = (x, y);
        return node;
    }

    /// <summary>
    /// ノードのピボット位置を水平・垂直アライメントで設定します
    /// </summary>
    /// <returns></returns>
    public static T Pivot<T>(
        this T node,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment
    )
        where T : Node
    {
        var x = horizontalAlignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => 0.5f,
            HorizontalAlignment.Right => 1,
            _ => 0,
        };
        var y = verticalAlignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => 0.5f,
            VerticalAlignment.Bottom => 1,
            _ => 0,
        };
        node.Pivot = (x, y);
        return node;
    }

    /// <summary>
    /// ノードの描画位置をピクセル単位にスナップするかどうかを設定します。
    /// </summary>
    /// <param name="node">対象の<see cref="Node"/>。</param>
    /// <param name="enabled">値。</param>
    /// <typeparam name="T"><see cref="Node"/>の具象型。</typeparam>
    /// <returns><see cref="Node"/></returns>
    public static T PixelSnap<T>(this T node, bool enabled)
        where T : Node
    {
        node.IsPixelSnapEnabled = enabled;
        return node;
    }
}
