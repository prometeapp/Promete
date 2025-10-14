---
title: ConsoleLayer
description: ConsoleLayerによる簡易テキスト出力・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

ConsoleLayer は、画面上に簡易なテキスト出力を行うためのレイヤーです。デバッグ用のログ表示や、簡単なUIの実装などに利用できます。
通常のノードとは異なり、テキストをバッファとして管理し、ウィンドウサイズやフォントに応じて自動的に表示行数を調整します。

## 基本的な使い方

ConsoleLayer は PrometeApp のプラグインとして登録し、シーンのコンストラクタで依存性注入を利用して受け取ります。

```csharp
// プラグイン登録例（Program.cs）
var app = PrometeApp.Create()
    .Use<ConsoleLayer>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();
```

```csharp
// シーンでの利用例
public class MainScene(ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        console.Print("Hello, world!");
        console.Print("Press [ESC] to exit");
    }
}
```

## 主なメソッド・プロパティ

- **`Print(object? obj)`**<br/>
  テキストを1行出力します。改行を含む文字列も複数行として出力されます。
- **`Clear()`**<br/>
  画面のテキストをすべて消去します。
- **`Cursor`**<br/>
  現在のカーソル位置（VectorInt型）。`Print`実行時に自動で次の行に移動します。
- **`Font`**<br/>
  表示に使うフォントを取得・設定できます。
- **`TextColor`**<br/>
  文字色を取得・設定できます。

## サンプル：ファイルリストの表示

```csharp
public override void OnUpdate()
{
    console.Clear();
    console.Print("Promete Demo\n");
    console.Print($"現在のディレクトリ: /{CurrentFolder.GetFullPath()}\n");

    for (var i = 0; i < CurrentFolder.Files.Count; i++)
    {
        var item = CurrentFolder.Files[i];
        var label = item is SceneFile file ? $"{file.Name} - {file.Description}" : item.Name;
        console.Print($"{(i == CurrentIndex ? ">" : " ")} {label}");
    }

    console.Print($"{(CurrentIndex == CurrentFolder.Files.Count ? ">" : " ")} もどる");
}
```

## 注意点

- Print で出力できる行数はウィンドウサイズとフォントサイズにより自動で決まります。
- 画面サイズ変更時は自動で再計算されます。
- テキストはバッファとして保持され、最新の行が常に表示されます。
