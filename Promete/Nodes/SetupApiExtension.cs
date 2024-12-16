using System.Collections.Generic;

namespace Promete.Nodes;

/// <summary>
/// Setup API向けの <see cref="Node" /> の拡張
/// </summary>
public static class SetupApiExtension
{
    public static T Location<T>(this T node, Vector vec) where T : Node
    {
        node.Location = vec;
        return node;
    }

    public static T Location<T>(this T node, float x, float y) where T : Node
    {
        node.Location = (x, y);
        return node;
    }

    public static T Angle<T>(this T node, float angle) where T : Node
    {
        node.Angle = angle;
        return node;
    }

    public static T Scale<T>(this T node, Vector vec) where T : Node
    {
        node.Scale = vec;
        return node;
    }

    public static T Scale<T>(this T node, float x, float y) where T : Node
    {
        node.Scale = (x, y);
        return node;
    }

    public static T Size<T>(this T node, VectorInt vec) where T : Node
    {
        node.Size = vec;
        return node;
    }

    public static T Size<T>(this T node, int width, int height) where T : Node
    {
        node.Size = (width, height);
        return node;
    }

    public static T Name<T>(this T node, string name) where T : Node
    {
        node.Name = name;
        return node;
    }

    public static T Children<T>(this T node, params Node[] children) where T : Container
    {
        node.AddRange(children);
        return node;
    }

    public static T Children<T>(this T node, IEnumerable<Node> children) where T : Container
    {
        node.AddRange(children);
        return node;
    }

    public static T ZIndex<T>(this T node, int zIndex) where T : Node
    {
        node.ZIndex = zIndex;
        return node;
    }

    public static T Pivot<T>(this T node, Vector pivot) where T : Node
    {
        node.Pivot = pivot;
        return node;
    }

    public static T Pivot<T>(this T node, float x, float y) where T : Node
    {
        node.Pivot = (x, y);
        return node;
    }

    public static T Pivot<T>(this T node, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment) where T : Node
    {
        var x = horizontalAlignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => 0.5f,
            HorizontalAlignment.Right => 1,
            _ => 0
        };
        var y = verticalAlignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => 0.5f,
            VerticalAlignment.Bottom => 1,
            _ => 0
        };
        node.Pivot = (x, y);
        return node;
    }
}
