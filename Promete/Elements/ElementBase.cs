using System.Collections.Generic;
using System.Linq;
using Promete.Elements.Components;
using Promete.Internal;

namespace Promete.Elements;

public abstract class ElementBase
{
	/// <summary>
	/// この要素の名前を取得または設定します。
	/// </summary>
	public virtual string Name { get; set; } = "";

	/// <summary>
	/// この要素の位置を取得または設定します。
	/// </summary>
	public virtual Vector Location { get; set; }

	/// <summary>
	/// この要素のスケールを取得または設定します。
	/// </summary>
	public virtual Vector Scale { get; set; } = (1, 1);

	/// <summary>
	/// この要素のサイズを取得または設定します。
	/// </summary>
	public virtual VectorInt Size { get; set; }

	/// <summary>
	/// この要素の角度（0-360）を取得または設定します。
	/// </summary>
	public virtual float Angle { get; set; }

	/// <summary>
	/// この要素が破棄されたかどうかを取得します。
	/// </summary>
	public bool IsDestroyed { get; private set; }

	public int Width
	{
		get => Size.X;
		set => Size = (value, Height);
	}

	public int Height
	{
		get => Size.Y;
		set => Size = (Width, value);
	}

	/// <summary>
	/// この要素の Z インデックスを取得または設定します。<br/>
	/// 要素は Z インデックスの昇順に描画されます。よって、大きいほど手前に描画されます。
	/// </summary>
	public int ZIndex
	{
		get => zIndex;
		set
		{
			if (zIndex == value) return;
			zIndex = value;
			Parent?.RequestSorting();
		}
	}

	public Vector AbsoluteLocation => Parent == null ? Location : Location * Parent.AbsoluteScale + Parent.AbsoluteLocation;
	public Vector AbsoluteScale => Parent == null ? Scale : Scale * Parent.AbsoluteScale;
	public float AbsoluteAngle => Parent == null ? Angle : Angle + Parent.AbsoluteAngle;

	public ContainableElementBase? Parent { get; internal set; }

	private int zIndex;

	public void Destroy()
	{
		if (IsDestroyed) return;
		IsDestroyed = true;
		OnDestroy();
	}

	internal virtual void Update()
	{
		OnUpdate();
	}

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnDestroy()
	{
	}
}
