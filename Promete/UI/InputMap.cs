using System;
using System.Collections.Generic;
using Promete.Input;

namespace Promete.UI;

/// <summary>
/// キーボードのキーを名前付きアクションにマッピングする、UI 専用の最小限の入力マップです。
/// 将来 Unified Input API に置き換えることを想定しており、UI が必要とする機能に留めています。
/// </summary>
internal sealed class InputMap
{
    private readonly Dictionary<string, InputAction> _actions = new();
    private readonly List<(InputAction Action, KeyCode Key)> _bindings = [];

    /// <summary>
    /// 新しいアクションを定義します。
    /// </summary>
    /// <param name="name">アクション名。</param>
    /// <returns>作成された InputAction。</returns>
    public InputAction AddAction(string name)
    {
        var action = new InputAction(name);
        _actions[name] = action;
        return action;
    }

    /// <summary>
    /// 定義済みのアクションを取得します。
    /// </summary>
    /// <param name="name">アクション名。</param>
    /// <exception cref="KeyNotFoundException">アクションが存在しない場合。</exception>
    public InputAction GetAction(string name) => _actions[name];

    /// <summary>
    /// キーボードのキーをアクションにバインドします。
    /// </summary>
    /// <param name="actionName">定義済みのアクション名。</param>
    /// <param name="key">バインドするキー。</param>
    /// <returns>メソッドチェーン用の自身。</returns>
    public InputMap BindKey(string actionName, KeyCode key)
    {
        _bindings.Add((_actions[actionName], key));
        return this;
    }

    /// <summary>
    /// 全アクションの状態を更新します。毎フレーム呼び出してください。
    /// </summary>
    /// <param name="keyboard">キーボード入力。</param>
    public void Update(Keyboard keyboard) => Update(key => keyboard.KeyOf(key).IsPressed);

    /// <summary>
    /// 全アクションの状態を、キーの押下判定を差し替えて更新します。
    /// </summary>
    /// <param name="isPressed">キーが押されているかを返す関数。</param>
    internal void Update(Func<KeyCode, bool> isPressed)
    {
        foreach (var action in _actions.Values)
        {
            action.WasPressed = action.IsPressed;
            action.IsPressed = false;
        }

        foreach (var (action, key) in _bindings)
        {
            if (isPressed(key))
                action.IsPressed = true;
        }
    }
}
