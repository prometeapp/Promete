using System;

namespace Promete.Graphics;

/// <summary>
/// 画像ファイルを表示するタイルです。
/// </summary>
public class Tile : ITile
{
	/// <summary>
	/// 描画されるテクスチャを取得します。
	/// </summary>
	public ITexture Texture { get; private set; }

	/// <summary>
	/// アニメーションに使われるテクスチャの配列を取得します。
	/// </summary>
	public ITexture[] Animations { get; private set; }

	/// <summary>
	/// アニメーションにおけるテクスチャ1枚あたりの描画時間を取得します。
	/// </summary>
	public double Interval { get; private set; }

	private readonly bool textureIsInternal;

	/// <summary>
	/// テクスチャを指定して、<see cref="Tile"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="texture">タイルとして描画されるテクスチャ。</param>
	public Tile(ITexture texture)
		: this(texture, false)
	{
	}

	/// <summary>
	/// <see cref="Tile"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	protected Tile(ITexture texture, bool b1)
		: this(new[] { texture }, 0)
	{
		textureIsInternal = b1;
	}

	/// <summary>
	/// テクスチャの配列とアニメーション時間を指定して、<see cref="Tile"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="animations">アニメーション描画されるテクスチャの配列。</param>
	/// <param name="interval">アニメーションの時間。</param>
	/// <exception cref="ArgumentNullException">animations が <c>null</c> です。</exception>
	/// <exception cref="ArgumentException">animations の長さが <c>0</c> です。</exception>
	public Tile(ITexture[] animations, double interval)
	{
		if (animations.Length < 1)
			throw new ArgumentException(null, nameof(animations));
		Animations = animations;
		Interval = interval;
		Texture = Animations[0];
	}

	// public void Draw(Tilemap map, VectorInt _, Vector locationToDraw, Color? color)
	// {
	// 	if (prevFrameCount != DF.Window.TotalFrame)
	// 	{
	// 		if (timer > Interval)
	// 		{
	// 			animationState++;
	// 			if (animationState >= Animations.Length)
	// 				animationState = 0;
	// 			timer = 0;
	// 		}
	//
	// 		Texture = Animations[animationState];
	// 		timer += Time.DeltaTime;
	// 	}
	// 	prevFrameCount = DF.Window.TotalFrame;
	// 	DF.TextureDrawer.Draw(Texture, locationToDraw, map.AbsoluteScale, color);
	// }

	/// <summary>
	/// この <see cref="Tile"/> を削除します。
	/// </summary>
	public void Destroy()
	{
		if (textureIsInternal)
			Texture.Dispose();
	}

	private int animationState;
	private double timer;
	private long prevFrameCount = -1;
}
