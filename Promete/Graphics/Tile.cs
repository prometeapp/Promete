using System;
using Promete.Nodes;
using Promete.Windowing;

namespace Promete.Graphics;

/// <summary>
/// 画像ファイルを表示するタイルです。
/// </summary>
public class Tile : ITile
{
    private readonly bool _textureIsInternal;

    private int _animationState;
    private long _prevFrameCount = -1;
    private double _timer;

    /// <summary>
    /// テクスチャを指定して、<see cref="Tile" /> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="texture">タイルとして描画されるテクスチャ。</param>
    public Tile(Texture2D texture)
        : this(texture, false)
    {
    }

    /// <summary>
    /// <see cref="Tile" /> クラスの新しいインスタンスを初期化します。
    /// </summary>
    protected Tile(Texture2D texture, bool b1)
        : this(new[] { texture }, 0)
    {
        _textureIsInternal = b1;
    }

    /// <summary>
    /// テクスチャの配列とアニメーション時間を指定して、<see cref="Tile" /> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="animations">アニメーション描画されるテクスチャの配列。</param>
    /// <param name="interval">アニメーションの時間。</param>
    /// <exception cref="ArgumentNullException">animations が <c>null</c> です。</exception>
    /// <exception cref="ArgumentException">animations の長さが <c>0</c> です。</exception>
    public Tile(Texture2D[] animations, double interval)
    {
        if (animations.Length < 1)
            throw new ArgumentException(null, nameof(animations));
        Animations = animations;
        Interval = interval;
        Texture = Animations[0];
    }

    /// <summary>
    /// 描画されるテクスチャを取得します。
    /// </summary>
    public Texture2D Texture { get; private set; }

    /// <summary>
    /// アニメーションに使われるテクスチャの配列を取得します。
    /// </summary>
    public Texture2D[] Animations { get; }

    /// <summary>
    /// アニメーションにおけるテクスチャ1枚あたりの描画時間を取得します。
    /// </summary>
    public double Interval { get; }

    public Texture2D GetTexture(Tilemap map, VectorInt tileLocation, IWindow window)
    {
        if (_prevFrameCount != window.TotalFrame)
        {
            if (_timer > Interval)
            {
                _animationState++;
                if (_animationState >= Animations.Length)
                    _animationState = 0;
                _timer = 0;
            }

            Texture = Animations[_animationState];
            _timer += window.DeltaTime;
        }

        _prevFrameCount = window.TotalFrame;
        return Texture;
    }

    /// <summary>
    /// この <see cref="Tile" /> を削除します。
    /// </summary>
    public void Destroy()
    {
        if (_textureIsInternal)
            Texture.Dispose();
    }
}
