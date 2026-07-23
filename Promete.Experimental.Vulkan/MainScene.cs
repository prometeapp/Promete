using System.Drawing;
using Promete.Graphics;
using Promete.Nodes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;

namespace Promete.Experimental.Vulkan;

/// <summary>
/// Vulkan バックエンドの描画検証シーン。
/// スプライトとプリミティブを描画し、スクリーンショットのピクセル色を検証して終了します。
/// </summary>
public class MainScene : Scene
{
    private const int ScreenshotFrame = 60;

    private static readonly string ScreenshotPath = Path.Combine(
        AppContext.BaseDirectory,
        "vulkan_test.png"
    );

    private Texture2D _redTexture;
    private int _frameCount;
    private bool _screenshotRequested;

    public override void OnStart()
    {
        App.BackgroundColor = Color.DarkSlateBlue;

        // 赤い 100x100 スプライト (左上 50,50)
        _redTexture = App.TextureFactory.CreateSolid(Color.Red, (100, 100));
        Root.Add(new Sprite(_redTexture).Location(50, 50));

        // ライム色の塗りつぶし矩形 (250,100)-(350,200)
        Root.Add(Shape.CreateRect(250, 100, 350, 200, Color.Lime));

        // ティント検証用: 白テクスチャ × 青ティント (450,50)
        var white = App.TextureFactory.CreateSolid(Color.White, (50, 50));
        var tinted = new Sprite(white).Location(450, 50);
        tinted.TintColor = Color.Blue;
        Root.Add(tinted);

        Console.WriteLine("[MainScene] OnStart: ノード配置完了");
    }

    public override void OnUpdate()
    {
        _frameCount++;

        if (_frameCount == ScreenshotFrame && !_screenshotRequested)
        {
            _screenshotRequested = true;
            _ = VerifyAndExitAsync();
        }
    }

    public override void OnDestroy()
    {
        _redTexture.Dispose();
        Console.WriteLine("[MainScene] OnDestroy");
    }

    private async Task VerifyAndExitAsync()
    {
        try
        {
            await Window.SaveScreenshotAsync(ScreenshotPath);
            Console.WriteLine($"[MainScene] スクリーンショット保存: {ScreenshotPath}");

            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(ScreenshotPath);
            var failures = 0;
            failures += Verify(img, 10, 10, Color.DarkSlateBlue, "背景 (左上)");
            failures += Verify(img, 100, 100, Color.Red, "赤スプライト中心");
            failures += Verify(img, 300, 150, Color.Lime, "ライム矩形中心");
            failures += Verify(img, 475, 75, Color.Blue, "青ティントスプライト");
            failures += Verify(img, 100, 400, Color.DarkSlateBlue, "背景 (下部, Y軸反転検出)");
            failures += Verify(img, 620, 460, Color.DarkSlateBlue, "背景 (右下)");

            Console.WriteLine(
                failures == 0
                    ? "[MainScene] ✅ 全ピクセル検証パス"
                    : $"[MainScene] ❌ {failures} 件の検証失敗"
            );
            App.Exit(failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainScene] ❌ 検証中に例外: {ex}");
            App.Exit(2);
        }
    }

    private static int Verify(Image<Rgba32> img, int x, int y, Color expected, string label)
    {
        var actual = img[x, y];
        var ok =
            Math.Abs(actual.R - expected.R) <= 2
            && Math.Abs(actual.G - expected.G) <= 2
            && Math.Abs(actual.B - expected.B) <= 2;

        Console.WriteLine(
            $"[MainScene] {(ok ? "OK" : "NG")} {label} ({x},{y}): "
                + $"expected=({expected.R},{expected.G},{expected.B}) actual=({actual.R},{actual.G},{actual.B})"
        );
        return ok ? 0 : 1;
    }
}
