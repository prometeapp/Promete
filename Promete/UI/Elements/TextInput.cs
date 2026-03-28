using System;
using Promete.Input;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// テキスト入力UI要素です。
/// フォーカス中にキーボード入力を受け付けます。
/// </summary>
public class TextInput : UIElement
{
	private readonly Text _textNode;
	private readonly Text _placeholderNode;
	private string _text = "";
	private string _placeholder = "";
	private int _cursorPosition;
	private int _cursorBlinkCounter;
	private bool _cursorVisible;
	private bool _needsBufferFlush;
	private IUIStyle<TextInput> _style;

	/// <summary>
	/// TextInput の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="placeholder">プレースホルダーテキスト。</param>
	/// <param name="style">適用するスタイル。null の場合は DefaultTextInputStyle が使用されます。</param>
	public TextInput(string placeholder = "", IUIStyle<TextInput>? style = null)
	{
		_style = style ?? new DefaultTextInputStyle();
		_placeholder = placeholder;
		_placeholderNode = new Text(placeholder);
		_textNode = new Text("");
		Add(_placeholderNode);
		Add(_textNode);
		Size = (200, 32);
		IsFocusable = true;
		_needsBufferFlush = true;

		GotFocus += () => _needsBufferFlush = true;
	}

	/// <summary>入力テキストを取得または設定します。</summary>
	public string Text
	{
		get => _text;
		set
		{
			if (_text == value) return;
			_text = value;
			_cursorPosition = Math.Min(_cursorPosition, _text.Length);
			_textNode.Content = _text;
			NotifyStyleDirty();
			TextChanged?.Invoke(_text);
		}
	}

	/// <summary>プレースホルダーテキストを取得または設定します。</summary>
	public string Placeholder
	{
		get => _placeholder;
		set
		{
			_placeholder = value;
			_placeholderNode.Content = value;
			NotifyStyleDirty();
		}
	}

	/// <summary>カーソル位置を取得します。</summary>
	public int CursorPosition
	{
		get => _cursorPosition;
		set => _cursorPosition = Math.Clamp(value, 0, _text.Length);
	}

	/// <summary>テキスト表示用の Text ノード。</summary>
	public Text TextNode => _textNode;

	/// <summary>プレースホルダー表示用の Text ノード。</summary>
	public Text PlaceholderNode => _placeholderNode;

	/// <summary>テキストの左パディング。</summary>
	public int PaddingLeft { get; set; } = 6;

	/// <summary>カーソルの点滅間隔（フレーム数）。</summary>
	public int CursorBlinkInterval { get; set; } = 30;

	/// <summary>カーソル表示状態。スタイルから参照されます。</summary>
	public bool IsCursorVisible => _cursorVisible && IsFocused;

	/// <summary>スタイルを取得または設定します。</summary>
	public IUIStyle<TextInput> Style
	{
		get => _style;
		set
		{
			_style = value;
			NotifyStyleDirty();
		}
	}

	/// <summary>テキストが変更されたときに発火します。</summary>
	public event Action<string>? TextChanged;

	/// <summary>Enterキーが押されたときに発火します。</summary>
	public event Action<string>? Submitted;

	/// <summary>
	/// フォーカス中にキーボード入力を処理します。
	/// UIManager から毎フレーム呼ばれます。
	/// </summary>
	internal void ProcessInput(Keyboard keyboard)
	{
		if (!IsFocused) return;

		// フォーカス直後はバッファに溜まった入力を捨てる
		if (_needsBufferFlush)
		{
			keyboard.GetString();
			_needsBufferFlush = false;
			return;
		}

		// カーソル点滅
		_cursorBlinkCounter++;
		if (_cursorBlinkCounter >= CursorBlinkInterval)
		{
			_cursorVisible = !_cursorVisible;
			_cursorBlinkCounter = 0;
			NotifyStyleDirty();
		}

		// 文字入力
		var input = keyboard.GetString();
		if (input.Length > 0)
		{
			_text = _text.Insert(_cursorPosition, input);
			_cursorPosition += input.Length;
			_textNode.Content = _text;
			ResetCursorBlink();
			NotifyStyleDirty();
			TextChanged?.Invoke(_text);
		}

		// BackSpace
		if (keyboard.BackSpace.IsKeyDown && _cursorPosition > 0)
		{
			_text = _text.Remove(_cursorPosition - 1, 1);
			_cursorPosition--;
			_textNode.Content = _text;
			ResetCursorBlink();
			NotifyStyleDirty();
			TextChanged?.Invoke(_text);
		}

		// Delete
		if (keyboard.Delete.IsKeyDown && _cursorPosition < _text.Length)
		{
			_text = _text.Remove(_cursorPosition, 1);
			_textNode.Content = _text;
			ResetCursorBlink();
			NotifyStyleDirty();
			TextChanged?.Invoke(_text);
		}

		// カーソル移動
		if (keyboard.Left.IsKeyDown && _cursorPosition > 0)
		{
			_cursorPosition--;
			ResetCursorBlink();
			NotifyStyleDirty();
		}

		if (keyboard.Right.IsKeyDown && _cursorPosition < _text.Length)
		{
			_cursorPosition++;
			ResetCursorBlink();
			NotifyStyleDirty();
		}

		if (keyboard.Home.IsKeyDown)
		{
			_cursorPosition = 0;
			ResetCursorBlink();
			NotifyStyleDirty();
		}

		if (keyboard.End.IsKeyDown)
		{
			_cursorPosition = _text.Length;
			ResetCursorBlink();
			NotifyStyleDirty();
		}

		// Enter で Submit
		if (keyboard.Enter.IsKeyDown)
		{
			Submitted?.Invoke(_text);
		}
	}

	protected override void ApplyStyle(UIElementState state)
	{
		_style.Apply(this, state);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// プレースホルダーの表示切り替え
		_placeholderNode.IsVisible = string.IsNullOrEmpty(_text) && !IsFocused;

		// テキストとプレースホルダーの位置
		var textY = (Size.Y - _textNode.Size.Y) / 2f;
		if (_textNode.Size.Y > 0)
			_textNode.Location = new Vector(PaddingLeft, textY);
		if (_placeholderNode.Size.Y > 0)
			_placeholderNode.Location = new Vector(PaddingLeft, textY);
	}

	private void ResetCursorBlink()
	{
		_cursorVisible = true;
		_cursorBlinkCounter = 0;
	}
}
