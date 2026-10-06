using System;
using System.Buffers.Binary;
using System.IO;

namespace Promete.Graphics.Imaging;

/// <summary>
/// PNG の 1 ピクセル分のサンプルを RGBA8888 へ変換します。
/// </summary>
internal sealed class PngPixelConverter
{
    private readonly int _colorType;
    private readonly int _bitDepth;
    private readonly int _channels;
    private readonly byte[]? _palette;
    private readonly byte[]? _transparency;
    private readonly int[] _keys = new int[3];
    private readonly bool _hasKey;

    public PngPixelConverter(
        int colorType,
        int bitDepth,
        int channels,
        byte[]? palette,
        byte[]? transparency
    )
    {
        _colorType = colorType;
        _bitDepth = bitDepth;
        _channels = channels;
        _palette = palette;
        _transparency = transparency;

        // グレースケールと RGB の tRNS は、透明にする色 (16bit 固定) を指す
        var keyLength = colorType switch
        {
            0 => 2,
            2 => 6,
            _ => 0,
        };
        if (keyLength > 0 && transparency is { } t && t.Length >= keyLength)
        {
            _hasKey = true;
            for (var i = 0; i < keyLength / 2; i++)
                _keys[i] = BinaryPrimitives.ReadUInt16BigEndian(t.AsSpan(i * 2));
        }
    }

    public void Convert(ReadOnlySpan<byte> row, int x, Span<byte> destination)
    {
        var baseIndex = x * _channels;
        switch (_colorType)
        {
            case 0:
            {
                var g = Sample(row, baseIndex);
                var gray = To8(g);
                destination[0] = destination[1] = destination[2] = gray;
                destination[3] = (byte)(_hasKey && g == KeyFor(_keys[0]) ? 0 : 255);
                break;
            }
            case 2:
            {
                var r = Sample(row, baseIndex);
                var g = Sample(row, baseIndex + 1);
                var b = Sample(row, baseIndex + 2);
                destination[0] = To8(r);
                destination[1] = To8(g);
                destination[2] = To8(b);
                var transparent =
                    _hasKey
                    && r == KeyFor(_keys[0])
                    && g == KeyFor(_keys[1])
                    && b == KeyFor(_keys[2]);
                destination[3] = (byte)(transparent ? 0 : 255);
                break;
            }
            case 3:
            {
                var index = Sample(row, baseIndex);
                var offset = index * 3;
                if (offset + 2 >= _palette!.Length)
                    throw new InvalidDataException("PNG のパレット番号が範囲外です。");
                destination[0] = _palette[offset];
                destination[1] = _palette[offset + 1];
                destination[2] = _palette[offset + 2];
                destination[3] =
                    _transparency is not null && index < _transparency.Length
                        ? _transparency[index]
                        : (byte)255;
                break;
            }
            case 4:
            {
                var gray = To8(Sample(row, baseIndex));
                destination[0] = destination[1] = destination[2] = gray;
                destination[3] = To8(Sample(row, baseIndex + 1));
                break;
            }
            default:
                destination[0] = To8(Sample(row, baseIndex));
                destination[1] = To8(Sample(row, baseIndex + 1));
                destination[2] = To8(Sample(row, baseIndex + 2));
                destination[3] = To8(Sample(row, baseIndex + 3));
                break;
        }
    }

    /// <summary>
    /// tRNS のキーはビット深度に関わらず 16bit で格納されているため、下位ビットだけを比較用に取り出します。
    /// </summary>
    private int KeyFor(int key)
    {
        return _bitDepth == 16 ? key : key & ((1 << _bitDepth) - 1);
    }

    private int Sample(ReadOnlySpan<byte> row, int index)
    {
        switch (_bitDepth)
        {
            case 8:
                return row[index];
            case 16:
                return (row[index * 2] << 8) | row[(index * 2) + 1];
            default:
                var bit = index * _bitDepth;
                var shift = 8 - _bitDepth - (bit & 7);
                return (row[bit >> 3] >> shift) & ((1 << _bitDepth) - 1);
        }
    }

    private byte To8(int value)
    {
        return _bitDepth switch
        {
            8 => (byte)value,
            16 => (byte)(value >> 8),
            _ => (byte)(value * 255 / ((1 << _bitDepth) - 1)),
        };
    }
}
