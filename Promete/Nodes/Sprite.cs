﻿using System.Drawing;
using Promete.Graphics;

namespace Promete.Nodes;

public class Sprite(Texture2D? texture = null, Color? tintColor = default) : Node
{
    private VectorInt? _size;
    public Texture2D? Texture { get; set; } = texture;

    public Color TintColor { get; set; } = tintColor ?? Color.White;

    public override VectorInt Size
    {
        get => _size ?? Texture?.Size ?? (0, 0);
        set => _size = value;
    }

    public void ResetSize()
    {
        _size = null;
    }
}
