using System;

namespace Promete.Graphics;

/// <summary>
/// カスタムシェーダープログラムを表します。
/// <see cref="Create"/> でビルダーを生成し、<see cref="Vertex"/>・<see cref="Fragment"/> で
/// ソースコードを設定した後、<see cref="Compile"/> でコンパイルしてください。
/// </summary>
/// <remarks>
/// <see cref="Compile"/> はメインスレッド上かつ GL コンテキストが有効な状態
/// （<c>OnStart()</c> 以降）で呼び出してください。
/// </remarks>
public sealed class ShaderProgram : IDisposable
{
    /// <summary>頂点シェーダーのソースコードを取得します。</summary>
    public string? VertexShaderSource { get; private set; }

    /// <summary>フラグメントシェーダーのソースコードを取得します。</summary>
    public string? FragmentShaderSource { get; private set; }

    /// <summary>コンパイル済みシェーダープログラムのバックエンドハンドルを取得します。</summary>
    public int Handle { get; private set; }

    private Action<ShaderProgram>? _onDispose;

    private ShaderProgram() { }

    /// <summary>新しい <see cref="ShaderProgram"/> ビルダーを生成します。</summary>
    public static ShaderProgram Create() => new();

    /// <summary>頂点シェーダーのソースコードを設定します。</summary>
    public ShaderProgram Vertex(string source)
    {
        VertexShaderSource = source;
        return this;
    }

    /// <summary>フラグメントシェーダーのソースコードを設定します。</summary>
    public ShaderProgram Fragment(string source)
    {
        FragmentShaderSource = source;
        return this;
    }

    /// <summary>
    /// シェーダーをコンパイルします。
    /// 内部で <see cref="IShaderFactory"/> を取得し、バックエンドに処理を委譲します。
    /// </summary>
    public ShaderProgram Compile()
    {
        PrometeApp.Current.GetPlugin<IShaderFactory>().Compile(this);
        return this;
    }

    /// <summary>
    /// コンパイル済みデータを設定します。<see cref="IShaderFactory"/> の実装のみが呼び出します。
    /// </summary>
    internal void SetCompiledData(int handle, Action<ShaderProgram> onDispose)
    {
        Handle = handle;
        _onDispose = onDispose;
    }

    /// <summary>GPU リソースを解放します。</summary>
    public void Dispose() => _onDispose?.Invoke(this);
}
