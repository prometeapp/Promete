namespace Promete.Elements.Renderer;

public static class RenderingHelper
{
	public static Vector Transform(Vector vertex, ElementBase el, Vector? additionalLocation = null)
	{
		vertex = vertex
			.Translate(additionalLocation ?? (0, 0))
			.Rotate(MathHelper.ToRadian(el.Angle))
			.Scale(el.Scale)
			.Translate(el.Location);
		var parent = el.Parent;
		while (parent != null)
		{
			vertex = vertex
				.Rotate(MathHelper.ToRadian(parent.Angle))
				.Scale(parent.Scale)
				.Translate(parent.Location);
			parent = parent.Parent;
		}

		return vertex;
	}
}
