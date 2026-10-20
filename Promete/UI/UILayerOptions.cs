using System;

namespace Promete.UI;

/// <summary>
/// <see cref="UILayer"/> の挙動を指定します。
/// </summary>
internal sealed record UILayerOptions
{
    /// <summary>
    /// モーダルかどうか。<see cref="UIManager.CurrentModal"/> の対象になります。
    /// </summary>
    public bool IsModal { get; init; }

    /// <summary>
    /// ルート要素そのものへのクリックを、レイヤーの外側へのクリックとして扱うかどうか。
    /// 画面全体を覆うオーバーレイ付きのモーダルのように、ルートが背景を兼ねる場合に true にします。
    /// </summary>
    public bool RootIsBackdrop { get; init; }

    /// <summary>
    /// レイヤーの外側がクリックされたときに呼ばれます。null の場合は何も起きません。
    /// いずれの場合も、そのクリックは下の要素には届きません。
    /// </summary>
    public Action? OnOutsideClick { get; init; }

    /// <summary>
    /// キャンセル操作 (Esc) が行われたときに呼ばれます。null の場合は何も起きません。
    /// </summary>
    public Action? OnCancel { get; init; }

    /// <summary>
    /// レイヤーを閉じたときに、表示前にフォーカスしていた要素へフォーカスを戻すかどうか。
    /// </summary>
    public bool RestoreFocus { get; init; }
}
