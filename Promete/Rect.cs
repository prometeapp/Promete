namespace Promete
{
	/// <summary>
	/// Rectangle structure.
	/// </summary>
	public struct Rect
	{
		/// <summary>
		/// Get or set the location of this rect.
		/// </summary>
		public Vector Location { get; set; }

		/// <summary>
		/// Get or set the size of this rect.
		/// </summary>
		public Vector Size { get; set; }

		/// <summary>
		/// Get or set the left position of this rect.
		/// </summary>
		public float Left
		{
			get => Location.X;
			set => Location = new Vector(value, Top);
		}

		/// <summary>
		/// Get or set the top position of this rect.
		/// </summary>
		public float Top
		{
			get => Location.Y;
			set => Location = new Vector(Left, value);
		}

		/// <summary>
		/// Get or set the right position of this rect.
		/// </summary>
		public float Right
		{
			get => Left + Width;
			set => Left = value - Width;
		}

		/// <summary>
		/// Get or set the bottom position of this rect.
		/// </summary>
		public float Bottom
		{
			get => Top + Height;
			set => Top = value - Height;
		}

		/// <summary>
		/// Get or set width of this rect.
		/// </summary>
		public float Width
		{
			get => Size.X;
			set => Size = new Vector(value, Height);
		}

		/// <summary>
		/// Get or set height of this rect.
		/// </summary>
		public float Height
		{
			get => Size.Y;
			set => Size = new Vector(Width, value);
		}

		/// <summary>
		/// Initialize a new instance of <see cref="Rect"/> class.
		/// </summary>
		public Rect(Vector location, Vector size)
		{
			Location = location;
			Size = size;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="Rect"/> class.
		/// </summary>
		public Rect(float left, float top, float width, float height)
			: this(new Vector(left, top), new Vector(width, height)) { }

		public void Deconstruct(out float x, out float y, out float width, out float height)
		{
			x = Left;
			y = Top;
			width = Width;
			height = Height;
		}

		public void Deconstruct(out Vector location, out Vector size)
		{
			location = Location;
			size = Size;
		}

		/// <summary>
		/// この矩形と指定された矩形が重なっているかどうかを判定します。
		/// </summary>
		/// <param name="rect">判定する矩形。</param>
		/// <returns>重なっている場合は <see langword="true"/>、それ以外の場合は <see langword="false"/>。</returns>
		public bool Intersect(RectInt rect)
		{
			return Left < rect.Right && Right > rect.Left && Top < rect.Bottom && Bottom > rect.Top;
		}
	}
}
