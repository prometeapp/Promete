using System;

namespace Promete.Audio.Filters;

/// <summary>
/// <see cref="AudioPlayer"/> の出力段に挿入できる DSP フィルターの仕様を定義するインターフェースです。
/// </summary>
/// <remarks>
/// <para>
/// フィルターは、常駐レンダーループの一部として、再生停止中・無音バッファ中も含めて毎バッファ呼び出され続けます。
/// これにより、ディレイの残響などがソースの終端や <see cref="AudioPlayer.Stop"/> の呼び出し後も自然に減衰しながら鳴り切ります
/// （出力終了を待つための特別な「テール処理」は不要です）。
/// </para>
/// <para>
/// パイプラインが自動的に <see cref="Reset"/> を呼び出すことはありません。再生の停止・シーク・別ソースへの差し替えを挟んでも、
/// フィルターの内部状態（ディレイラインなど）はそのまま維持されます。状態を明示的にクリアしたい場合は、
/// 利用者が任意のタイミングで <see cref="Reset"/> を呼び出してください。
/// </para>
/// <para>
/// <see cref="Process"/> はオーディオ出力スレッドから呼び出されます。一方でカットオフ周波数などのパラメータプロパティは
/// ゲームスレッドから随時書き込まれることを想定しているため、実装は float の単発読み書きのみで完結させ、
/// ロックを取得しないでください（多少の値の食い違いは許容し、単純さを優先する設計です）。
/// </para>
/// </remarks>
public interface IAudioFilter
{
    /// <summary>
    /// バッファをインプレースで加工します。
    /// </summary>
    /// <param name="buffer">
    /// 加工対象のバッファ。チャンネルインターリーブ形式の float PCM（ステレオ interleaved）です。
    /// </param>
    /// <param name="channels">バッファのチャンネル数。</param>
    /// <param name="sampleRate">現在のサンプリング周波数。</param>
    public void Process(Span<float> buffer, int channels, int sampleRate);

    /// <summary>
    /// フィルターの内部状態（ディレイラインなど）を明示的に消去します。
    /// </summary>
    public void Reset();
}
