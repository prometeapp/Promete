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
    public Texture2D Texture { get; internal set; }

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
    public VectorInt Size
    {
        get => _size;
        set
        {
            if (_size == value) return;

            _size = value;
            _frameBufferProvider.Resize(this);
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
    /// ソート済みの子ノードのリストを取得します。
    /// </summary>
    public IReadOnlyList<Node> SortedChildren => _children.sortedChildren;

    private bool _disposed;

    private VectorInt _size;

    private readonly Container _children = [];

    private readonly FrameBufferManager _frameBufferManager;
    private readonly IFrameBufferProvider _frameBufferProvider;

    /// <summary>
    /// 指定したサイズの <see cref="FrameBuffer"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="width">フレームバッファの幅。</param>
    /// <param name="height">フレームバッファの高さ。</param>
    public FrameBuffer(int width, int height)
    {
        _size = (width, height);
        _frameBufferProvider = PrometeApp.Current.TryGetPlugin<IFrameBufferProvider>(out var provider)
            ? provider
            : throw new InvalidOperationException("Current backend does not support FrameBuffer.");

        _frameBufferManager = PrometeApp.Current.GetPlugin<FrameBufferManager>();
        _frameBufferManager.ActiveFrameBuffers.Add(this);

        _children.Location = (0, height);
        _children.Scale = (1, -1);

        Texture = _frameBufferProvider.CreateTexture(this);
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
