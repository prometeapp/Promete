namespace Promete.UI;

/// <summary>
/// UI要素の視覚的な描画を担当するインターフェースです。
/// 要素の状態に基づいて、内部ノードの生成・更新を行います。
/// </summary>
/// <typeparam name="T">対象となるUI要素の型。</typeparam>
public interface IUIStyle<in T> where T : UIElement
{
	/// <summary>
	/// 要素の状態が変化したときに呼び出されます。
	/// 要素の子ノード（背景Shapeなど）を必要に応じて再作成・差し替えします。
	/// </summary>
	/// <param name="element">対象のUI要素。</param>
	/// <param name="state">現在のインタラクション状態。</param>
	void Apply(T element, UIElementState state);
}
