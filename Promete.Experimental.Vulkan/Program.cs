using Promete;
using Promete.Experimental.Vulkan;
using Promete.VulkanDesktop;
using Promete.Windowing;

// Vulkan バックエンドの起動確認用プログラム。
// 数秒間クリアカラーを表示し、自動終了する。例外はそのままコンソールに出る。
var app = PrometeApp
    .Create()
    .BuildWithVulkanDesktop(
        WindowOptions.Default with
        {
            Title = "Promete Vulkan Experimental",
            Mode = WindowMode.Resizable,
        }
    );

return app.Run<MainScene>();
