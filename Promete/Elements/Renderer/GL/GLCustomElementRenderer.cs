﻿using System;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Elements.Renderer.GL;

public class GLCustomElementRenderer(PrometeApp app, IWindow window) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var el = (CustomElement)element;
		var w = window as OpenGLDesktopWindow ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

		if (el.isTrimmable) TrimStart(el, w.GL);
		for (var i = 0; i < el.children.Count; i++)
		{
			app.RenderElement(el.children[i]);
		}
		if (el.isTrimmable) TrimEnd(w.GL);
	}

	private void TrimStart(CustomElement el, Silk.NET.OpenGL.GL gl)
	{
		gl.Enable(GLEnum.ScissorTest);
		var left = (VectorInt)el.AbsoluteLocation.ToDeviceCoord();
		var size = (VectorInt)(el.Size * el.AbsoluteScale).ToDeviceCoord();

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