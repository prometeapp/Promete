---
title: クイックスタート
description: Prometeの導入方法および、最初のウィンドウを表示するまでの手順を解説します。
sidebar:
  order: 2
---

さあ、まずははじめの一歩を踏み出しましょう。このチュートリアルでは、Prometeの導入方法および、実際にウィンドウを表示するまでの手順を解説します。

## 必要な環境

* .NET 8 SDK
* .NET CLI
  * あるいは、Visual Studio、JetBrains Riderなどの、NuGetをサポートするIDE
* IDE あるいはテキストエディタ

## プロジェクトの作成

.NET CLIあるいは、IDEのテンプレートから、.NET 8を用いたコンソールアプリのプロジェクトを作成します。

## Prometeのインストール

NuGetパッケージマネージャを用いて、Prometeのパッケージをインストールします。

### .NET CLI

以下のコマンドを実行して、NuGetパッケージを追加します。

```sh
dotnet add package Promete --prerelease
```

### Visual Studio, Rider等のIDE

NuGet パッケージマネージャを開き、`Promete`を検索してインストールします。

## エントリーポイントの記述

プロジェクトに `Program.cs` および `MainScene.cs` の2つのファイルを作成します。
これがPrometeアプリケーションの標準的なエントリーポイントとなります。

```csharp title="Program.cs"
using Promete;
using Promete.GLDesktop;

var app = PrometeApp.Create()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
```

```csharp title="MainScene.cs"
using Promete;

public class MainScene : Scene
{
}
```

これは、デフォルトの黒いウィンドウを表示する最小のコードです。ビルドして実行してみましょう。

```sh
dotnet run
```

![window](/assets/initial-window.png)

ウィンドウが表示されるはずです。

次の例では、コンソール機能を使ったHello, world!を実演します。
