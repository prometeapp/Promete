using System;
using System.IO;

namespace Promete.Audio;

/// <summary>
/// Ogg Vorbis 形式の音声を <see cref="VorbisAudioSource"/> にデコードする、標準のデコーダーです。 (v2.4~)
/// </summary>
public sealed class VorbisAudioDecoder : IAudioDecoder
{
    /// <summary>
    /// 先頭が <c>OggS</c> である場合にデコードできると判定します。拡張子は見ません。
    /// </summary>
    /// <param name="path">ファイルのパス。使いません。</param>
    /// <param name="header">ファイルの先頭のバイト列。</param>
    /// <returns>デコードできる場合は <see langword="true"/>。</returns>
    public bool CanDecode(string path, ReadOnlySpan<byte> header)
    {
        return header.Length >= 4 && header[..4].SequenceEqual("OggS"u8);
    }

    /// <summary>
    /// ストリームを <see cref="VorbisAudioSource"/> にデコードします。
    /// ストリームは、別スレッドで行われるデコードの終わりに閉じられます。
    /// </summary>
    /// <param name="stream">Ogg Vorbis 形式のデータのストリーム。</param>
    /// <returns>デコードした音声。</returns>
    public IAudioSource Decode(Stream stream)
    {
        return new VorbisAudioSource(stream);
    }
}
