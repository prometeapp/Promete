using System;

namespace Promete.Audio;

/// <summary>
/// 1バッファ分の interleaved float PCM（範囲は -1.0～1.0）を生成するコールバックです。
/// </summary>
/// <param name="buffer">
/// 書き込み先のバッファ。要素数は「1回に要求されるフレーム数 × チャンネル数」です。
/// </param>
public delegate void AudioRenderCallback(Span<float> buffer);
