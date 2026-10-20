using System;

namespace Promete.UI;

/// <summary>
/// UIManager のレイヤースタックに積まれる、最前面に表示される UI の単位です。
/// モーダルやポップアップが該当し、最上位のレイヤーの子孫だけが入力を受け取ります。
/// </summary>
internal sealed class UILayer(UIElement root, UILayerOptions options)
{
    /// <summary>レイヤーのルート要素。</summary>
    public UIElement Root { get; } = root;

    /// <summary>レイヤーの挙動の設定。</summary>
    public UILayerOptions Options { get; } = options;

    /// <summary>レイヤーを閉じたときにフォーカスを戻す要素。</summary>
    public UIElement? ReturnFocusTo { get; set; }
}
