using System;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Elements.Renderer.GL;

public class GLContainerRenderer(PrometeApp app, IWindow window) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var container = (Container)element;
		var w = window as OpenGLDesktopWindow ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

		if (container.IsTrimmable) TrimStart(container, w.GL);
		for (var i = 0; i < container.Count; i++)
		{
			app.Render(container[i]);
		}
		if (container.IsTrimmable) TrimEnd(w.GL);
	}

	private void TrimStart(Container container, Silk.NET.OpenGL.GL gl)
	{
		gl.Enable(GLEnum.ScissorTest);
		var left = (VectorInt)container.AbsoluteLocation.ToDeviceCoord();
		var size = (VectorInt)(container.Size * container.AbsoluteScale).ToDeviceCoord();

		if (left.X < 0) left.X = 0;
		if (left.Y < 0) left.Y = 0;

		if (left.X + size.X > window.ActualWidth)
			size.X = left.X + size.X - window.ActualWidth;

		if (left.Y + size.Y > window.ActualHeight)
			size.Y = left.Y + size.Y - window.ActualHeight;

		left.Y = window.ActualHeight - left.Y - size.Y;

		gl.Scissor(left.X, left.Y, (uint)size.X, (uint)size.Y);
	}

	private void TrimEnd(Silk.NET.OpenGL.GL gl)
	{
		gl.Scissor(0, 0, (uint)window.ActualWidth, (uint)window.ActualHeight);
		gl.Disable(GLEnum.ScissorTest);
	}
}
