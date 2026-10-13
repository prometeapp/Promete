using System;
using System.Numerics;
using Promete.Graphics;
using Promete.Graphics.Rendering;

namespace Promete.Nodes;

/// <summary>
/// 全てのノードの基底クラスです。
/// </summary>
public abstract class Node
{
    private Angle _angle;

    private bool _isModelMatrixDirty = true;

    private bool _isPixelSnapEnabled = true;

    private Vector _location;
    private Vector _pivot = Vector.Zero;
    private Vector _scale = (1, 1);
    private VectorInt _size;

    private int _zIndex;

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
            if (_location == value)
                return;
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
            if (_scale == value)
                return;
            _scale = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードのサイズを取得または設定します。
    /// </summary>
    public virtual VectorInt Size
    {
        get => _size;
        set
        {
            if (_size == value)
                return;
            _size = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードの角度を取得または設定します。
    /// </summary>
    public Angle Angle
    {
        get => _angle;
        set
        {
            if (_angle == value)
                return;
            _angle = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードが破棄されたかどうかを取得します。
    /// </summary>
    public bool IsDestroyed { get; private set; }

    /// <summary>
    /// このノードに適用するマテリアルを取得または設定します。使用されない場合もあります。
    /// null の場合はデフォルトシェーダーで描画します。
    /// </summary>
    public Material? Material { get; set; }

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
            if (_zIndex == value)
                return;
            _zIndex = value;
            Parent?.RequestSorting();
        }
    }

    /// <summary>
    /// このノードの、描画時の中心点（ピボット）を取得または設定します。
    /// </summary>
    /// <remarks>
    /// 単位は、このノードの幅・高さを 1 とした相対座標です。<br />
    /// 幅・高さのいずれかが 0 の場合、中心点は無効化されます。
    /// </remarks>
    public Vector Pivot
    {
        get => _pivot;
        set
        {
            if (_pivot == value)
                return;
            _pivot = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードの描画位置をピクセル単位にスナップするかどうかを取得または設定します。
    /// </summary>
    /// <remarks>
    /// 有効の場合、モデル行列の平行移動成分が整数に丸められ、ピボットや位置の端数によるにじみを防ぎます。<br />
    /// 回転や非整数スケールを伴うアニメーションでガタつきが生じる場合は、無効にしてください。
    /// </remarks>
    public bool IsPixelSnapEnabled
    {
        get => _isPixelSnapEnabled;
        set
        {
            if (_isPixelSnapEnabled == value)
                return;
            _isPixelSnapEnabled = value;
            _isModelMatrixDirty = true;
        }
    }

    /// <summary>
    /// このノードを描画するかどうかを取得または設定します。
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// このノードの絶対位置（ピボット点の、画面上での座標）を取得します。
    /// </summary>
    /// <remarks>
    /// 親ノードの位置・回転・スケール・ピボットをすべて反映します。<br />
    /// <see cref="ModelMatrix"/> と違い、ピクセルスナップは適用されず、描画前でも最新の値を返します。
    /// </remarks>
    public Vector AbsoluteLocation
    {
        get
        {
            var p = Vector2.Transform(
                new Vector2(Pivot.X * Size.X, Pivot.Y * Size.Y),
                CreateWorldMatrix()
            );
            return (p.X, p.Y);
        }
    }

    /// <summary>
    /// このノードの絶対スケール（親ノードのスケールを考慮したスケール）を取得します。
    /// </summary>
    /// <remarks>
    /// 親が反転（負のスケール）していれば、結果にも反映されます。<br />
    /// 回転を挟んだ非均一スケールは剪断になるため、近似値になります。
    /// </remarks>
    public Vector AbsoluteScale
    {
        get
        {
            Decompose(CreateWorldMatrix(), out var scale, out _);
            return scale;
        }
    }

    /// <summary>
    /// このノードの絶対角度（親ノードの角度を考慮した角度）を取得します。
    /// </summary>
    /// <remarks>
    /// 親が反転していれば、回転の向きも反転します。<br />
    /// 値は -180° 〜 180° の範囲に正規化されます。
    /// </remarks>
    public Angle AbsoluteAngle
    {
        get
        {
            Decompose(CreateWorldMatrix(), out _, out var angle);
            return angle;
        }
    }

    /// <summary>
    /// このノードの親ノードを取得します。
    /// </summary>
    public ContainableNode? Parent { get; internal set; }

    /// <summary>
    /// 位置・回転・スケール・ピボットを合成した変換行列を取得します。親ノードの変形も含みます。
    /// カスタムノードの <see cref="Collect"/> で描画コマンドに渡すために使用します。
    /// </summary>
    public Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.Identity;

    /// <summary>
    /// このノードを破棄します。
    /// </summary>
    public void Destroy()
    {
        if (IsDestroyed)
            return;
        IsDestroyed = true;
        OnDestroy();
    }

    internal virtual void Update()
    {
        if (IsDestroyed)
            return;
        OnUpdate();
    }

    internal virtual void BeforeRender()
    {
        if (_isModelMatrixDirty)
            UpdateModelMatrix();
        OnPreRender();
        OnRender();
    }

    /// <summary>
    /// このノードのレンダリングコマンドをキューに収集します。
    /// </summary>
    /// <param name="queue">コマンドの収集先キュー。</param>
    /// <param name="ctx">レンダリングコンテキスト。</param>
    public virtual void Collect(RenderCommandQueue queue, RenderContext ctx) { }

    protected internal virtual void UpdateModelMatrix()
    {
        var parentMatrix = Parent?.ModelMatrix ?? Matrix4x4.Identity;
        var matrix = CreateLocalMatrix() * parentMatrix;

        if (IsPixelSnapEnabled)
        {
            matrix.M41 = MathF.Round(matrix.M41);
            matrix.M42 = MathF.Round(matrix.M42);
        }

        ModelMatrix = matrix;
        _isModelMatrixDirty = false;
    }

    /// <summary>
    /// 親を含まない、このノード単体の変換行列を作成します。
    /// </summary>
    private Matrix4x4 CreateLocalMatrix() =>
        Matrix4x4.CreateTranslation(-Pivot.X * Size.X, -Pivot.Y * Size.Y, 0)
        * Matrix4x4.CreateScale(Scale.X, Scale.Y, 1)
        * Matrix4x4.CreateRotationZ(Angle.ToRadians())
        * Matrix4x4.CreateTranslation(Location.X, Location.Y, 0);

    /// <summary>
    /// 祖先まで辿って、ピクセルスナップなしのワールド変換行列を作成します。
    /// キャッシュされた <see cref="ModelMatrix"/> は描画前だと古いため、Absolute 系プロパティはこちらを使います。
    /// </summary>
    private Matrix4x4 CreateWorldMatrix() =>
        CreateLocalMatrix() * (Parent?.CreateWorldMatrix() ?? Matrix4x4.Identity);

    /// <summary>
    /// 変換行列を、スケールと角度に分解します。
    /// </summary>
    private static void Decompose(in Matrix4x4 m, out Vector scale, out Angle angle)
    {
        var sx = MathF.Sqrt((m.M11 * m.M11) + (m.M12 * m.M12));

        // 回転なしなら対角成分がそのままスケールになる。(-1, 1) のような反転も、そのまま返せる。
        // X のスケールが 0 の場合は角度が決まらないので、同じ扱いにする。
        if ((m.M12 == 0 && m.M21 == 0) || sx == 0)
        {
            scale = (m.M11, m.M22);
            angle = Angle.Zero;
            return;
        }

        // 回転ありの場合、反転は Y に寄せる（(-1, 1) と (1, -1) + 180° は同じ変換）。
        var det = (m.M11 * m.M22) - (m.M12 * m.M21);
        scale = (sx, det / sx);
        angle = Angle.FromRadians(MathF.Atan2(m.M12, m.M11));
    }

    protected virtual void OnUpdate() { }

    protected virtual void OnRender() { }

    protected virtual void OnPreRender() { }

    protected virtual void OnDestroy() { }
}
