# Promete.ImGui

Promete.ImGuiは、ImGUIを利用可能にするPromete プラグインです。

## 動作要件

* .NET 8
* 最新版のPromete
    * OpenGL Desktop バックエンドのみサポートしています。

## サンプル

```cs
using Promete;

var app = PrometeApp.Create()
	.Use<ImGuiPlugin>()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();

class MainScene(ImGuiPlugin imgui) : Scene
{
	public override void OnStart()
	{
		imgui.OnRender = OnRenderUI;
	}

	private void OnRenderUI()
	{
		ImGuiNET.ImGui.ShowDemoWindow();
	}
}
```
