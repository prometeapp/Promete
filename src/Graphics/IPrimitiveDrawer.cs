using System.Drawing;

namespace Promete
{
	public interface IPrimitiveDrawer
	{
		void Draw(Vector originLocation, Vector originScale, VectorInt[] vertices, ShapeType type, Color color, int lineWidth = 0, Color? lineColor = null);
	}
}
