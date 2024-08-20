#pragma warning disable RECS0018 // 等値演算子による浮動小数点値の比較
using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Promete.Elements.Renderer.GL.Helper
{
	/// <summary>
	/// <see cref="Texture2D"/> オブジェクトをバッファ上に描画する機能を提供します。
	/// </summary>
	public class GLTextureRendererHelper
	{
		private readonly uint shader;

		private readonly uint _vbo, _vao, _ebo;

		private readonly Matrix4x4 _view = Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -1.0f));

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

			// X, Y, U, V
			Span<float> vertices =
			[
				0.0f, 0.0f, 0.0f, 0.0f, // 左下
				1.0f, 0.0f, 1.0f, 0.0f, // 右下
				1.0f, 1.0f, 1.0f, 1.0f, // 右上
				0.0f, 1.0f, 0.0f, 1.0f // 左上
			];

			// VAO
			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);

			// VBO
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

			// 頂点位置属性
			gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
			gl.EnableVertexAttribArray(0);

			// テクスチャ座標属性
			gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
			gl.EnableVertexAttribArray(1);

			gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			gl.BindVertexArray(0);

			// EBO
			// _ebo = gl.GenBuffer();
			// Span<uint> indices = [0, 1, 2, 2, 3, 0];
			// gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
			// gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
		}

		public unsafe void Draw(Texture2D texture, ElementBase el, Color? color = null, Vector? pivot = null,
			float? overriddenWidth = null, float? overriddenHeight = null)
		{
			PrometeApp.Current.ThrowIfNotMainThread();
			var gl = window.GL;
			var finalWidth = overriddenWidth ?? texture.Size.X;
			var finalHeight = overriddenHeight ?? texture.Size.Y;
			var modelMatrix =
				Matrix4x4.CreateScale(new Vector3(finalWidth, finalHeight, 1))
				* Matrix4x4.CreateTranslation(new Vector3(-(pivot ?? Vector.Zero).ToNumerics(), 0))
				* el.ModelMatrix;
			var viewMatrix = Matrix4x4.Identity;
			var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, window.ActualWidth, window.ActualHeight, 0, 0.1f, 100f);
			var c = color ?? Color.White;

			gl.Enable(GLEnum.Blend);
			gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

			gl.UseProgram(shader);
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, (uint)texture.Handle);

			var uView = gl.GetUniformLocation(shader, "uView");
			var uModel = gl.GetUniformLocation(shader, "uModel");
			var uProjection = gl.GetUniformLocation(shader, "uProjection");
			var uTexture0 = gl.GetUniformLocation(shader, "uTexture0");
			var uTintColor = gl.GetUniformLocation(shader, "uTintColor");

			gl.UniformMatrix4(uView, 1, false, (float*)&viewMatrix);
			gl.UniformMatrix4(uModel, 1, false, (float*)&modelMatrix);
			gl.UniformMatrix4(uProjection, 1, false, (float*)&projectionMatrix);
			gl.Uniform1(uTexture0, 0);
			gl.Uniform4(uTintColor, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));

			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
			gl.BindVertexArray(0);
		}

		/// <summary>
		/// 指定した頂点の座標を変換します。
		/// </summary>
		/// <param name="el"></param>
		/// <param name="additionalLocation"></param>
		/// <param name="worldVertices"></param>
		/// <returns>座標が一つでも画面内にあれば <see langword="true"/>、そうでなければ <see langword="false"/>。</returns>
		private bool TransformAll(ElementBase el, Vector? additionalLocation, Span<Vector> worldVertices)
		{
			var windowBounds = new Rect(0, 0, window.ActualWidth, window.ActualHeight);
			var isOutside = true;

			var halfWidth = window.ActualWidth / 2;
			var halfHeight = window.ActualHeight / 2;

			for (var i = 0; i < worldVertices.Length; i++)
			{
				worldVertices[i] = RenderingHelper.Transform(worldVertices[i], el, additionalLocation) *
				                   window.PixelRatio;
				if (worldVertices[i].In(windowBounds)) isOutside = false;
				worldVertices[i] = worldVertices[i].ToViewportPoint(halfWidth, halfHeight);
			}

			return !isOutside;
		}
	}
}
