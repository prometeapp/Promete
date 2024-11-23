# 好きなテキストを表示する

`Text` Elementを使うと、画面上にテキストを表示できます。

テキストの表示には、通常フォントが必要です。Prometeにはフォントが内蔵されているため、すぐに使うことができます。

```csharp
var text = new Text("Hello, Promete!")
    .Location(32, 32);

Root.Add(text);
```

このコードを `OnStart` メソッドに追加すると、画面上に `Hello, Promete!` と表示されます。

## サイズや色の変更

コンストラクタでサイズや色の指定ができます。

```csharp
var text = new Text("Hello, Promete!", Font.GetDefault(24), Color.White)
    .Location(32, 32);

Root.Add(text);
```

`Font.GetDefault` メソッドは、Promete内蔵のフォントを取得する静的メソッドです。

## フォントのスタイルを変更する

対応しているフォントの場合、太字や斜体のレンダリングにも対応しています。

`Font.GetDefault` メソッドのオーバーロードで指定します。以下に、太字を指定する場合の例を示します。

```csharp
var text = new Text("Hello, Promete!", Font.GetDefault(24, FontStyle.Bold), Color.White)
    .Location(32, 32);

Root.Add(text);
```

## フォントの変更

内蔵フォント以外にも、システムフォントや、プロジェクトに追加したフォントを使うこともできます。

いずれの場合も、`Font` クラスの新しいインスタンスを作成し、それを `Text` クラスのコンストラクタの第ニ引数に（`Font.GetDefault` の代わりに）指定します。

### システムフォントを使う場合

システムフォントを使う場合、そのフォントファミリー名とサイズを指定します。

::: info

フォントファミリー名の調べ方はOSに依存するため、本ドキュメントでは触れません。

:::

```csharp
var font = new Font("MS Gothic", 24);
```

### プロジェクトに追加したフォントを使う場合

プロジェクトに追加したフォントを使う場合、そのフォントファイルのパスとサイズを指定します。

```csharp
var font = new Font("assets/myfont.ttf", 24);
```

::: info

技術的には、指定した文字列をまずファイルパスとして解釈し、ファイルが存在する場合はそれをフォントファイルとして読み込みます。存在しない場合は、その文字列をフォントファミリー名として解釈し、システムフォントとして読み込みます。それすらも存在しない場合、エラーとなります。

:::

## フォントの更新

`Text` クラスの `Font` プロパティを使って、フォントを更新することができます。

```csharp
text.Font = new Font("MS Gothic", 24);
```
