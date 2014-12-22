using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Cartelet.Html;
using ExCSS;
using System.Text.RegularExpressions;

namespace Cartelet.StylesheetExpander
{
    /// <summary>
    /// スタイルシートをstyle属性に展開する機能を提供します。
    /// </summary>
    public class StylesheetExpander
    {
        private static IDictionary<String, StylesheetExpander> Expanders { get; set; }

        private HtmlFilter _htmlFilter;
        private DateTime _cssLastUpdatedAt;
        private String _cssPath;

        /// <summary>
        /// class属性を保持するためのコンテキストのストレージのキーです。
        /// コンテキストのストレージにtrueをセットすることでclass属性を削除せず出力されるようになります。
        /// </summary>
        public const string ContextKeyPreserveClassNames = "Cartelet.StylesheetExpander:PreserveClassNames";

        static StylesheetExpander()
        {
            Expanders = new ConcurrentDictionary<String, StylesheetExpander>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlFilter"></param>
        /// <param name="cssPath"></param>
        public StylesheetExpander(HtmlFilter htmlFilter, String cssPath)
        {
            _htmlFilter = htmlFilter;
            _cssPath = cssPath;
        }

        /// <summary>
        /// HtmlFilterにハンドラ等を設定します。
        /// </summary>
        /// <param name="htmlFilter"></param>
        /// <param name="cssPath"></param>
        public static void Register(HtmlFilter htmlFilter, String cssPath)
        {
            if (Expanders.ContainsKey(cssPath))
            {
                throw new ArgumentException("すでに登録されています。", "cssPath");
            }

            var expander = new StylesheetExpander(htmlFilter, cssPath);

            expander.SetupExpanderForViewEngine();
            expander.UpdateStyleSheet();

            Expanders.Add(cssPath, expander);
        }

        /// <summary>
        /// スタイルシートが変更されていたら更新します。
        /// </summary>
        public static void UpdateStyleSheetIfChanged()
        {
            if (!IsStylesheetsChanged())
                return;

            lock (Expanders)
            {
                // 最初にハンドラを削除する
                foreach (var expander in Expanders.Values)
                {
                    expander.RemoveHandler();
                }

                // それから登録しなおす
                foreach (var expander in Expanders.Values)
                {
                    expander.UpdateStyleSheet();
                }
            }
        }

        private void RemoveHandler()
        {
            _htmlFilter.RemoveHandlers("Cartelet.StylesheetExpander.ExecuteHandlers");
        }

        /// <summary>
        /// CarteletViewEngineのHtmlFilterにハンドラを設定したりします。
        /// </summary>
        private void SetupExpanderForViewEngine()
        {
            lock (_htmlFilter)
            {
                // Register Expander
                if (!_htmlFilter.AggregatedHandlers.ContainsKey("Cartelet.StylesheetExpander.ExecuteHandlers"))
                {
                    _htmlFilter.AggregatedHandlers.Add("Cartelet.StylesheetExpander.ExecuteHandlers", ExecuteHandlers);
                    _htmlFilter.AttributesFilter.Add((context, attrName, attrValue) => (attrName == "class" && !context.Items.Get<Boolean>(ContextKeyPreserveClassNames)) ? null : attrValue); // classを残す設定
                }
            }
        }

        /// <summary>
        /// マッチした要素たちのスタイルを展開するハンドラ
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="nodeInfo"></param>
        /// <param name="matchedHandlers"></param>
        /// <returns></returns>
        private static Boolean ExecuteHandlers(CarteletContext ctx, NodeInfo nodeInfo, IList<CompiledSelectorHandler> matchedHandlers)
        {
            foreach (var handler in matchedHandlers.OrderBy(x => x.Selector.Specificity))
            {
                handler.Handler(ctx, nodeInfo);
            }

            var styleDict = ctx.Items.Get<Dictionary<String, String>>("Cartelet.StylesheetExpander:StyleDictionary");
            if (styleDict != null)
            {
                var newStyle = new StringBuilder();

                var newPseudoBeforeStyle = new StringBuilder();
                var newPseudoAfterStyle = new StringBuilder();
                var newPseudoBeforeAttrs = new StringBuilder();
                var newPseudoAfterAttrs = new StringBuilder();
                var pseudoBeforeDisplay = "";
                var pseudoAfterDisplay = "";
                var pseudoBeforeContent = "";
                var pseudoAfterContent = "";

                foreach (var style in styleDict)
                {
                    // -cartelet-attribute-attrname: hoge; => attrname="hoge"
                    if (style.Key.StartsWith("-cartelet-attribute-"))
                    {
                        // とりあえず先頭がクォートだったら前後のは外す
                        nodeInfo.Attributes[style.Key.Substring(20)] = StripQuotes(style.Value);
                    }
                    // -cartelet-pseudo-before-propname: ...
                    else if (style.Key.StartsWith("-cartelet-pseudo-"))
                    {
                        if (style.Key.StartsWith("-cartelet-pseudo-before-"))
                        {
                            var key = style.Key.Substring(24);
                            if (key == "display")
                            {
                                pseudoBeforeDisplay = style.Value;
                                continue;
                            }
                            else if (key == "content")
                            {
                                pseudoBeforeContent = style.Value;
                                continue;
                            }
                            else if (key.StartsWith("-cartelet-attribute-"))
                            {
                                newPseudoBeforeAttrs
                                    .Append(' ')
                                    .Append(key.Substring(20))
                                    .Append("=\"")
                                    .Append(EscapeHtml(StripQuotes(style.Value)))
                                    .Append('"');
                                continue;
                            }

                            newPseudoBeforeStyle
                                    .Append(key)
                                    .Append(':')
                                    .Append(style.Value)
                                    .Append(';');
                        }
                        else if (style.Key.StartsWith("-cartelet-pseudo-after-"))
                        {
                            var key = style.Key.Substring(23);
                            if (key == "display")
                            {
                                pseudoAfterDisplay = style.Value;
                                continue;
                            }
                            else if (key == "content")
                            {
                                pseudoAfterContent = style.Value;
                                continue;
                            }
                            else if (key.StartsWith("-cartelet-attribute-"))
                            {
                                newPseudoAfterAttrs
                                    .Append(' ')
                                    .Append(key.Substring(20))
                                    .Append("=\"")
                                    .Append(EscapeHtml(StripQuotes(style.Value)))
                                    .Append('"');
                                continue;
                            }

                            newPseudoAfterStyle
                                .Append(key)
                                .Append(':')
                                .Append(style.Value)
                                .Append(';');
                        }
                    }
                    else
                    {
                        newStyle.Append(style.Key)
                                .Append(':')
                                .Append(style.Value)
                                .Append(';');
                    }
                }
                if (nodeInfo.Attributes.ContainsKey("style"))
                {
                    newStyle.Append(nodeInfo.Attributes["style"]); // 元のを後ろにつけることでstyle属性直接指定を残す
                }
                nodeInfo.Attributes["style"] = newStyle.ToString();
                styleDict.Clear();

                // ::before/after を差し込む
                if (!String.IsNullOrWhiteSpace(pseudoBeforeDisplay))
                {
                    nodeInfo.BeforeContent += BuildPseudoContent(newPseudoBeforeStyle, pseudoBeforeDisplay, pseudoBeforeContent, newPseudoBeforeAttrs);
                }
                if (!String.IsNullOrWhiteSpace(pseudoAfterDisplay))
                {
                    nodeInfo.AfterContent = BuildPseudoContent(newPseudoAfterStyle, pseudoAfterDisplay, pseudoAfterContent, newPseudoAfterAttrs) + nodeInfo.AfterContent;
                }
            }

            return true;
        }

        /// <summary>
        /// 擬似要素のHTMLを生成します
        /// </summary>
        /// <param name="pseudoStyle"></param>
        /// <param name="pseudoDisplay"></param>
        /// <param name="pseudoContent"></param>
        /// <returns></returns>
        private static String BuildPseudoContent(StringBuilder pseudoStyle, String pseudoDisplay, String pseudoContent, StringBuilder pseudoAttrs)
        {
            var style = (pseudoStyle.Length != 0 ? " style=\"" + EscapeHtml(pseudoStyle.ToString()) + "\"" : "");
            if (pseudoContent.StartsWith("url("))
            {
                // url(...) はimg要素を出力する(alt属性は自動生成なので意図的につけない)
                var html = new StringBuilder();
                html.Append("<img src=\"");
                html.Append(EscapeHtml(StripQuotes(pseudoContent.Substring(4).TrimEnd(')'))));
                html.Append("\"");
                html.Append(style);
                html.Append(pseudoAttrs);
                html.Append(" />");

                if (pseudoDisplay == "block")
                {
                    return "<div>" + html.ToString() + "</div>";
                }
                else
                {
                    return html.ToString();
                }
            }
            else
            {
                var tagName = (pseudoDisplay == "block" ? "div" : "span");
                return "<" + tagName + style + ">" + GetHtmlFromContentValue(pseudoContent) + "</" + tagName + ">";
            }
        }

        /// <summary>
        /// contentプロパティをよしなにしてHTMLを取り出します。
        /// </summary>
        /// <param name="contentPropValue"></param>
        /// <returns></returns>
        private static String GetHtmlFromContentValue(String contentPropValue)
        {
            if (contentPropValue.StartsWith("-cartelet-raw("))
            {
                // -cartelet-raw(...) はHTMLをそのまま返す
                return StripQuotes(contentPropValue.Substring(14).TrimEnd(')'));
            }
            else
            {
                // クォートはずしてHTMLエスケープして返す
                return EscapeHtml(StripQuotes(contentPropValue));
            }
        }

        /// <summary>
        /// シングルクォートまたはダブルクォートで囲まれている部分を外して取り出します。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static String StripQuotes(String value)
        {
            return value.StartsWith("'")
                    ? value.Trim('\'')
                    : value.StartsWith("\"")
                        ? value.Trim('"')
                        : value;
        }

        private static String EscapeHtml(String value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static Boolean IsStylesheetsChanged()
        {
            return Expanders.Values.Any(expander => expander.IsStylesheetChanged());
        }

        private Boolean IsStylesheetChanged()
        {
            if (File.Exists(_cssPath))
            {
                var updatedAt = File.GetLastWriteTime(_cssPath);
                if (updatedAt != _cssLastUpdatedAt)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// スタイルシートが変更されていたら更新します。
        /// </summary>
        private void UpdateStyleSheet()
        {
            try
            {
                if (File.Exists(_cssPath))
                {
                    UpdateStyleSheetInternal();
                    _cssLastUpdatedAt = File.GetLastWriteTime(_cssPath);
                }
                else
                {
                    _cssLastUpdatedAt = new DateTime();
                }
            }
            catch
            {
                _cssLastUpdatedAt = new DateTime();
            }

        }

        /// <summary>
        /// スタイルシートを読み込みます。
        /// </summary>
        private void UpdateStyleSheetInternal()
        {
            lock (_htmlFilter)
            {
                // Parse Stylesheet
                var stylesheet = new Parser().Parse(File.ReadAllText(_cssPath));

                // Register Style/Selectors
                foreach (var styleRule in stylesheet.Rulesets)
                {
                    // TODO: !important
                    var declarations = new Dictionary<String, String>(StringComparer.Ordinal);
                    foreach (var decl in styleRule.Declarations)
                    {
                        declarations[decl.Name] = decl.Term.ToString();
                    }

                    foreach (var selector in (styleRule.Selector is MultipleSelectorList ? styleRule.Selector as IEnumerable<SimpleSelector> : new [] { styleRule.Selector }))
                    {
                        // マッチした要素に対する処理のハンドラ。
                        var selectorString = selector.ToString();
                        var isPseudoBefore = false;
                        var isPseudoAfter = false;

                        // ::before/::afterは特別扱いする
                        var match = Regex.Match(selectorString, "::?(before|after)");
                        if (match.Success)
                        {
                            // 一旦擬似要素のセレクタを外す (Selectorコンパイラーが処理できないので)
                            selectorString = Regex.Replace(selectorString, "::?(before|after)", "");
                            isPseudoBefore = (match.Groups[1].Value == "before");
                            isPseudoAfter = (match.Groups[1].Value == "after");
                        }

                        _htmlFilter.AddHandler("Cartelet.StylesheetExpander.ExecuteHandlers", selectorString, (ctx, nodeInfo) =>
                        {
                            var styleDict = ctx.Items.Get<Dictionary<String, String>>("Cartelet.StylesheetExpander:StyleDictionary");
                            if (styleDict == null)
                            {
                                styleDict = new Dictionary<String, String>(StringComparer.Ordinal);
                                ctx.Items.Set("Cartelet.StylesheetExpander:StyleDictionary", styleDict);
                            }
                            foreach (var declaration in declarations)
                            {
                                // 擬似要素にマッチするように見せかけるために仮のプロパティにセットする
                                styleDict[
                                    (
                                        isPseudoBefore ? "-cartelet-pseudo-before-" :
                                        isPseudoAfter  ? "-cartelet-pseudo-after-" :
                                        ""
                                    ) +
                                    declaration.Key
                                ] = declaration.Value;
                            }
                            return true;
                        });
                    }
                }
            }
        }
    }
}
