using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cartelet.Html;
using System.Runtime.CompilerServices;

namespace Cartelet.Html
{
    public class HtmlParser
    {
        private static Char[] TagNameEndChars = new[] { ' ', '>', '\r', '\n', '\t' };
        private static readonly char[] ClassNameSeparator = new[] { ' ', '\n', '\t', '\r' };

        enum AttributeReadState
        {
            ExpectName,
            Name,
            ExpectEqualOrNextAttrName,
            Equal,
            QuoteStart,
            ValueStart,
        }

        public static NodeInfo Parse(String content)
        {
            var pos = 0;
            var contentEndPos = content.Length - 1;

            var rootNode = new NodeInfo(null, new ClassList(), new AttributesDictionary());
            var node = rootNode;
            var tagNameUpperCache = new Dictionary<String, String>(StringComparer.Ordinal);
            var curAttrName = new StringBuilder();
            var curAttrValue = new StringBuilder();

            while (true)
            {
                var isXmlStyleSelfClose = false;
                var isEndTagOrSelfClose = false;
                // タグの開始を見つける
                pos = content.IndexOf('<', pos);
                if (pos == -1)
                    break;

                // 要素の名前をまず切り出す
                var tagNameEndPos = content.IndexOfAny(TagNameEndChars, pos);
                if (tagNameEndPos == -1 || pos + 1 == tagNameEndPos)
                {
                    // Report:Invalid
                    pos = tagNameEndPos;
                    continue;
                }
                if (content[tagNameEndPos - 1] == '/')
                {
                    tagNameEndPos--;
                }
                var tagName = content.Substring(pos + 1, tagNameEndPos - pos - 1);

                // コメントは見なかったことにする(スキップする)
                if (tagName.StartsWith("!--", StringComparison.Ordinal))
                {
                    // -- が含めないとかそういうことは考えず単純に --> を探す
                    var commentClose = content.IndexOf("-->", pos + 4, StringComparison.Ordinal);
                    pos = (commentClose == -1) ? contentEndPos : commentClose;
                    continue;
                }

                var tagNameUpper = tagNameUpperCache.ContainsKey(tagName)
                                        ? tagNameUpperCache[tagName]
                                        : tagNameUpperCache[tagName] = tagName.ToUpper(CultureInfo.InvariantCulture);
                if (tagName[0] == '/')
                {
                    // 閉じタグ
                    pos = tagNameEndPos;
                    if (node.Parent == null)
                    {
                        // 外側にでるのはなんかおかしい
                        // Report:Invalid
                    }
                    else if (node.TagNameUpper == "SCRIPT" && tagNameUpper != "/SCRIPT")
                    {
                        // script要素内は必ずscript要素で終わらないとダメ
                    }
                    else if (tagNameUpper.Substring(1) != node.TagNameUpper)
                    {
                        // タグ名が違うよ…
                        // Report:Invalid
#if DEBUG
                        var lineEndPos = content.IndexOf('\n');
                        var line = 0;
                        while (lineEndPos != -1 && lineEndPos < pos)
                        {
                            line++;
                            lineEndPos = content.IndexOf('\n', lineEndPos+1);
                        }

                        Trace.WriteLine(String.Format("Unmatched Tag: Line: {0}; TagName={1}; EndTagName={2}", line, node.TagNameUpper, tagNameUpper.Substring(1)));
#endif
                        node.EndOfElement = pos + 1;
                        node = node.Parent;
                    }
                    else
                    {
                        node.EndOfElement = pos + 1;
                        node = node.Parent;
                    }
                    continue;
                }
                else
                {
                    switch (tagNameUpper)
                    {
                        case "?XML":
                        case "?XSL-STYLESHEET":
                        case "!DOCTYPE":
                        case "BR":
                        case "HR":
                        case "IMG":
                        case "META":
                        case "INPUT":
                        case "LINK":
                            isEndTagOrSelfClose = true;
                            break;
                        default:
                            break;
                    }
                }

                // 属性とは
                var id = null as String;
                var classNames = null as String;
                var tagEndPos = tagNameEndPos;
                var attributes = new AttributesDictionary();
                if (tagNameEndPos <= contentEndPos && content[tagNameEndPos] == '>')
                {
                    // タグの終わり(属性なし)
                }
                else
                {
                    // 属性部分をがんばる
                    var attributeScanPos = tagNameEndPos;
                    var attrState = AttributeReadState.ExpectName; // ExpectName -> Name -> ExpectEqualOrNextAttrName -> Equal -> ...
                    var curAttrQuote = '"';
                    var isLast = false;
                    curAttrName.Clear();
                    curAttrValue.Clear();

                    while (true)
                    {
                        if (attributeScanPos >= contentEndPos || isLast)
                        {
                            if (curAttrName.Length > 0)
                            {
                                SetAttributeValue(curAttrName, curAttrValue, attributes, ref id, ref classNames);
                            }
                            tagEndPos = attributeScanPos;
                            break;
                        }

                        var c = content[attributeScanPos++];
                        if (attrState == AttributeReadState.ExpectName)
                        {
                            // 名前の開始を探す
                            if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                            {
                                if ((c == '>') || (c == '/' && (attributeScanPos <= contentEndPos) && content[attributeScanPos] == '>'))
                                {
                                    attributeScanPos -= (c == '>' ? 1 : 0);
                                    isXmlStyleSelfClose = (c != '>');
                                    isLast = true;
                                    continue;
                                }
                                attrState = AttributeReadState.Name;
                                curAttrName.Append(c);
                            }
                        }
                        else if (attrState == AttributeReadState.Name)
                        {
                            // 名前部分を読む
                            if (c == '=')
                            {
                                attrState = AttributeReadState.Equal;
                            }
                            else if ((c == '>') || (c == '/' && (attributeScanPos <= contentEndPos) && content[attributeScanPos] == '>'))
                            {
                                attributeScanPos -= (c == '>' ? 1 : 2);
                                isLast = true;
                                continue;
                            }
                            else if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                            {
                                curAttrName.Append(c);
                            }
                            else
                            {
                                attrState = AttributeReadState.ExpectEqualOrNextAttrName;
                            }
                        }
                        else if (attrState == AttributeReadState.ExpectEqualOrNextAttrName)
                        {
                            // =か次の名前にあたるまで
                            if (c == '=')
                            {
                                attrState = AttributeReadState.Equal;
                            }
                            else if ((c == '>') || (c == '/' && (attributeScanPos <= contentEndPos) && content[attributeScanPos] == '>'))
                            {
                                // 終わり
                                attributeScanPos -= (c == '>' ? 1 : 2);
                                isXmlStyleSelfClose = (c == '/');
                                isLast = true;
                                continue;
                            }
                            else if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                            {
                                // 次の名前
                                attrState = AttributeReadState.Name;
                                SetAttributeValue(curAttrName, curAttrValue, attributes, ref id, ref classNames);
                                curAttrName.Append(c);
                            }
                        }
                        else if (attrState == AttributeReadState.Equal)
                        {
                            // =なので次はクォートを探すかそのまま値として読み込む
                            if (c == '"' || c == '\'')
                            {
                                attrState = AttributeReadState.QuoteStart;
                                curAttrQuote = c;
                            }
                            else if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                            {
                                attrState = AttributeReadState.ValueStart;
                                curAttrQuote = '\0';
                                curAttrValue.Append(c);
                            }
                            else if (c == '>')
                            {
                                attributeScanPos--;
                                isLast = true;
                                continue;
                            }
                        }
                        else if (attrState == AttributeReadState.QuoteStart)
                        {
                            // 値の " or' の位置にきたので最後まで読みだす
                            if (c == curAttrQuote)
                            {
                                // 閉じまたは値の最後
                                attrState = AttributeReadState.ExpectName; // AttrName
                                SetAttributeValue(curAttrName, curAttrValue, attributes, ref id, ref classNames);
                            }
                            else
                            {
                                curAttrValue.Append(c);
                            }
                        }
                        else if (attrState == AttributeReadState.ValueStart)
                        {
                            // 値の開始位置にきたので最後まで読みだす
                            if (c == '>')
                            {
                                attributeScanPos--;
                                isLast = true;
                                continue;
                            }
                            else if (c == ' ' || c == '\n' || c == '\r' || c == '\t')
                            {
                                // 閉じまたは値の最後
                                attrState = AttributeReadState.ExpectName; // AttrName
                                SetAttributeValue(curAttrName, curAttrValue, attributes, ref id, ref classNames);
                            }
                            else
                            {
                                curAttrValue.Append(c);
                            }
                        }
                    }
                }

                var classList = new ClassList();
                if (classNames != null)
                {
                    foreach (
                        var className in
                            classNames.Split(ClassNameSeparator, StringSplitOptions.RemoveEmptyEntries))
                    {
                        classList.Add(className);
                    }
                }
                var attributesRaw = content.Substring(tagNameEndPos, tagEndPos - tagNameEndPos);
                var nodeInfo = new NodeInfo(id, classList, attributes)
                {
                    TagName             = tagName,
                    TagNameUpper        = tagNameUpper,
                    Start               = pos,
                    TagNameEnd          = tagNameEndPos,
                    AttributeStart      = tagNameEndPos,
                    End                 = tagEndPos+1,
                    AttributesRaw       = attributesRaw,
                    IsXmlStyleSelfClose = isXmlStyleSelfClose,
                    IsSpecial           = tagName[0] == '!' || tagName[0] == '?'
                };
                pos = tagNameEndPos;

                // 親要素に子要素として追加
                node.AppendChild(nodeInfo);

                if (!isEndTagOrSelfClose)
                {
                    node = nodeInfo;
                }
            }
            return rootNode;
        }

        private static void SetAttributeValue(StringBuilder curAttrName, StringBuilder curAttrValue,
            AttributesDictionary attributes, ref String id, ref String classNames)
        {
            var curAttrNameStr = curAttrName.ToString();
            var curAttrValueStr = HtmlUnescape(curAttrValue.ToString());
            attributes[curAttrNameStr] = curAttrValueStr;

            if (curAttrNameStr == "id")
                id = curAttrValueStr;
            if (curAttrNameStr == "class")
                classNames = curAttrValueStr;

            curAttrName.Clear();
            curAttrValue.Clear();
        }

        public static void ToHtmlString(NodeInfo node, String originalContent, StringWriter writer)
        {
            var start = 0;
            ToHtmlString(node, originalContent, writer, ref start);
        }
        public static void ToHtmlString(NodeInfo node, String originalContent, StringWriter writer, ref Int32 start)
        {
            if (node.TagName != null)
            {
                // タグまで
                writer.Write(originalContent.Substring(start, node.Start - start));
                // タグ(<hoge>)
                writer.Write(originalContent.Substring(node.Start, node.End - node.Start));
            }
            start = node.End;

            for (var i = 0; i < node.ChildNodes.Count; i++)
            {
                ToHtmlString(node.ChildNodes[i], originalContent, writer, ref start);
            }

            if (node.Parent == null)
            {
                writer.Write(originalContent.Substring(start, originalContent.Length - start));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static String HtmlUnescape(String value)
        {
            return value.Replace("&quot;", "\"");
        }
    }
}
