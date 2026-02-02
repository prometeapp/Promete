using System;
using Promete.Graphics;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL;

/// <summary>
/// <see cref="MaskedContainer"/> のレンダリングを行います。
/// </summary>
public class GLMaskedContainerRenderer(
    PrometeApp app,
    IWindow window,
    GLMaskedContainerHelper maskHelper)
    : GLContainbleNodeRenderer(app, window)
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow ??
                                                   throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

    public override void Render(Node node)
    {
        if (node.IsDestroyed) return;
        var container = (MaskedContainer)node;

        // マスクなしの場合は通常のコンテナとして描画
        if (container.MaskTexture is not { } maskTexture)
        {
            base.Render(node);
            return;
        }

        // マスク方式に応じて描画
        if (container.UseAlphaMask)
        {
            RenderWithAlphaMask(container, maskTexture);
        }
        else
        {
            RenderWithStencilMask(container, maskTexture);
        }
    }

    /// <summary>
    /// ステンシルバッファ方式でマスク描画を行います。
    /// </summary>
    private void RenderWithStencilMask(MaskedContainer container, Texture2D maskTexture)
    {
        var gl = _window.GL;

        // 現在のステンシル設定を保存
        var stencilTestEnabled = gl.IsEnabled(GLEnum.StencilTest);

        // ステンシルテストを有効化
        gl.Enable(GLEnum.StencilTest);

        // ステンシルバッファをクリア
        gl.ClearStencil(0);
        gl.Clear(ClearBufferMask.StencilBufferBit);

        // ステンシルバッファへの書き込み設定
        gl.StencilFunc(GLEnum.Always, 1, 0xFF);
        gl.StencilOp(GLEnum.Keep, GLEnum.Keep, GLEnum.Replace);
        gl.StencilMask(0xFF);

        // カラーバッファへの書き込みを無効化
        gl.ColorMask(false, false, false, false);

        // マスクテクスチャをステンシルバッファに描画
        // アルファ値が0.5以上の部分のみステンシルバッファに書き込まれる
        maskHelper.DrawMaskToStencil(maskTexture, container);

        // カラーバッファへの書き込みを有効化
        gl.ColorMask(true, true, true, true);

        // ステンシルテストを「ステンシル=1の場合のみ描画」に設定
        gl.StencilFunc(GLEnum.Equal, 1, 0xFF);
        gl.StencilMask(0x00); // ステンシルバッファへの書き込みを無効化

        // 子要素を描画
        if (container.IsTrimmable) TrimStart(container, gl);

        var sorted = container.sortedChildren.AsSpan();
        foreach (var child in sorted)
        {
            app.RenderNode(child);
        }

        if (container.IsTrimmable) TrimEnd(gl);

        // ステンシルテストを元に戻す
        gl.StencilMask(0xFF);
        if (!stencilTestEnabled)
        {
            gl.Disable(GLEnum.StencilTest);
        }

        // ステンシルバッファをクリア
        gl.ClearStencil(0);
        gl.Clear(ClearBufferMask.StencilBufferBit);
    }

    /// <summary>
    /// アルファブレンディング方式でマスク描画を行います。
    /// </summary>
    private void RenderWithAlphaMask(MaskedContainer container, Texture2D maskTexture)
    {
        // 子要素をテクスチャにレンダリング
        var contentTexture = maskHelper.RenderToTexture(container);

        // マスクを適用して画面に描画
        maskHelper.DrawMasked(contentTexture, maskTexture, container);
    }
}
