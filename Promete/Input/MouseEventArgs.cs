using System;

namespace Promete.Input;

/// <summary>
/// マウスイベントに関連する情報を提供するクラスです。
/// </summary>
public class MouseEventArgs(VectorInt position) : EventArgs
{
    /// <summary>
    /// イベントに関連するマウスの位置を取得します。
    /// </summary>
    public VectorInt Position { get; } = position;
}
