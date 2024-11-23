# クイックスタート

さあ、まずははじめの一歩を踏み出しましょう。このチュートリアルでは、Prometeの導入方法および、実際にウィンドウを表示するまでの手順を解説します。

## インストール

### 必要な環境

* .NET 8 SDK
* .NET CLI
    * あるいは、Visual Studio、JetBrains Riderなどの、NuGetをサポートするIDE
* IDE あるいはテキストエディタ

### プロジェクトの作成

.NET CLIあるいは、IDEのテンプレートから、.NET 8を用いたコンソールアプリのプロジェクトを作成します。

### Prometeのインストール

NuGetパッケージマネージャを用いて、Prometeのパッケージをインストールします。

#### .NET CLI

```sh
dotnet add package Promete --prerelease
```

#### Visual Studio

NuGet パッケージマネージャを開き、`Promete`を検索してインストールします。

#### JetBrains Rider

NuGet パッケージマネージャを開き、`Promete`を検索してインストールします。

## エントリポイントの記述

プロジェクトに `Program.cs` および `MainScene.cs` の2つのファイルを作成します。その上で、次のように記述します。

### Program.cs

```csharp
using Promete;
using Promete.GLDesktop;

var app = PrometeApp.Create()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
```

### MainScene.cs

```csharp
using Promete;

public class MainScene : Scene
{
}
```

書き終わったら、ビルドして実行してみましょう。

```sh
dotnet run
```

![window](/assets/initial-window.png)

ウィンドウが表示されたら、成功です！

これだけではまだ物足りないですよね。次のページで、ウィンドウに文字列を表示させてみましょう。
