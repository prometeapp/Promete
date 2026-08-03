using Promete;
using Promete.Experimental.Vulkan;
using Promete.ImGui;
using Promete.VulkanDesktop;
using Promete.Windowing;

// Vulkan バックエンドの起動確認用プログラム。
// 描画各機能をピクセル検証し、自動終了する。例外はそのままコンソールに出る。
var app = PrometeApp
    .Create()
    .Use<ImGuiPlugin>()
    .BuildWithVulkanDesktop(
        WindowOptions.Default with
        {
            Title = "Promete Vulkan Experimental",
            Mode = WindowMode.Resizable,
        }
    );

return app.Run<MainScene>();
