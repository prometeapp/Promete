using System;
using System.Collections.Generic;

namespace Promete.Graphics.Fonts;

/// <summary>
/// ラスタライズされたグリフを 1 枚の大きなテクスチャへ集約して管理します。
/// </summary>
/// <remarks>
/// 同一ページ上のグリフは同じテクスチャハンドルを共有するため、
/// <see cref="Rendering.RenderCommandQueue" /> によって 1 回の描画命令にまとめられます。
/// </remarks>
public sealed class GlyphAtlas(TextureFactoryBase factory, int pageSize = 1024) : IDisposable
{
    /// <summary>
    /// グリフ同士の間に確保する余白。UV の丸め誤差による隣接グリフの混入を防ぎます。
    /// </summary>
    private const int Padding = 1;

    private readonly Dictionary<GlyphKey, GlyphEntry> _entries = new();
    private readonly List<AtlasPage> _pages = [];
    private bool _isDisposed;

    /// <summary>
    /// アトラスが確保しているページ数を取得します。
    /// </summary>
    public int PageCount => _pages.Count;

    /// <summary>
    /// アトラスに登録されているグリフの数を取得します。
    /// </summary>
    public int GlyphCount => _entries.Count;

    /// <summary>
    /// 指定したグリフをアトラスから取得します。未登録の場合はラスタライズして登録します。
    /// </summary>
    public GlyphEntry GetOrAdd(in GlyphInfo glyph, in GlyphRenderOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var key = new GlyphKey(glyph.Source.SourceId, glyph.GlyphIndex, options);
        if (_entries.TryGetValue(key, out var cached))
            return cached;

        var bitmap = glyph.Source.Rasterize(glyph, options);
        if (options.HasBorder)
            bitmap = GlyphOutline.Create(bitmap, options.BorderThickness);

        var entry = bitmap.IsEmpty ? default : Allocate(bitmap);

        _entries[key] = entry;
        return entry;
    }

    /// <summary>
    /// アトラスの内容をすべて破棄し、確保しているページを解放します。
    /// </summary>
    public void Clear()
    {
        foreach (var page in _pages)
            page.Texture.Dispose();

        _pages.Clear();
        _entries.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        Clear();
    }

    /// <summary>
    /// グリフ画像をいずれかのページへ配置し、その領域を指すエントリを生成します。
    /// </summary>
    private GlyphEntry Allocate(in GlyphBitmap bitmap)
    {
        // ページに収まらない巨大なグリフは、専用のテクスチャとして確保します。
        if (bitmap.Size.X > pageSize || bitmap.Size.Y > pageSize)
        {
            var dedicated = factory.Create(bitmap.Pixels, bitmap.Size);
            _pages.Add(AtlasPage.CreateExhausted(dedicated));
            return new GlyphEntry(dedicated, bitmap.Size, bitmap.Bearing);
        }

        foreach (var page in _pages)
        {
            if (page.TryAllocate(bitmap.Size, pageSize, Padding, out var position))
                return Write(page, position, bitmap);
        }

        var newPage = new AtlasPage(CreatePageTexture());
        _pages.Add(newPage);

        if (!newPage.TryAllocate(bitmap.Size, pageSize, Padding, out var newPosition))
            throw new FontException("グリフをアトラスへ配置できませんでした。");

        return Write(newPage, newPosition, bitmap);
    }

    private GlyphEntry Write(AtlasPage page, VectorInt position, in GlyphBitmap bitmap)
    {
        factory.Update(page.Texture, position, bitmap.Size, bitmap.Pixels);

        var uvStart = new Vector((float)position.X / pageSize, (float)position.Y / pageSize);
        var uvEnd = new Vector(
            (float)(position.X + bitmap.Size.X) / pageSize,
            (float)(position.Y + bitmap.Size.Y) / pageSize
        );

        // アトラス上の部分領域であるため、個別に破棄されないテクスチャとして生成します。
        var texture = new Texture2D(
            page.Texture.Handle,
            bitmap.Size,
            static _ => { },
            uvStart,
            uvEnd
        );

        return new GlyphEntry(texture, bitmap.Size, bitmap.Bearing);
    }

    private Texture2D CreatePageTexture()
    {
        return factory.Create(new byte[pageSize * pageSize * 4], (pageSize, pageSize));
    }

    /// <summary>
    /// アトラスの 1 ページを表します。グリフはシェルフ (横方向の帯) に沿って詰められます。
    /// </summary>
    private sealed class AtlasPage(Texture2D texture)
    {
        private int _cursorX;
        private int _shelfY;
        private int _shelfHeight;

        public Texture2D Texture { get; } = texture;

        /// <summary>
        /// これ以上グリフを配置できないページを生成します。
        /// </summary>
        public static AtlasPage CreateExhausted(Texture2D texture)
        {
            return new AtlasPage(texture) { _shelfY = int.MaxValue };
        }

        public bool TryAllocate(VectorInt size, int pageSize, int padding, out VectorInt position)
        {
            position = default;
            if (_shelfY == int.MaxValue)
                return false;

            // 現在のシェルフに幅が残っていなければ、次のシェルフへ移動します。
            if (_cursorX + size.X > pageSize)
            {
                _shelfY += _shelfHeight + padding;
                _shelfHeight = 0;
                _cursorX = 0;
            }

            if (_shelfY + size.Y > pageSize)
                return false;

            position = new VectorInt(_cursorX, _shelfY);
            _cursorX += size.X + padding;
            _shelfHeight = Math.Max(_shelfHeight, size.Y);
            return true;
        }
    }
}
