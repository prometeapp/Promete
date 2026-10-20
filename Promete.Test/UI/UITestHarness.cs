using System.Reflection;
using Promete.Headless;
using Promete.Input;
using Promete.UI;

namespace Promete.Test.UI;

/// <summary>
/// ヘッドレスのアプリに UIManager を載せ、マウス・キーボード入力を注入してフレームを進めるテスト用ハーネスです。
/// </summary>
internal sealed class UITestHarness : IDisposable
{
    public UITestHarness()
    {
        App = PrometeApp
            .Create()
            .Use<Keyboard>()
            .Use<Mouse>()
            .Use<UIManager>()
            .UseScenesFrom(typeof(UITestHarness).Assembly)
            .BuildWithHeadless();
        App.OnStart();
        UI = App.GetPlugin<UIManager>();
        Mouse = App.GetPlugin<Mouse>();
        Keyboard = App.GetPlugin<Keyboard>();
    }

    private static readonly FieldInfo MousePositionField =
        typeof(Mouse).GetField(
            "<Position>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        )
        ?? throw new InvalidOperationException(
            "Mouse.Position のバッキングフィールドが見つかりません。"
        );

    private static readonly FieldInfo KeyCharsField =
        typeof(Keyboard).GetField("_keyChars", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Keyboard の文字バッファが見つかりません。");

    public PrometeApp App { get; }

    public UIManager UI { get; }

    public Mouse Mouse { get; }

    public Keyboard Keyboard { get; }

    public void Dispose()
    {
        App.OnDestroy();
    }

    private void SetMousePosition(int x, int y) =>
        MousePositionField.SetValue(Mouse, new VectorInt(x, y));

    /// <summary>描画準備と更新を1フレーム分進めます。</summary>
    public void Frame(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            App.OnRender();
            App.OnUpdate();
        }
    }

    /// <summary>指定座標で左クリック（押下 → 解放）を行います。</summary>
    public void Click(int x, int y)
    {
        SetMousePosition(x, y);
        var left = Mouse[MouseButtonType.Left];
        left.IsPressed = true;
        left.IsButtonDown = true;
        Frame();

        left.IsPressed = false;
        left.IsButtonUp = true;
        Frame();
    }

    /// <summary>マウスカーソルを移動してフレームを進めます。</summary>
    public void MoveMouse(int x, int y)
    {
        SetMousePosition(x, y);
        Frame();
    }

    /// <summary>キーを押して1フレーム進めます。キーは押されたままになります。</summary>
    public void KeyDown(KeyCode code)
    {
        var key = Keyboard.KeyOf(code);
        key.IsPressed = true;
        key.IsKeyDown = true;
        Frame();
    }

    /// <summary>押されているキーを離して1フレーム進めます。</summary>
    public void KeyUp(KeyCode code)
    {
        var key = Keyboard.KeyOf(code);
        key.IsPressed = false;
        key.IsKeyUp = true;
        Frame();
    }

    /// <summary>キーを押して離します。</summary>
    public void PressKey(KeyCode code)
    {
        KeyDown(code);
        KeyUp(code);
    }

    /// <summary>文字を入力して1フレーム進めます。</summary>
    public void TypeText(string text)
    {
        var queue = (Queue<char>)KeyCharsField.GetValue(Keyboard)!;
        foreach (var c in text)
            queue.Enqueue(c);
        Frame();
    }
}

/// <summary>
/// シーンの切り替えをテストするための、何もしないシーンです。
/// </summary>
internal sealed class UITestScene : Scene
{
    /// <summary>OnStart が呼ばれた回数。</summary>
    public static int StartCount { get; set; }

    public override void OnStart()
    {
        StartCount++;
    }
}
