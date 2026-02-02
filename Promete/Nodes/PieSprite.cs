using System;
using System.Drawing;
using Promete.Graphics;

namespace Promete.Nodes;

/// <summary>
/// テクスチャを扇状（円グラフ状）に描画するスプライトクラスです。
/// </summary>
public class PieSprite(Texture2D? texture = null, Color? tintColor = default) : Sprite(texture, tintColor)
{
    private float _startPercent = 0.0f;
    private float _percent = 0.0f;

    /// <summary>
    /// 描画開始位置をパーセントで取得または設定します（0.0 ~ 100.0）。
    /// 0%は真上（12時方向）を示します。
    /// </summary>
    public float StartPercent
    {
        get => _startPercent;
        set => _startPercent = Math.Clamp(value, 0.0f, 100.0f);
    }

    /// <summary>
    /// 描画終了位置をパーセントで取得または設定します（0.0 ~ 100.0）。
    /// 0%は真上（12時方向）を示します。絶対的な終了位置です。
    /// </summary>
    public float Percent
    {
        get => _percent;
        set => _percent = Math.Clamp(value, 0.0f, 100.0f);
    }
}
