using System;
using System.Collections.Generic;
using System.Text;

namespace Promete.Markup;

/// <summary>
/// 文字列をPTMLとして解析します。
/// </summary>
public static class PtmlParser
{
    /// <summary>
    /// PTMLを解析します。
    /// </summary>
    /// <param name="ptml">PTML文字列。</param>
    /// <param name="throwsIfError">エラーがあったときに例外をスローするかどうか。</param>
    /// <returns></returns>
    /// <exception cref="PtmlParserException"><paramref name="throwsIfError" />が<c>true</c>であれば、エラーがあったときにスローされます。</exception>
    public static (string plainText, IReadOnlyList<PtmlDecoration> decorations) Parse(string ptml,
        bool throwsIfError = false)
    {
        // プレーンテキストの部分を格納。最終的にreturnする。
        var plainTextBuilder = new StringBuilder();
        // 解析したタグを格納。最終的にreturnする
        var decorations = new List<PtmlDecoration>();
        // 解析した開始タグを格納。Peekすることで終了タグを解析し、終了タグが一致したらPopしてdecorationsへ
        var decorationStack = new Stack<PtmlDecoration>();
        // タグ解析時点のプレーンテキスト位置を格納。
        var rangeStartStack = new Stack<int>();
        // タグ名を一時保管する。タグ記法が終わったらclear
        var tagNameBuilder = new StringBuilder();
        // 属性を一時保管する。タグ記法が終わったらclear
        var attributeBuilder = new StringBuilder();
        // エスケープシーケンスを一時保管する。エスケープシーケンスが終わったらclear
        var escapeBuilder = new StringBuilder();

        var state = State.PlainText;
        var i = -1;

        try
        {
            foreach (var c in ptml)
            {
                i++;
                var tagName = tagNameBuilder.ToString();
                switch (state)
                {
                    case State.PlainText:
                        if (c == '<')
                        {
                            state = State.StartTagName;
                            rangeStartStack.Push(plainTextBuilder.Length);
                            continue;
                        }

                        if (c == '&')
                        {
                            state = State.EscapeSequence;
                            continue;
                        }

                        plainTextBuilder.Append(c);
                        break;

                    case State.EscapeSequence:
                        if (c == ';')
                        {
                            switch (escapeBuilder.ToString().ToLowerInvariant())
                            {
                                case "lt":
                                    plainTextBuilder.Append('<');
                                    break;
                                case "gt":
                                    plainTextBuilder.Append('>');
                                    break;
                                case "amp":
                                    plainTextBuilder.Append('&');
                                    break;
                                default:
                                    plainTextBuilder.Append($"&{escapeBuilder.ToString()};");
                                    break;
                            }

                            escapeBuilder.Clear();
                            state = State.PlainText;
                            continue;
                        }

                        escapeBuilder.Append(c);
                        break;

                    case State.StartTagName:
                        if (c == '/')
                        {
                            if (tagNameBuilder.Length > 0)
                                throw new PtmlParserException("Invalid token /. Expected tag name.", i);

                            // 終了タグの場合、先にpushしたrangeStartを破棄する
                            _ = rangeStartStack.Pop();
                            state = State.EndTagName;
                            continue;
                        }

                        if (c == '=')
                        {
                            if (tagNameBuilder.Length == 0)
                                throw new PtmlParserException("Invalid token =. Expected tag name.", i);
                            state = State.Attribute;
                            continue;
                        }

                        if (c == '>')
                        {
                            if (tagNameBuilder.Length == 0)
                                throw new PtmlParserException("Invalid token >. Expected tag name.", i);

                            decorationStack.Push(new PtmlDecoration(0, 0, tagName, attributeBuilder.ToString()));
                            tagNameBuilder.Clear();
                            attributeBuilder.Clear();
                            state = State.PlainText;
                            continue;
                        }

                        if (!char.IsLetterOrDigit(c))
                            throw new PtmlParserException($"Token {c} cannot use as tag name.", i);
                        tagNameBuilder.Append(c);
                        break;
                    case State.Attribute:
                        if (c == '>')
                        {
                            if (attributeBuilder.Length == 0)
                                throw new PtmlParserException("Invalid token >. Expected attribute.", i);
                            decorationStack.Push(new PtmlDecoration(0, 0, tagName, attributeBuilder.ToString()));
                            tagNameBuilder.Clear();
                            attributeBuilder.Clear();
                            state = State.PlainText;
                            continue;
                        }

                        attributeBuilder.Append(c);
                        break;
                    case State.EndTagName:
                        if (c == '>')
                        {
                            if (tagNameBuilder.Length == 0)
                                throw new PtmlParserException("Invalid token >. Expected tag name.", i);
                            if (!decorationStack.TryPop(out var startTag) ||
                                !startTag.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                                throw new PtmlParserException(
                                    $"End tag \"{tagName}\" does not match to \"{startTag.TagName}\"", i);
                            startTag.Start = rangeStartStack.Pop();
                            startTag.End = plainTextBuilder.Length;
                            decorations.Add(startTag);
                            tagNameBuilder.Clear();
                            state = State.PlainText;
                            continue;
                        }

                        if (!char.IsLetterOrDigit(c))
                            throw new PtmlParserException($"Token {c} cannot use as tag name.", i);
                        tagNameBuilder.Append(c);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid State: " + state);
                }
            }

            switch (state)
            {
                case State.StartTagName:
                    plainTextBuilder.Append("<" + tagNameBuilder);
                    _ = rangeStartStack.Pop();
                    break;
                case State.Attribute:
                    plainTextBuilder.Append("<" + tagNameBuilder + "=" + attributeBuilder);
                    _ = rangeStartStack.Pop();
                    break;
                case State.EndTagName:
                    plainTextBuilder.Append("</" + tagNameBuilder);
                    break;
                case State.EscapeSequence:
                    plainTextBuilder.Append("&" + escapeBuilder);
                    break;
            }

            // 閉じられていないタグを順番に末尾に追加する
            while (decorationStack.TryPop(out var startTag))
                decorations.Add(startTag with { Start = rangeStartStack.Pop(), End = plainTextBuilder.Length });
        }
        catch (PtmlParserException)
        {
            if (throwsIfError) throw;
            return (ptml, []);
        }

        return (plainTextBuilder.ToString(), decorations.AsReadOnly());
    }

    private enum State
    {
        PlainText,
        StartTagName,
        Attribute,
        EndTagName,
        EscapeSequence
    }
}
