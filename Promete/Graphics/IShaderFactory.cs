namespace Promete.Graphics;

/// <summary>
/// シェーダープログラムのコンパイルを担うファクトリーのインターフェースです。
/// バックエンドがこのインターフェースを実装し、DI コンテナに登録します。
/// </summary>
public interface IShaderFactory
{
    /// <summary>
    /// <paramref name="program"/> のソースコードをコンパイルし、
    /// <see cref="ShaderProgram.SetCompiledData"/> でハンドルと破棄デリゲートを設定します。
    /// </summary>
    void Compile(ShaderProgram program);
}
