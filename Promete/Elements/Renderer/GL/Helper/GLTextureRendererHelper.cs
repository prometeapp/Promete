#pragma warning disable RECS0018 // 等値演算子による浮動小数点値の比較
using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Elements.Renderer.GL.Helper
{
	/// <summary>
	/// <see cref="Texture2D"/> オブジェクトをバッファ上に描画する機能を提供します。
	/// </summary>
	public class GLTextureRendererHelper
	{
		private uint shader;

		private uint vbo, vao, ebo;

		private readonly OpenGLDesktopWindow window;

		public GLTextureRendererHelper(IWindow window)
		{
			this.window = window as OpenGLDesktopWindow ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

			var gl = this.window.GL;

			// --- 頂点シェーダー ---
			var vsh = gl.CreateShader(GLEnum.VertexShader);
			gl.ShaderSource(vsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.texture.vert"));
			gl.CompileShader(vsh);

			// --- フラグメントシェーダー ---
			var fsh = gl.CreateShader(GLEnum.FragmentShader);
			gl.ShaderSource(fsh, EmbeddedResource.GetResourceAsString("Promete.Resources.shaders.texture.frag"));
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

		/// <summary>
		/// テクスチャを描画します。
		/// </summary>
		public unsafe void Draw(Texture2D texture, ElementBase el, Color? color = null, Vector? additionalLocation = null, float? overriddenWidth = null, float? overriddenHeight = null)
		{
			PrometeApp.Current.ThrowIfNotMainThread();
			var finalAngle = el.AbsoluteAngle;
			var finalScale = el.AbsoluteScale;

			var finalWidth = overriddenWidth ?? el.Width;
			var finalHeight = overriddenHeight ?? el.Height;

			Span<Vector> worldVertices =
			[
				(finalWidth, 0),
				(finalWidth, finalHeight),
				(0, finalHeight),
				(0, 0),
			];

			var gl = window.GL;
			var bb = new Rect(0, 0, window.ActualWidth, window.ActualHeight);
			var isOutside = true;

			var halfWidth = window.ActualWidth / 2;
			var halfHeight = window.ActualHeight / 2;

			for (var i = 0; i < worldVertices.Length; i++)
			{
				worldVertices[i] = RenderingHelper.Transform(worldVertices[i], el, additionalLocation) *
				                   window.PixelRatio;
				if (worldVertices[i].In(bb)) isOutside = false;
				worldVertices[i] = worldVertices[i].ToViewportPoint(halfWidth, halfHeight);
			}
			// どの頂点も画面内になければ描画しない
			if (isOutside) return;

			gl.BindVertexArray(vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

			Span<float> vertices =
			[
				worldVertices[0].X, worldVertices[0].Y, 1f, 0f,
				worldVertices[1].X, worldVertices[1].Y, 1f, 1f,
				worldVertices[2].X, worldVertices[2].Y, 0f, 1f,
				worldVertices[3].X, worldVertices[3].Y, 0f, 0f,
			];
			gl.BufferData<float>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);

			Span<uint> indices =
			[
				0, 1, 3,
				1, 2, 3,
			];
			gl.BufferData<uint>(GLEnum.ElementArrayBuffer, indices, GLEnum.StaticDraw);

			// --- レンダリング ---
			gl.Enable(GLEnum.Blend);
			gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(0 * sizeof(float)));
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);
			gl.BindVertexArray(vao);

			gl.UseProgram(shader);
			gl.ActiveTexture(GLEnum.Texture0);
			gl.BindTexture(GLEnum.Texture2D, (uint)texture.Handle);
			var uTexture0 = gl.GetUniformLocation(shader, "uTexture0");
			gl.Uniform1(uTexture0, 0);
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");
			var c = color ?? Color.White;
			gl.Uniform4(uTintColor, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));
			gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, (void*)0);
		}
	}
}
