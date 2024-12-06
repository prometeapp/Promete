namespace Promete.Input;

public class MouseButtonEventArgs(int buttonId, VectorInt position) : MouseEventArgs(position)
{
    /// <summary>
    /// このイベントが発生したボタンの種類を取得します。
    /// </summary>
    public int ButtonId { get; } = buttonId;

    /// <summary>
    /// このイベントが発生したボタンの種類を取得します。
    /// </summary>
    public MouseButtonType ButtonType => (MouseButtonType)ButtonId;
}
