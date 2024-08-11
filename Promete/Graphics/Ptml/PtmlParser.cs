using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Promete.Graphics.Ptml;

class PtmlParser
{
	internal static (string plainText, IReadOnlyList<PtmlDecoration> decorations) Parse(string ptml)
	{
		// プレーンテキストの部分を格納
		var plainTextBuilder = new StringBuilder();
		// 解析中のメタテキストを格納。タグがエラーになったときにplainTextBuilderにAppendして自身をクリアする。
		var metaTextBuilder = new StringBuilder();
		//
		var decorations = new List<RichTextRun>();
		var stack = new Stack<RichTextRun>();
		var tagNameBuilder = new StringBuilder();
		var attributeBuilder = new StringBuilder();

		var state = State.PlainText;

		foreach (var c in ptml)
		{
			switch (state)
			{
				case State.PlainText:
					if (c == '<')
					{
						state = State.StartTagName;
						continue;
					}
					plainTextBuilder.Append(c);
					break;

				case State.StartTagName:
					if (c == '/')
					{
						if (tagNameBuilder.Length > 0) throw new PtmlParserException("Invalid token /. Expected tag name.");
						state = State.EndTagName;
						continue;
					}
					if (c == '=')
					{
						if (tagNameBuilder.Length == 0) throw new PtmlParserException("Invalid token =. Expected tag name.");
						tagNameBuilder.Append(c);
						state = State.Attribute;
						continue;
					}
					if (c == '>')
					{
						if (tagNameBuilder.Length == 0) throw new PtmlParserException("Invalid token >. Expected tag name.");
						decorations.Add(new RichTextRun(tagNameBuilder.ToString(), attributeBuilder.ToString()));
						tagNameBuilder.Clear();
						attributeBuilder.Clear();
						state = State.PlainText;
						continue;
					}

					if (!char.IsLetterOrDigit(c)) throw new PtmlParserException($"Invalid token {c}. Expected tag name.");
					tagNameBuilder.Append(c);
					break;
				case State.Attribute:
					break;
				case State.EndTagName:
					break;
				default:
					throw new InvalidOperationException("Invalid State: " + state);
			}
		}

		return (plainTextBuilder.ToString(), decorations.AsReadOnly());
	}

	enum State
	{
		PlainText,
		StartTagName,
		Attribute,
		EndTagName,
	}
}
