using System;
using System.IO;

namespace Promete.Audio;

/// <summary>
/// RIFF WAVE 形式の音声を <see cref="WaveAudioSource"/> にデコードする、標準のデコーダーです。 (v2.4~)
/// </summary>
public sealed class WaveAudioDecoder : IAudioDecoder
{
    /// <summary>
    /// 先頭が <c>RIFF</c> で、8 バイト目からが <c>WAVE</c> である場合にデコードできると判定します。拡張子は見ません。
    /// </summary>
    /// <param name="path">ファイルのパス。使いません。</param>
    /// <param name="header">ファイルの先頭のバイト列。</param>
    /// <returns>デコードできる場合は <see langword="true"/>。</returns>
    public bool CanDecode(string path, ReadOnlySpan<byte> header)
    {
        return header.Length >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header.Slice(8, 4).SequenceEqual("WAVE"u8);
    }

    /// <summary>
    /// ストリームを <see cref="WaveAudioSource"/> にデコードします。ストリームはデコードの完了時に閉じられます。
    /// </summary>
    /// <param name="stream">WAVE 形式のデータのストリーム。</param>
    /// <returns>デコードした音声。</returns>
    public IAudioSource Decode(Stream stream)
    {
        return new WaveAudioSource(stream);
    }
}
