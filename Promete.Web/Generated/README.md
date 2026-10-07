# Generated

`tools/gen-calli-signatures.cs` が生成したファイルです。直接編集しないでください。

Mono WASM のインタプリタは、ネイティブ関数ポインタを呼ぶスタブを、シグネチャごとにビルド時に作ります。Silk.NET の calli はこの集計の対象外なので、`Silk.NET.OpenGL` の IL からシグネチャを集め、登録専用の P/Invoke（`CalliSignatures.g.cs`）と C 関数（`calli_signatures.g.c`）を生成しています。詳しくは `../DESIGN_NOTES.md` の §4.1 を参照してください。

Silk.NET フォーク側で登録を完結させる（DESIGN_PLAN の S1）までのつなぎです。

## 再生成

`Promete.Silk.OpenGL` のバージョンを変えたら、再生成してください。`tools/` で実行します（csproj のあるディレクトリで実行すると、終わらなくなります）。

```bash
cd tools
dotnet run gen-calli-signatures.cs -- --out ../Promete.Web/Generated --library promete_web --namespace Promete.Web.Generated <Silk.NET.OpenGL.dll のパス>
```

`Silk.NET.OpenGL.dll` は、NuGet のキャッシュ（`~/.nuget/packages/promete.silk.opengl/<バージョン>/lib/net10.0/`）にあります。`--library` は、ネイティブのシム（`native/promete_web.c`）のファイル名と一致させる必要があります。
