#pragma warning disable RECS0018 // 等値演算子による浮動小数点値の比較
using System;
using System.Drawing;
using System.Numerics;
using Promete.Exceptions;
using Promete.Graphics;
using Promete.Graphics.GL;
using Promete.Internal;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Elements.Renderer.GL.Helper
{
	/// <summary>
	/// <see cref="GLTexture2D"/> オブジェクトをバッファ上に描画する機能を提供します。
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
			gl.BindVertexArray(vao);

			// --- VBO ---
			vbo = gl.GenBuffer();
			gl.BindBuffer(GLEnum.ArrayBuffer, vbo);

			// --- EBO ---
			ebo = gl.GenBuffer();
			gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
		}

		/// <summary>
		/// テクスチャを描画します。
		/// </summary>
		public unsafe void Draw(ITexture texture, Vector location, Vector scale, Color? color = null, float? width = null, float? height = null, float angle = 0)
		{
			if (texture is not GLTexture2D glTexture) return;

			if (glTexture.IsDisposed)
			{
				throw new TextureDisposedException();
			}

			location = location.ToDeviceCoord();
			scale = scale.ToDeviceCoord();

			var matrix = Matrix4x4.Identity * Matrix4x4.CreateFromYawPitchRoll(0, 0, angle);

			var w = width ?? texture.Size.X;
			var h = height ?? texture.Size.Y;

			w *= scale.X;
			h *= scale.Y;

			var (left, top) = location;
			var right = left + w;
			var bottom = top + h;

			var gl = window.GL;

			// カリング
			if (left > window.ActualWidth || top > window.ActualHeight || right < 0 || bottom < 0)
				return;

			var hw = window.ActualWidth / 2;
			var hh = window.ActualHeight / 2;

			var (x0, y0) = (right, top).ToViewportPoint(hw, hh);
			var (x1, y1) = (right, bottom).ToViewportPoint(hw, hh);
			var (x2, y2) = (left, bottom).ToViewportPoint(hw, hh);
			var (x3, y3) = (left, top).ToViewportPoint(hw, hh);

			var vertices = stackalloc float[]
			{
				x0, y0, 1f, 0f,
				x1, y1, 1f, 1f,
				x2, y2, 0f, 1f,
				x3, y3, 0f, 0f,
			};
			const uint verticesSize = 4 * 4;
			gl.BufferData(GLEnum.ArrayBuffer, verticesSize * sizeof(float), vertices, GLEnum.StaticDraw);

			var indices = stackalloc uint[]
			{
				0, 1, 3,
				1, 2, 3,
			};
			uint indicesSize = 3 * 2;
			gl.BufferData(GLEnum.ElementArrayBuffer, indicesSize * sizeof(uint), indices, GLEnum.StaticDraw);

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
			gl.BindTexture(GLEnum.Texture2D, (uint)glTexture.Handle);
			var uModel = gl.GetUniformLocation(shader, "uModel");
			gl.UniformMatrix4(uModel, 1, false, (float*)&matrix);
			var uTexture0 = gl.GetUniformLocation(shader, "uTexture0");
			gl.Uniform1(uTexture0, 0);
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");
			var c = color ?? Color.White;
			gl.Uniform4(uTintColor, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));
			gl.DrawElements(GLEnum.Triangles, indicesSize, GLEnum.UnsignedInt, null);
		}
	}
}
