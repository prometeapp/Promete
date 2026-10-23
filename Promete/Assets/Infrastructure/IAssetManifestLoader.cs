using System;
using System.Reflection;

namespace Promete.Assets.Infrastructure;

/// <summary>
/// アセンブリの <see cref="PrometeAssetSourceAttribute"/> から、マウントするソースを作ります。
/// </summary>
/// <remarks>
/// <para>
/// <b>バックエンド実装者向けのインターフェースです。</b> アプリのコードで実装する必要はありません。
/// </para>
/// <para>
/// コアは、デスクトップ向けにビルドされた属性を読む既定の実装を、アプリの構築時に (まだ登録されていなければ) 登録します。
/// 別の形式のマニフェストを読む環境 (Web など) のバックエンドは、<c>Build</c> より前に
/// <c>Use&lt;IAssetManifestLoader, TImpl&gt;()</c> で自前の実装を登録して、既定の実装を置き換えます。
/// </para>
/// <para>
/// 実装は、自分の環境向けにビルドされていない属性 (<see cref="PrometeAssetSourceAttribute.Environment"/>) や、
/// 知らない種類 (<see cref="PrometeAssetSourceAttribute.Kind"/>) を受け取ったら、原因の分かるメッセージで例外をスローしてください。
/// </para>
/// </remarks>
public interface IAssetManifestLoader
{
    /// <summary>
    /// 属性 1 つ分の読み込み元を開いて、ソースを作ります。
    /// </summary>
    /// <param name="assembly">属性を持つアセンブリ。埋め込みリソースを読むときに使います。</param>
    /// <param name="attribute">読み込み元を表す属性。</param>
    /// <returns>マウントするソース。<see cref="IDisposable"/> を実装していれば、所有権は呼び出し元 (<see cref="AssetManager"/>) に移ります。</returns>
    /// <exception cref="System.IO.FileNotFoundException">マニフェストやパッケージが見つからない場合。</exception>
    /// <exception cref="InvalidOperationException">別の環境向けにビルドされた属性の場合。</exception>
    /// <exception cref="NotSupportedException">知らない種類の属性や、対応していない形式の場合。</exception>
    public IAssetSource CreateSource(Assembly assembly, PrometeAssetSourceAttribute attribute);
}
