using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Promete.Graphics.Rendering;
using Promete.Nodes;

namespace Promete.Graphics;

/// <summary>
/// 子要素をテクスチャにレンダリングできる要素です。
/// </summary>
public class FrameBuffer : IEnumerable<Node>, IDisposable
{
    private bool _disposed;

    private VectorInt _size;

    private readonly Container _children = [];

    private readonly FrameBufferManager _frameBufferManager;
    private readonly RenderTexture _renderTexture;

    /// <summary>
    /// 指定したサイズの <see cref="FrameBuffer"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="width">フレームバッファの幅。</param>
    /// <param name="height">フレームバッファの高さ。</param>
    public FrameBuffer(int width, int height)
    {
        _size = (width, height);

        var provider = PrometeApp.Current.TryGetPlugin<IRenderTextureProvider>(out var p)
            ? p
            : throw new InvalidOperationException("Current backend does not support RenderTexture.");

        _renderTexture = provider.Create((width, height));
        _frameBufferManager = PrometeApp.Current.GetPlugin<FrameBufferManager>();
        _frameBufferManager.ActiveFrameBuffers.Add(this);

        _children.Location = (0, height);
        _children.Scale = (1, -1);
    }

    /// <summary>
    /// レンダリングされたテクスチャを取得します。
    /// </summary>
    public Texture2D Texture => _renderTexture.Texture;

    /// <summary>
    /// フレームバッファの子ノードの数を取得します。
    /// </summary>
    public int Count => _children.Count;

    /// <summary>
    /// このフレームバッファのサイズを取得します。
    /// </summary>
    public VectorInt Size
    {
        get => _size;
        set
        {
            if (_size == value) return;

            _size = value;
            _renderTexture.Resize(value);
            _children.Location = (0, value.Y);
        }
    }

    /// <summary>
    /// フレームバッファの幅を取得します。
    /// </summary>
    public int Width => Size.X;

    /// <summary>
    /// フレームバッファの高さを取得します。
    /// </summary>
    public int Height => Size.Y;

    /// <summary>
    /// フレームバッファの背景色を取得または設定します。
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 毎フレーム自動レンダリングするかどうかを取得または設定します（デフォルト: true）。
    /// </summary>
    public bool AutoRender { get; set; } = true;

    /// <summary>
    /// レンダリング前に画面をクリアするかどうかを取得または設定します（デフォルト: true）。
    /// </summary>
    public bool AutoClear { get; set; } = true;

    /// <summary>
    /// ソート済みの子ノードのリストを取得します。
    /// </summary>
    public IReadOnlyList<Node> SortedChildren => _children.sortedChildren;

    /// <summary>
    /// このフレームバッファの子ノードを取得または設定します。
    /// </summary>
    public Node this[int index] => _children[index];

    internal void BeforeRender()
    {
        _children.BeforeRender();
    }

    internal void Update()
    {
        if (_disposed) return;

        _children.Update();
    }

    /// <summary>
    /// 手動レンダリングを実行します。
    /// </summary>
    public void Render()
    {
        var app = PrometeApp.Current;
        var queue = app.GetPlugin<RenderCommandQueue>();
        var view = app.View;
        var ctx = new RenderContext
        {
            WindowSize = view.Size,
            WindowScale = view.Scale,
            ActualWidth = view.ActualWidth,
            ActualHeight = view.ActualHeight,
        };

        var clearColor = AutoClear ? BackgroundColor : (Color?)null;
        using var _ = _renderTexture.BeginCapture(clearColor);

        queue.PushScope();
        foreach (var child in SortedChildren)
            app.CollectNode(child, queue, ctx);
        queue.PopScopeAndFlush();
    }

    #region IEnumerable<Node>
    /// <summary>
    /// 指定したインデックスの位置に子ノードを挿入します。
    /// </summary>
    /// <param name="index">子ノードを挿入する位置のインデックス。</param>
    /// <param name="node">挿入するノード。</param>
    public void Insert(int index, Node node)
    {
        _children.Insert(index, node);
    }

    /// <summary>
    /// 指定したインデックスの位置にある子ノードを削除します。
    /// </summary>
    /// <param name="index">削除する子ノードのインデックス。</param>
    public void RemoveAt(int index)
    {
        Remove(this[index]);
    }

    /// <summary>
    /// フレームバッファに子ノードを追加します。
    /// </summary>
    /// <param name="node">追加するノード。</param>
    public void Add(Node node)
    {
        _children.Add(node);
    }

    /// <summary>
    /// 複数の子ノードをフレームバッファに追加します。
    /// </summary>
    /// <param name="nodes">追加するノードのコレクション。</param>
    public void AddRange(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            Add(node);
    }

    /// <summary>
    /// 複数の子ノードをフレームバッファに追加します。
    /// </summary>
    /// <param name="nodes">追加するノードの配列。</param>
    public void AddRange(params Node[] nodes)
    {
        AddRange((IEnumerable<Node>)nodes);
    }

    /// <summary>
    /// フレームバッファから全ての子ノードを削除します。
    /// </summary>
    public void Clear()
    {
        _children.Clear();
    }

    /// <summary>
    /// 指定したノードがフレームバッファの子ノードとして含まれているかどうかを確認します。
    /// </summary>
    /// <param name="node">検索するノード。</param>
    /// <returns>ノードが含まれている場合はtrue、それ以外はfalse。</returns>
    public bool Contains(Node node)
    {
        return _children.Contains(node);
    }

    /// <summary>
    /// 指定したノードをフレームバッファから削除します。
    /// </summary>
    /// <param name="node">削除するノード。</param>
    /// <returns>ノードが正常に削除された場合はtrue、それ以外はfalse。</returns>
    public bool Remove(Node node)
    {
        return _children.Remove(node);
    }

    /// <summary>
    /// フレームバッファ内の子ノードを反復処理する列挙子を返します。
    /// </summary>
    /// <returns>フレームバッファの子ノードを反復処理するための列挙子。</returns>
    public IEnumerator<Node> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _children.GetEnumerator();
    }
    #endregion

    #region IDisposable
    /// <summary>
    /// このオブジェクトによって使用されているリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// このオブジェクトによって使用されているリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージドリソースとアンマネージドリソースの両方を解放する場合はtrue、アンマネージドリソースのみを解放する場合はfalse。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // マネージドリソースを解放
            _renderTexture.Dispose();
            _frameBufferManager.ActiveFrameBuffers.Remove(this);
            foreach (var child in _children)
            {
                child.Destroy();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// ファイナライザー
    /// </summary>
    ~FrameBuffer()
    {
        Dispose(false);
    }
    #endregion
}
