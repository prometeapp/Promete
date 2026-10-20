using System;
using System.IO;

namespace Promete.Audio;

/// <summary>
/// ストリームから <see cref="IAudioSource"/> を作るデコーダーです。 (v2.4~)
/// </summary>
/// <remarks>
/// <para>
/// <c>PrometeApp.Create().Use&lt;IAudioDecoder, MyDecoder&gt;()</c> で登録すると、Assets API の <c>LoadAudio</c> が使います。
/// 登録したデコーダーは、標準のデコーダー (<see cref="WaveAudioDecoder"/>、<see cref="VorbisAudioDecoder"/>) より先に、登録した順に判定されます。
/// </para>
/// <para>
/// <see cref="Decode"/> が返す <see cref="IAudioSource"/> は、キャッシュされて複数の呼び出し元 (同時に鳴らす複数の <see cref="AudioPlayer"/> など) で共有されることがあります。
/// そのため、呼び出し元ごとの状態 (読み出し位置など) に依存せず、<see cref="IAudioSource.FillSamples"/> が複数のスレッドから並行して呼ばれても安全である必要があります。
/// </para>
/// </remarks>
public interface IAudioDecoder
{
    /// <summary>
    /// 指定したファイルをデコードできるかどうかを判定します。
    /// </summary>
    /// <param name="path">ファイルのパス (Assets API からは、正規化した論理パス)。拡張子で判定する形式のために渡されます。</param>
    /// <param name="header">ファイルの先頭のバイト列 (最大 64 バイト。ファイルが短ければそれより短い)。マジック (RIFF / OggS など) で判定する形式のために渡されます。</param>
    /// <returns>デコードできる場合は <see langword="true"/>。</returns>
    public bool CanDecode(string path, ReadOnlySpan<byte> header);

    /// <summary>
    /// ストリームをデコードします。
    /// </summary>
    /// <param name="stream">先頭に位置した、seek できるストリーム。閉じる責任は、このメソッド (と、返した <see cref="IAudioSource"/>) が持ちます。</param>
    /// <returns>デコードした音声。</returns>
    public IAudioSource Decode(Stream stream);
}
