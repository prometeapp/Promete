namespace Promete.UI;

/// <summary>
/// UI のキーボード操作で使う入力アクションの状態です。
/// </summary>
internal sealed class InputAction
{
    internal InputAction(string name)
    {
        Name = name;
    }

    /// <summary>アクション名。</summary>
    public string Name { get; }

    /// <summary>現在押されているかどうか。</summary>
    public bool IsPressed { get; internal set; }

    /// <summary>このフレームで押された瞬間かどうか。</summary>
    public bool IsJustPressed => IsPressed && !WasPressed;

    internal bool WasPressed { get; set; }
}
