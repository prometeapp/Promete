using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using Promete.Internal;
using Promete.Nodes;

namespace Promete.Graphics;

/// <summary>
/// 子要素をテクスチャにレンダリングする <see cref="Container"/> です。
/// </summary>
public class FrameBuffer : IEnumerable<Node>, IDisposable
{
    /// <summary>
    /// レンダリングされたテクスチャを取得します。
    /// </summary>
    public Texture2D Texture { get; }

    /// <summary>
    /// フレームバッファの子ノードの数を取得します。
    /// </summary>
    public int Count => _children.Count;


    /// <summary>
    /// このフレームバッファの子ノードを取得または設定します。
    /// </summary>
    public Node this[int index] => _children[index];

    /// <summary>
    /// このフレームバッファのサイズを取得します。
    /// </summary>
    public VectorInt Size { get; }

    public int Width => Size.X;

    public int Height => Size.Y;

    /// <summary>
    /// フレームバッファの背景色を取得または設定します。
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;

    public IReadOnlyList<Node> SortedChildren => _children.sortedChildren;

    private bool _disposed;

    private readonly Container _children = [];

    private readonly FrameBufferManager _frameBufferManager;

    /// <summary>
    /// 指定したサイズの <see cref="FrameBuffer"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="width">フレームバッファの幅。</param>
    /// <param name="height">フレームバッファの高さ。</param>
    public FrameBuffer(int width, int height)
    {
        Size = (width, height);
        var p = PrometeApp.Current.TryGetPlugin<IFrameBufferProvider>(out var provider)
            ? provider
            : throw new InvalidOperationException("Current backend does not support FrameBuffer.");

        _frameBufferManager = PrometeApp.Current.GetPlugin<FrameBufferManager>();
        _frameBufferManager.ActiveFrameBuffers.Add(this);

        _children.Location = (0, height);
        _children.Scale = (1, -1);

        Texture = p.CreateTexture(this);
    }

    internal void BeforeRender()
    {
        _children.BeforeRender();
    }

    internal void Update()
    {
        if (_disposed) return;

        _children.Update();
    }

    #region IEnumerable<Node>
    public void Insert(int index, Node node)
    {
        _children.Insert(index, node);
    }

    public void RemoveAt(int index)
    {
        Remove(this[index]);
    }

    public void Add(Node node)
    {
        _children.Add(node);
    }

    public void AddRange(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            Add(node);
    }

    public void AddRange(params Node[] nodes)
    {
        AddRange((IEnumerable<Node>)nodes);
    }

    public void Clear()
    {
        _children.Clear();
    }

    public bool Contains(Node node)
    {
        return _children.Contains(node);
    }

    public bool Remove(Node node)
    {
        return _children.Remove(node);
    }

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
            Texture.Dispose();
            _frameBufferManager.ActiveFrameBuffers.Remove(this);
            foreach (var child in _children)
            {
                child.Destroy();
            }
        }

        // アンマネージドリソースを解放（必要であれば）

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
