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
		gl.BindVertexArray(vao);

		// --- VBO ---
		vbo = gl.GenBuffer();
		gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
		// --- EBO ---
		ebo = gl.GenBuffer();
		gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
	}

	public unsafe void Draw(Vector originLocation, Vector originScale, VectorInt[] vertices, ShapeType type,
		Color color, int lineWidth = 0, Color? lineColor = null)
	{
		if (vertices.Length == 0)
			return;

		var gl = window.GL;

		var hw = window.ActualWidth / 2;
		var hh = window.ActualHeight / 2;

		gl.Enable(EnableCap.Blend);
		gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		var v = stackalloc float[vertices.Length * 2];

		for (var i = 0; i < vertices.Length; i++)
		{
			var dest = originLocation + vertices[i] * originScale;
			var (x, y) = dest.ToDeviceCoord().ToViewportPoint(hw, hh);
			v[i * 2 + 0] = x;
			v[i * 2 + 1] = y;
		}

		if (color.A > 0)
		{
			if (type == ShapeType.Line) gl.LineWidth(lineWidth);
			gl.BufferData(GLEnum.ArrayBuffer, (uint)vertices.Length * 2 * sizeof(float), v, GLEnum.StaticDraw);

			// --- レンダリング ---
			gl.UseProgram(shader);

			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
			gl.EnableVertexAttribArray(0);
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");
			gl.Uniform4(uTintColor, new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));

			if (type == ShapeType.Rect)
			{
				var indices = stackalloc uint[]
				{
					0, 1, 2,
					0, 2, 3,
				};
				uint indicesSize = 3 * 2;
				gl.BufferData(GLEnum.ElementArrayBuffer, indicesSize * sizeof(uint), indices, GLEnum.StaticDraw);
				gl.DrawElements(GLEnum.Triangles, indicesSize, GLEnum.UnsignedInt, null);
			}
			else
			{
				gl.DrawArrays(ToGLType(type), 0, (uint)vertices.Length);
			}
		}

		if (lineWidth > 0 && lineColor is { } lc)
		{
			gl.LineWidth(lineWidth);
			gl.BufferData(GLEnum.ArrayBuffer, (uint)vertices.Length * 2 * sizeof(float), v, GLEnum.StaticDraw);

			// --- レンダリング ---
			gl.UseProgram(shader);

			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)(0 * sizeof(float)));
			gl.EnableVertexAttribArray(0);
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");
			gl.Uniform4(uTintColor, new Vector4(lc.R / 255f, lc.G / 255f, lc.B / 255f, lc.A / 255f));

			gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)vertices.Length);
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
}
