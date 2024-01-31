using System;

namespace Promete;

/// <summary>
/// 文字列に関する拡張メソッドを提供します。
/// </summary>
public static class StringExtension
{
	/// <summary>
	/// この文字列の指定した位置から始まる部分を指定した文字列で置き換え、新たな文字列として返します。
	/// </summary>
	/// <param name="str">この文字列。</param>
	/// <param name="index">置き換えを開始する位置。</param>
	/// <param name="replace">置き換える文字列。</param>
	/// <returns></returns>
	public static string ReplaceAt(this string str, int index, string replace)
		=> str.Remove(index, Math.Min(replace.Length, str.Length - index))
			.Insert(index, replace);
}
