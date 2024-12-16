using System.Numerics;

namespace Promete.Nodes;

/// <summary>
/// 全てのノードの基底クラスです。
/// </summary>
public abstract class Node
{
    /// <summary>
    /// このノードの名前を取得または設定します。
    /// </summary>
    public virtual string Name { get; set; } = "";

    /// <summary>
    /// このノードの位置を取得または設定します。
    /// </summary>
    public Vector Location
    {
        get => _location;
        set
        {
            if (_location == value) return;
            _location = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードのスケールを取得または設定します。
    /// </summary>
    public Vector Scale
    {
        get => _scale;
        set
        {
            if (_scale == value) return;
            _scale = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードのサイズを取得または設定します。
    /// </summary>
    public virtual VectorInt Size { get; set; }

    /// <summary>
    /// このノードの角度（0-360°）を取得または設定します。
    /// </summary>
    public float Angle
    {
        get => _angle;
        set
        {
            if (_angle == value) return;
            _angle = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードが破棄されたかどうかを取得します。
    /// </summary>
    public bool IsDestroyed { get; private set; }

    /// <summary>
    /// このノードの幅を取得または設定します。
    /// </summary>
    public int Width
    {
        get => Size.X;
        set => Size = (value, Height);
    }

    /// <summary>
    /// このノードの高さを取得または設定します。
    /// </summary>
    public int Height
    {
        get => Size.Y;
        set => Size = (Width, value);
    }

    /// <summary>
    /// このノードの Z インデックスを取得または設定します。<br />
    /// ノードは Z インデックスの昇順に描画されます。よって、大きいほど手前に描画されます。
    /// </summary>
    public int ZIndex
    {
        get => _zIndex;
        set
        {
            if (_zIndex == value) return;
            _zIndex = value;
            Parent?.RequestSorting();
        }
    }

    public Vector AbsoluteLocation =>
        Parent == null ? Location : Location * Parent.AbsoluteScale + Parent.AbsoluteLocation;

    public Vector AbsoluteScale => Parent == null ? Scale : Scale * Parent.AbsoluteScale;
    public float AbsoluteAngle => Parent == null ? Angle : Angle + Parent.AbsoluteAngle;

    /// <summary>
    /// このノードの親ノードを取得します。
    /// </summary>
    public ContainableNode? Parent { get; internal set; }

    internal Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.Identity;

    private float _angle;

    private bool _isModelMatrixDirty = true;

    private Vector _location;
    private Vector _scale = (1, 1);
    private Vector _pivot = Vector.Zero;

    private int _zIndex;

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

    internal void BeforeRender()
    {
        if (_isModelMatrixDirty) UpdateModelMatrix();
        OnRender();
    }

    internal virtual void UpdateModelMatrix()
    {
        var parentMatrix = Parent?.ModelMatrix ?? Matrix4x4.Identity;
        ModelMatrix = Matrix4x4.CreateScale(Scale.X, Scale.Y, 1) *
                      Matrix4x4.CreateRotationZ(MathHelper.ToRadian(Angle)) *
                      Matrix4x4.CreateTranslation(Location.X, Location.Y, 0) *
                      parentMatrix;
        _isModelMatrixDirty = false;
    }

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnRender()
    {
    }

    protected virtual void OnDestroy()
    {
    }
}
