using System;

namespace Promete.Audio;

/// <summary>
/// オーディオソースの仕様を定義するインターフェース。Promete APIで扱えるオーディオソースを表します。
/// </summary>
public interface IAudioSource
{
    /// <summary>
    /// このオーディオソースの総フレーム数を取得します。1フレームは全チャンネル分のサンプルをまとめた単位です。
    /// 総フレーム数が未確定、またはソースが無限に続くストリームである場合は <c>null</c> を返します。
    /// </summary>
    public int? Frames { get; }

    /// <summary>
    /// チャンネル数を取得します。
    /// </summary>
    public int Channels { get; }

    /// <summary>
    /// サンプリング周波数を取得します。
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// サンプルデータを指定されたバッファに読み込みます。
    /// </summary>
    /// <param name="buffer">
    /// 書き込み先のバッファ。チャンネルインターリーブ形式の float PCM（範囲は -1.0～1.0）を格納します。
    /// 要素数は「読み込みたいフレーム数 × <see cref="Channels"/>」です。
    /// 実装は、このバッファの長さを超えて書き込んではいけません。
    /// </param>
    /// <param name="offsetFrames">
    /// 読み出しを開始するフレーム位置。シークに対応していないソース（無限ストリームなど）は、この値を無視して構いません。
    /// </param>
    /// <returns>
    /// 実際に書き込んだフレーム数 (<c>filledFrames</c>) と、ソースの終端に達したかどうか (<c>isFinished</c>) の組。
    /// </returns>
    public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames);
}
