using System;
using Promete.Input;

namespace Promete.UI;

/// <summary>
/// キーを押し続けたときの繰り返し (オートリピート) を、経過時間から作り出します。
/// 文字の入力は OS が繰り返してくれますが、BackSpace や矢印キーなどは「押した瞬間」しか通知されないため、
/// 押している時間を数えて繰り返し回数を求めます。
/// </summary>
internal sealed class KeyRepeater
{
    /// <summary>1フレームに繰り返す回数の上限。処理落ちの直後に、大量に繰り返さないための保険です。</summary>
    private const int MaxRepeatsPerFrame = 50;

    private bool _isHeld;
    private float _heldTime;
    private float _nextRepeatTime;

    /// <summary>
    /// 押している状態を忘れます。フォーカスが外れたときなどに呼びます。
    /// </summary>
    public void Reset()
    {
        _isHeld = false;
        _heldTime = 0;
        _nextRepeatTime = 0;
    }

    /// <summary>
    /// このフレームで、キーの入力を何回として扱うかを求めます。
    /// </summary>
    /// <param name="key">対象のキー。</param>
    /// <param name="deltaTime">前のフレームからの経過時間 (秒)。</param>
    /// <param name="delay">押してから、繰り返しが始まるまでの時間 (秒)。</param>
    /// <param name="interval">繰り返しの間隔 (秒)。</param>
    /// <returns>
    /// 押した瞬間は 1、押し続けて繰り返しの時刻に達した回数分、それ以外は 0。
    /// このクラスが押した瞬間を見ていないキー (フォーカスの前から押されていたキー) は、繰り返しません。
    /// </returns>
    public int Poll(Key key, float deltaTime, float delay, float interval)
    {
        if (key.IsKeyDown)
        {
            _isHeld = true;
            _heldTime = 0;
            _nextRepeatTime = delay;
            return 1;
        }

        if (!key.IsPressed)
        {
            Reset();
            return 0;
        }

        if (!_isHeld)
            return 0;

        _heldTime += deltaTime;
        interval = Math.Max(interval, 0.001f);

        var count = 0;
        while (_heldTime >= _nextRepeatTime && count < MaxRepeatsPerFrame)
        {
            _nextRepeatTime += interval;
            count++;
        }

        return count;
    }
}
