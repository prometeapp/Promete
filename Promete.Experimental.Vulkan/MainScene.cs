using System.Drawing;

namespace Promete.Experimental.Vulkan;

/// <summary>
/// クリアカラー表示のみを行う起動確認シーン。一定フレーム経過後に自動終了します。
/// </summary>
public class MainScene : Scene
{
    private const int ExitAfterFrames = 300;

    private int _frameCount;

    public override void OnStart()
    {
        App.BackgroundColor = Color.CornflowerBlue;
        Console.WriteLine("[MainScene] OnStart: Vulkan バックエンド起動成功");
        Console.WriteLine($"[MainScene] Window: {Window.Size} (actual: {Window.ActualSize})");
    }

    public override void OnUpdate()
    {
        _frameCount++;

        // クリアカラーの切り替わりも確認する
        if (_frameCount == ExitAfterFrames / 2)
        {
            App.BackgroundColor = Color.DarkOrange;
            Console.WriteLine("[MainScene] クリアカラーを DarkOrange に変更");
        }

        if (_frameCount >= ExitAfterFrames)
        {
            Console.WriteLine($"[MainScene] {ExitAfterFrames} フレーム描画完了、終了します");
            App.Exit();
        }
    }

    public override void OnDestroy()
    {
        Console.WriteLine("[MainScene] OnDestroy");
    }
}
