﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cartelet.Html;
using Cartelet.Selector;

namespace Cartelet.Html
{
    public class HtmlFilter
    {
        /// <summary>
        /// マッチしたセレクタの一覧を受け取って処理をするハンドラのリストです。
        /// </summary>
        public IDictionary<String, Func<CarteletContext, NodeInfo, IList<CompiledSelectorHandler>, Boolean>> AggregatedHandlers { get; private set; }
        /// <summary>
        /// 処理のハンドラのリストです。
        /// </summary>
        private IList<CompiledSelectorHandler> Handlers { get; set; }
        /// <summary>
        /// タグ名で絞り込まれた処理のハンドラのリストです。
        /// </summary>
        private IDictionary<String, IList<CompiledSelectorHandler>> HandlersByTagName { get; set; }
        /// <summary>
        /// IDで絞り込まれた処理のハンドラのリストです。
        /// </summary>
        private IDictionary<String, IList<CompiledSelectorHandler>> HandlersById { get; set; }
        /// <summary>
        /// クラス名で処理のハンドラのリストです。
        /// </summary>
        private IDictionary<String, IList<CompiledSelectorHandler>> HandlersByClassName { get; set; }
        /// <summary>
        /// エラーや問題がある場合に知らせるハンドラのリストです。
        /// </summary>
        public IList<Func<CarteletContext, NodeInfo, Boolean>> TraceHandlers { get; set; }

        public HtmlFilter()
        {
            AggregatedHandlers = new Dictionary<String, Func<CarteletContext, NodeInfo, IList<CompiledSelectorHandler>, Boolean>>();
            Handlers = new List<CompiledSelectorHandler>();
            HandlersByClassName = new Dictionary<String, IList<CompiledSelectorHandler>>(StringComparer.Ordinal);
            HandlersByTagName = new Dictionary<String, IList<CompiledSelectorHandler>>(StringComparer.Ordinal);
            HandlersById = new Dictionary<String, IList<CompiledSelectorHandler>>(StringComparer.Ordinal);
            TraceHandlers = new List<Func<CarteletContext, NodeInfo, Boolean>>();
        }

        /// <summary>
        /// 指定した種別のハンドラーを削除します。
        /// </summary>
        /// <param name="handlerType"></param>
        public void RemoveHandlers(String handlerType)
        {
            foreach (var handler in Handlers.Where(x => x.Type == handlerType).ToList())
            {
                Handlers.Remove(handler);
            }

            foreach (var handlers in HandlersByClassName.Values)
            {
                foreach (var handler in handlers.Where(x => x.Type == handlerType).ToList())
                {
                    handlers.Remove(handler);
                }
            }
            foreach (var handlers in HandlersById.Values)
            {
                foreach (var handler in handlers.Where(x => x.Type == handlerType).ToList())
                {
                    handlers.Remove(handler);
                }
            }
            foreach (var handlers in HandlersByTagName.Values)
            {
                foreach (var handler in handlers.Where(x => x.Type == handlerType).ToList())
                {
                    handlers.Remove(handler);
                }
            }
        }

        /// <summary>
        /// セレクターとハンドラのタイプを指定してハンドラを設定します。
        /// ハンドラはAggregatedHandlersに登録されたマッチされたものをまとめて処理する実行ハンドラまたはデフォルトの実行ハンドラで処理されます。
        /// </summary>
        /// <param name="handlerType"></param>
        /// <param name="selector"></param>
        /// <param name="handler"></param>
        public void AddHandler(String handlerType, String selector, Func<CarteletContext, NodeInfo, Boolean> handler)
        {
            var parsed = new SelectorParser(selector).Parse();
            var compiled = new CompiledSelectorHandler
            {
                Matcher = CompiledSelector.Compile(parsed),
                Selector = parsed,
                SelectorString = selector,
                Handler = handler,
                Type = handlerType,
            };

            var lastClassSelector = parsed.Children.Last().Children.OfType<ClassSelector>().FirstOrDefault();
            if (lastClassSelector != null)
            {
                if (!HandlersByClassName.ContainsKey(lastClassSelector.ClassName))
                {
                    HandlersByClassName[lastClassSelector.ClassName] = new List<CompiledSelectorHandler>();
                }
                HandlersByClassName[lastClassSelector.ClassName].Add(compiled);
                return;
            }

            var lastIdSelector = parsed.Children.Last().Children.OfType<IdSelector>().FirstOrDefault();
            if (lastIdSelector != null)
            {
                if (!HandlersById.ContainsKey(lastIdSelector.Id))
                {
                    HandlersById[lastIdSelector.Id] = new List<CompiledSelectorHandler>();
                }
                HandlersById[lastIdSelector.Id].Add(compiled);
                return;
            }

            var lastTypeSelector = parsed.Children.Last().Children.OfType<TypeSelector>().FirstOrDefault();
            if (lastTypeSelector != null)
            {
                var elementNameUpper = lastTypeSelector.ElementName.ToUpper();
                if (!HandlersByTagName.ContainsKey(elementNameUpper))
                {
                    HandlersByTagName[elementNameUpper] = new List<CompiledSelectorHandler>();
                }
                HandlersByTagName[elementNameUpper].Add(compiled);
                return;
            }

            Handlers.Add(compiled);
        }

        /// <summary>
        /// セレクターを指定してハンドラを設定します。
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="handler"></param>
        public void AddHandler(String selector, Func<CarteletContext, NodeInfo, Boolean> handler)
        {
            AddHandler(null, selector, handler);
        }

        /// <summary>
        /// HTMLをフィルターします。スレッドセーフ。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public void Execute(CarteletContext context, NodeInfo node)
        {
            var start = 0;
            context.ElapsedHandlerTicks = 0;
            context.ElapsedSelectorMatchTicks = 0;
            ToHtmlString(context, node, ref start);
        }

        private void ToHtmlString(CarteletContext context, NodeInfo node, ref Int32 start)
        {
            var originalContent = context.Content;
            var writer = context.Writer;

            using (context.BeginStorageSession())
            {
                if (node.TagName != null)
                {
                    // タグまで
                    writer.Write(originalContent.Substring(start, node.Start - start));

                    // 親から先祖までたどってclassをかき集める
                    var cascadeClassNames = new HashSet<String>(StringComparer.Ordinal);
                    var cascadeIds = new HashSet<String>(StringComparer.Ordinal);
                    var parent = node.Parent;
                    while (parent != null)
                    {
                        foreach (var className in parent.ClassList)
                        {
                            cascadeClassNames.Add(className);
                        }
                        if (!String.IsNullOrEmpty(node.Id))
                            cascadeIds.Add(node.Id);

                        parent = parent.Parent;
                    }

                    // フィルタ
                    var stopwatchMatch = Stopwatch.StartNew();
                    var matchedHandlers = new List<CompiledSelectorHandler>();
                    if (node.Id != null && HandlersById.ContainsKey(node.Id))
                    {
                        foreach (var handler in HandlersById[node.Id])
                        {
                            if (handler.Match(context, node, cascadeClassNames, cascadeIds))
                                matchedHandlers.Add(handler);
                        }
                    }

                    if (HandlersByTagName.ContainsKey(node.TagNameUpper))
                    {
                        foreach (var handler in HandlersByTagName[node.TagNameUpper])
                        {
                            if (handler.Match(context, node, cascadeClassNames, cascadeIds))
                                matchedHandlers.Add(handler);
                        }
                    }

                    foreach (var className in node.ClassList)
                    {
                        if (HandlersByClassName.ContainsKey(className))
                        {
                            foreach (var handler in HandlersByClassName[className])
                            {
                                if (handler.Match(context, node, cascadeClassNames, cascadeIds))
                                    matchedHandlers.Add(handler);
                            }
                        }
                    }

                    foreach (var handler in Handlers)
                    {
                        if (handler.Match(context, node, cascadeClassNames, cascadeIds))
                            matchedHandlers.Add(handler);
                    }

                    stopwatchMatch.Stop();
                    context.ElapsedSelectorMatchTicks += stopwatchMatch.ElapsedTicks;

                    // 最後にマッチしたハンドラを渡してまとめて処理するやつに投げる
                    var stopwatchHandler = Stopwatch.StartNew();
                    if (matchedHandlers.Count > 0)
                    {
                        foreach (var matchedHandlerGroup in matchedHandlers.GroupBy(x => x.Type))
                        {
                            var handler = (matchedHandlerGroup.Key != null && AggregatedHandlers.ContainsKey(matchedHandlerGroup.Key))
                                            ? AggregatedHandlers[matchedHandlerGroup.Key]
                                            : (CarteletContext ctx, NodeInfo nodeInfo, IList<CompiledSelectorHandler> handlers) =>
                                            {
                                                foreach (var h in handlers)
                                                {
                                                    h.Handler(ctx, nodeInfo);
                                                }
                                                return true;
                                            };
                            handler(context, node, matchedHandlerGroup.Select(x => x).ToList());
                        }
                    }
                    stopwatchHandler.Stop();
                    context.ElapsedHandlerTicks += stopwatchHandler.ElapsedTicks;

                    // タグ(<hoge>)
                    if (node.IsDirty)
                    {
                        // 属性が変わってる
                        writer.Write("<" + node.TagName);
                        if (node.Attributes.Count > 0)
                        {
                            foreach (var attr in node.Attributes)
                            {
                                writer.Write(" " + attr.Key + "=\"" + EscapeHtml(attr.Value) + "\"");
                            }
                        }

                        if (node.IsXmlStyleSelfClose)
                        {
                            writer.Write(" />");

                        }
                        else
                        {
                            writer.Write(">");
                        }
                    }
                    else
                    {
                        writer.Write(originalContent.Substring(node.Start, node.End - node.Start));
                    }
                }
                start = node.End;

                // 子ノード
                for (var i = 0; i < node.ChildNodes.Count; i++)
                {
                    ToHtmlString(context, node.ChildNodes[i], ref start);
                }

                // 残りの部分
                if (node.EndOfElement != -1)
                {
                    writer.Write(originalContent.Substring(start, node.EndOfElement - start));
                    start = node.EndOfElement;
                }
            }

            // 本当に余った部分
            if (node.Parent == null)
            {
                writer.Write(originalContent.Substring(start, originalContent.Length - start));
            }
        }

        private static String EscapeHtml(String value)
        {
            //return value.Replace("&", "&amp;").Replace("<", "&gt;").Replace(">", "&lt;").Replace("\"", "&quot;");
            return value.Replace("\"", "&quot;");
        }
    }
}
