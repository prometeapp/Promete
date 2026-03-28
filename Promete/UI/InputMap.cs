using System;
using System.Collections.Generic;
using Promete.Input;

namespace Promete.UI;

/// <summary>
/// 入力アクションの状態を表します。
/// </summary>
public class InputAction
{
	internal bool PreviousPressed;
	internal bool CurrentPressed;

	internal InputAction(string name)
	{
		Name = name;
	}

	/// <summary>アクション名。</summary>
	public string Name { get; }

	/// <summary>現在押されているかどうか。</summary>
	public bool IsPressed => CurrentPressed;

	/// <summary>このフレームで押された瞬間かどうか。</summary>
	public bool IsJustPressed => CurrentPressed && !PreviousPressed;

	/// <summary>このフレームで離された瞬間かどうか。</summary>
	public bool IsJustReleased => !CurrentPressed && PreviousPressed;
}

/// <summary>
/// マウス・キーボード・ゲームパッドの入力を統一的なアクションに抽象化する入力マップです。
/// プログラムからの仮想入力発火にも対応します。
/// </summary>
public class InputMap
{
	private readonly Dictionary<string, InputAction> _actions = new();
	private readonly List<InputBinding> _bindings = [];
	private readonly HashSet<string> _fireQueue = [];
	private readonly Dictionary<string, bool> _programmaticState = new();

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
	/// <returns>対応する InputAction。</returns>
	/// <exception cref="KeyNotFoundException">アクションが存在しない場合。</exception>
	public InputAction GetAction(string name) => _actions[name];

	/// <summary>
	/// アクションが定義されているかどうかを確認します。
	/// </summary>
	public bool HasAction(string name) => _actions.ContainsKey(name);

	/// <summary>
	/// キーボードのキーをアクションにバインドします。
	/// </summary>
	public InputMap BindKey(string actionName, KeyCode key)
	{
		_bindings.Add(new InputBinding(actionName, InputBindingType.Key, Key: key));
		return this;
	}

	/// <summary>
	/// マウスボタンをアクションにバインドします。
	/// </summary>
	public InputMap BindMouseButton(string actionName, MouseButtonType button)
	{
		_bindings.Add(new InputBinding(actionName, InputBindingType.MouseButton, Mouse: button));
		return this;
	}

	/// <summary>
	/// ゲームパッドボタンをアクションにバインドします。
	/// </summary>
	public InputMap BindGamepadButton(string actionName, GamepadButtonType button)
	{
		_bindings.Add(new InputBinding(actionName, InputBindingType.GamepadButton, Gamepad: button));
		return this;
	}

	/// <summary>
	/// 指定したアクションから特定のキーバインドを解除します。
	/// </summary>
	public InputMap UnbindKey(string actionName, KeyCode key)
	{
		_bindings.RemoveAll(b => b.ActionName == actionName && b.Type == InputBindingType.Key && b.Key == key);
		return this;
	}

	/// <summary>
	/// 指定したアクションから特定のマウスボタンバインドを解除します。
	/// </summary>
	public InputMap UnbindMouseButton(string actionName, MouseButtonType button)
	{
		_bindings.RemoveAll(b => b.ActionName == actionName && b.Type == InputBindingType.MouseButton && b.Mouse == button);
		return this;
	}

	/// <summary>
	/// 指定したアクションから特定のゲームパッドボタンバインドを解除します。
	/// </summary>
	public InputMap UnbindGamepadButton(string actionName, GamepadButtonType button)
	{
		_bindings.RemoveAll(b => b.ActionName == actionName && b.Type == InputBindingType.GamepadButton && b.Gamepad == button);
		return this;
	}

	/// <summary>
	/// 指定したアクションの全バインドを解除します。
	/// </summary>
	public InputMap UnbindAll(string actionName)
	{
		_bindings.RemoveAll(b => b.ActionName == actionName);
		return this;
	}

	/// <summary>
	/// 指定したアクションを1フレームだけ発火します。
	/// 次の Update() で pressed になり、その次の Update() で自動的にリセットされます。
	/// </summary>
	/// <param name="actionName">発火するアクション名。</param>
	public void Fire(string actionName)
	{
		_fireQueue.Add(actionName);
	}

	/// <summary>
	/// 指定したアクションの押下状態をプログラムから制御します。
	/// 明示的に false に設定するまで状態が維持されます。
	/// </summary>
	/// <param name="actionName">アクション名。</param>
	/// <param name="pressed">押下状態。</param>
	public void SetPressed(string actionName, bool pressed)
	{
		_programmaticState[actionName] = pressed;
	}

	/// <summary>
	/// プログラムから設定した押下状態をクリアします。
	/// </summary>
	/// <param name="actionName">アクション名。</param>
	public void ClearProgrammaticState(string actionName)
	{
		_programmaticState.Remove(actionName);
	}

	/// <summary>
	/// 全アクションの状態を更新します。毎フレーム呼び出してください。
	/// </summary>
	/// <param name="keyboard">キーボード入力。</param>
	/// <param name="mouse">マウス入力。</param>
	/// <param name="gamepads">ゲームパッド入力（省略可）。</param>
	public void Update(Keyboard keyboard, Mouse mouse, Gamepads? gamepads = null)
	{
		// 前フレームの状態を保存し、リセット
		foreach (var action in _actions.Values)
		{
			action.PreviousPressed = action.CurrentPressed;
			action.CurrentPressed = false;
		}

		// 物理入力バインドを評価
		foreach (var binding in _bindings)
		{
			if (!_actions.TryGetValue(binding.ActionName, out var action)) continue;

			if (EvaluateBinding(binding, keyboard, mouse, gamepads))
			{
				action.CurrentPressed = true;
			}
		}

		// プログラムからの持続的押下状態を適用
		foreach (var (name, pressed) in _programmaticState)
		{
			if (_actions.TryGetValue(name, out var action) && pressed)
			{
				action.CurrentPressed = true;
			}
		}

		// Fire キューを適用（1フレームだけ）
		foreach (var name in _fireQueue)
		{
			if (_actions.TryGetValue(name, out var action))
			{
				action.CurrentPressed = true;
			}
		}
		_fireQueue.Clear();
	}

	private static bool EvaluateBinding(InputBinding binding, Keyboard keyboard, Mouse mouse, Gamepads? gamepads)
	{
		return binding.Type switch
		{
			InputBindingType.Key => keyboard.KeyOf(binding.Key!.Value).IsPressed,
			InputBindingType.MouseButton => mouse[binding.Mouse!.Value].IsPressed,
			InputBindingType.GamepadButton => gamepads?[0]?[binding.Gamepad!.Value].IsPressed ?? false,
			_ => false,
		};
	}

	private enum InputBindingType
	{
		Key,
		MouseButton,
		GamepadButton,
	}

	private record InputBinding(
		string ActionName,
		InputBindingType Type,
		KeyCode? Key = null,
		MouseButtonType? Mouse = null,
		GamepadButtonType? Gamepad = null
	);
}
