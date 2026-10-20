using System.Collections.Specialized;

namespace Promete.UI;

/// <summary>
/// 項目のコレクションが変更されたときに、選択位置が同じ項目を指し続けるよう補正します。
/// </summary>
internal static class SelectionAdjuster
{
    /// <summary>
    /// コレクションの変更後の、選択位置を求めます。
    /// 選択中の項目が取り除かれた場合や、コレクションが作り直された場合は、未選択 (-1) になります。
    /// </summary>
    /// <param name="selected">変更前の選択位置。未選択は -1。</param>
    /// <param name="e">コレクションの変更の内容。</param>
    public static int Adjust(int selected, NotifyCollectionChangedEventArgs e)
    {
        if (selected < 0)
            return selected;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                // 選択位置以前に挿入されたら、その分だけ後ろにずれる
                var count = e.NewItems?.Count ?? 0;
                return e.NewStartingIndex <= selected ? selected + count : selected;
            }

            case NotifyCollectionChangedAction.Remove:
            {
                var start = e.OldStartingIndex;
                var count = e.OldItems?.Count ?? 0;
                if (selected < start)
                    return selected;

                // 選択中の項目が取り除かれた場合は未選択、それより前が取り除かれた場合は前にずれる
                return selected < start + count ? -1 : selected - count;
            }

            case NotifyCollectionChangedAction.Move:
            {
                var from = e.OldStartingIndex;
                var to = e.NewStartingIndex;
                if (selected == from)
                    return to;
                if (from < selected && to >= selected)
                    return selected - 1;
                if (from > selected && to <= selected)
                    return selected + 1;
                return selected;
            }

            case NotifyCollectionChangedAction.Replace:
                // 同じ位置の項目が入れ替わるだけなので、選択位置は変わらない
                return selected;

            default:
                return -1;
        }
    }
}
