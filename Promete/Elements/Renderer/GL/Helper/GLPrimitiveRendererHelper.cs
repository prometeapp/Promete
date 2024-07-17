using System;
using System.Drawing;
using System.Numerics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Elements.Renderer.GL.Helper;

public class GLPrimitiveRendererHelper
{
	private uint shader;
	private uint vao, vbo, ebo;

	private readonly OpenGLDesktopWindow window;

	public GLPrimitiveRendererHelper(IWindow window)
	{
		this.window = window as OpenGLDesktopWindow ??
		              throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

		var gl = this.window.GL;
		// --- 頂点シェーダー ---
		var vsh = gl.CreateShader(GLEnum.VertexShader);
		gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.primitive.vert"));
		gl.CompileShader(vsh);

		// --- フラグメントシェーダー ---
		var fsh = gl.CreateShader(GLEnum.FragmentShader);
		gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.primitive.frag"));
		gl.CompileShader(fsh);

		// --- シェーダーを紐付ける ---
		shader = gl.CreateProgram();
		gl.AttachShader(shader, vsh);
		gl.AttachShader(shader, fsh);
		gl.LinkProgram(shader);
		gl.DetachShader(shader, vsh);
		gl.DetachShader(shader, fsh);

		gl.DeleteShader(vsh);
		gl.DeleteShader(fsh);

		// --- VAO ---
		vao = gl.GenVertexArray();

		// --- VBO ---
		vbo = gl.GenBuffer();

		// --- EBO ---
		ebo = gl.GenBuffer();
	}

	public unsafe void Draw(ElementBase el, Span<VectorInt> worldVertices, ShapeType type, Color color, int lineWidth = 0, Color? lineColor = null)
	{
		PrometeApp.Current.ThrowIfNotMainThread();
		if (worldVertices.Length == 0)
			return;

		var gl = window.GL;

		var hw = window.ActualWidth / 2;
		var hh = window.ActualHeight / 2;

		gl.Enable(EnableCap.Blend);
		gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		Span<float> vertices = stackalloc float[worldVertices.Length * 2];

		for (var i = 0; i < worldVertices.Length; i++)
		{
			var vertex = RenderingHelper.Transform(worldVertices[i], el) * window.PixelRatio;

			var (x, y) = vertex.ToViewportPoint(hw, hh);
			vertices[i * 2 + 0] = x;
			vertices[i * 2 + 1] = y;
		}

		if (color.A > 0)
		{
			if (type == ShapeType.Line) gl.LineWidth(lineWidth);
			gl.BindVertexArray(vao);
			gl.BindBuffer(GLEnum.ArrayBuffer, vbo);

			gl.BufferData<float>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);

			// --- レンダリング ---
			gl.UseProgram(shader);

			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
			gl.EnableVertexAttribArray(0);
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");
			gl.Uniform4(uTintColor, new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));

			if (type == ShapeType.Rect)
			{
				gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
				Span<uint> indices = stackalloc uint[]
				{
					0, 1, 2,
					0, 2, 3,
				};
				gl.BufferData<uint>(GLEnum.ElementArrayBuffer, indices, GLEnum.StaticDraw);
				gl.DrawElements(GLEnum.Triangles, (uint)indices.Length, GLEnum.UnsignedInt, null);
			}
			else
			{
				gl.DrawArrays(ToGLType(type), 0, (uint)worldVertices.Length);
			}
		}

		if (lineWidth > 0 && lineColor is { } lc)
		{
			gl.LineWidth(lineWidth);

			gl.BindVertexArray(vao);
			gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
			gl.BufferData<float>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);

			// --- レンダリング ---
			gl.UseProgram(shader);

			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
			gl.EnableVertexAttribArray(0);
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");
			gl.Uniform4(uTintColor, new Vector4(lc.R / 255f, lc.G / 255f, lc.B / 255f, lc.A / 255f));

			gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)worldVertices.Length);
		}

		gl.Disable(EnableCap.Blend);
	}

	private static PrimitiveType ToGLType(ShapeType type)
	{
		return type switch
		{
			ShapeType.Pixel => PrimitiveType.Points,
			ShapeType.Line => PrimitiveType.Lines,
			ShapeType.Rect => PrimitiveType.TriangleStrip,
			ShapeType.Triangle => PrimitiveType.Triangles,
			ShapeType.Polygon => PrimitiveType.TriangleStrip,
			_ => throw new ArgumentException(null, nameof(type)),
		};
	}

	private static Vector Transform(Vector vertex, ElementBase el, Vector? additionalLocation)
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
