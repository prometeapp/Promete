using System;

namespace Promete.UI;

/// <summary>
/// <see cref="UIElement"/> 向けの Setup API 拡張メソッドです。
/// </summary>
public static class UISetupApiExtension
{
	/// <summary>
	/// クリック時のハンドラを設定します。
	/// </summary>
	public static T OnClick<T>(this T element, Action handler) where T : UIElement
	{
		element.Clicked += handler;
		return element;
	}

	/// <summary>
	/// 要素の有効/無効状態を設定します。
	/// </summary>
	public static T Enabled<T>(this T element, bool enabled) where T : UIElement
	{
		element.IsEnabled = enabled;
		return element;
	}

	/// <summary>
	/// 要素のフォーカス可否を設定します。
	/// </summary>
	public static T Focusable<T>(this T element, bool focusable) where T : UIElement
	{
		element.IsFocusable = focusable;
		return element;
	}

	/// <summary>
	/// フォーカスナビゲーションの順序を設定します。
	/// </summary>
	public static T NavigationOrder<T>(this T element, int order) where T : UIElement
	{
		element.NavigationOrder = order;
		return element;
	}

	/// <summary>
	/// フォーカスグループを設定します。
	/// </summary>
	public static T InFocusGroup<T>(this T element, FocusGroup group) where T : UIElement
	{
		element.FocusGroup = group;
		return element;
	}
}
