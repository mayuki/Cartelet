﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
        /// タグ名、ID、クラスで絞り込まれた処理のハンドラのリストです。
        /// </summary>
        private IList<CompiledSelectorHandler> HandlersForSimplifiedSelectors { get; set; }
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
        /// <summary>
        /// 書き換えが発生している要素の属性を出力する際のフィルターのリストです。
        /// (attrName, attrValue) => attrValue; で null を返すと属性を削除し、String.Emptyを返すと空文字列が出力されます。
        /// 書き換えが発生しなかった要素には適用されません。
        /// </summary>
        public IList<Func<CarteletContext, String, String, String>> AttributesFilter { get; set; }
        /// <summary>
        /// Executeを実行する前に処理するハンドラーです。
        /// </summary>
        public IList<Action<CarteletContext, NodeInfo>> PreExecuteHandlers { get; set; }
        /// <summary>
        /// Executeを実行するあとに処理するハンドラーです。
        /// </summary>
        public IList<Action<CarteletContext, NodeInfo>> PostExecuteHandlers { get; set; }

        /// <summary>
        /// セレクターのキャッシュを使うかどうかを取得・設定します。
        /// </summary>
        public Boolean UseSelectorCache { get; set; }

        private ConcurrentDictionary<String, CacheHandlerSet> _cacheHandlerSet = new ConcurrentDictionary<string, CacheHandlerSet>(StringComparer.Ordinal);

        public HtmlFilter()
        {
            UseSelectorCache = true;
            AggregatedHandlers = new Dictionary<String, Func<CarteletContext, NodeInfo, IList<CompiledSelectorHandler>, Boolean>>();
            Handlers = new List<CompiledSelectorHandler>();
            HandlersForSimplifiedSelectors = new List<CompiledSelectorHandler>();
            HandlersByClassName = new Dictionary<String, IList<CompiledSelectorHandler>>(StringComparer.Ordinal);
            HandlersByTagName = new Dictionary<String, IList<CompiledSelectorHandler>>(StringComparer.Ordinal);
            HandlersById = new Dictionary<String, IList<CompiledSelectorHandler>>(StringComparer.Ordinal);
            TraceHandlers = new List<Func<CarteletContext, NodeInfo, Boolean>>();
            AttributesFilter = new List<Func<CarteletContext, String, String, String>>();
            PreExecuteHandlers = new List<Action<CarteletContext, NodeInfo>>();
            PostExecuteHandlers = new List<Action<CarteletContext, NodeInfo>>();
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
            foreach (var handler in HandlersForSimplifiedSelectors.Where(x => x.Type == handlerType).ToList())
            {
                HandlersForSimplifiedSelectors.Remove(handler);
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
            _cacheHandlerSet.Clear();
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
                Type = handlerType,
            };
            compiled.Handler = (ctx, nodeInfo) =>
            {
#if DEBUG && MEASURE_TIME
                var stopwatch = Stopwatch.StartNew();
#endif
                var result = handler(ctx, nodeInfo);
#if DEBUG && MEASURE_TIME
                stopwatch.Stop();
                ctx.TraceCountHandlers[compiled].HandlerElapsedTotalTicks += stopwatch.Elapsed.Ticks;
#endif
                return result;
            };


            _cacheHandlerSet.Clear();

            if (compiled.IsSelectorSimply)
            {
                HandlersForSimplifiedSelectors.Add(compiled);
                return;
            }

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
        /// HTMLをフィルターします。このメソッドはコンテキスト単位でスレッドセーフです。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public void Execute(CarteletContext context, NodeInfo node)
        {
            var start = 0;
            context.ElapsedHandlerTicks = 0;
            context.ElapsedSelectorMatchTicks = 0;

#if DEBUG && MEASURE_TIME
            context.TraceCountHandlers.Clear();
            foreach (var handler in HandlersByClassName.Values
                .Concat(HandlersById.Values)
                .Concat(HandlersByTagName.Values)
                .SelectMany(x => x)
                .Concat(HandlersForSimplifiedSelectors)
                .Concat(Handlers)
            )
            {
                context.TraceCountHandlers[handler] = new TraceResult();
            }
#endif

            // PreExecute
            foreach (var handler in PreExecuteHandlers)
            {
                handler(context, node);
            }

            // Execute
            ToHtmlString(context, node, ref start);

            // PostExecute
            foreach (var handler in PostExecuteHandlers)
            {
                handler(context, node);
            }

#if DEBUG && MEASURE_TIME
            var slow = context.TraceCountHandlers.OrderByDescending(x => x.Value.TotalTicks).ToList();
            Debug.WriteLine("-----");
            foreach (var slowQuery in slow.Take(5))
            {
                Debug.WriteLine(slowQuery.Key.SelectorString + ": " + slowQuery.Value.ToString());
            }
            Debug.WriteLine("-----");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="start"></param>
        private void ToHtmlString(CarteletContext context, NodeInfo node, ref Int32 start)
        {
            var originalContent = context.Content;
            var writer = context.Writer;

            using (context.BeginStorageSession())
            {
                if (node.TagName != null)
                {
                    // 要素の開始タグまで
                    writer.Write(originalContent.Substring(start, node.Start - start));

                    // 要素にフィルターを適用するハンドラを探してくる
                    var matchedHandlers = ExecuteMatches(context, node);

                    // 最後にマッチしたハンドラを渡してまとめてフィルター処理する
                    ProcessHandlers(context, node, matchedHandlers);

                    // 開始タグ(<hoge>)
                    if (node.IsDirty && !node.IsSpecial)
                    {
                        // 属性が変わってる
                        var sb = new StringBuilder();
                        sb.Append('<');
                        sb.Append(node.TagName);

                        if (node.Attributes.Any())
                        {
                            foreach (var attr in node.Attributes)
                            {
                                // 属性値フィルター
                                var value = AttributesFilter.Aggregate(attr.Value, (current, filter) => filter(context, attr.Key, current));

                                if (!String.IsNullOrWhiteSpace(value))
                                {
                                    sb.Append(' ');
                                    sb.Append(attr.Key);
                                    sb.Append("=\"");
                                    sb.Append(EscapeHtml(value));
                                    sb.Append('"');
                                }
                            }
                        }

                        sb.Append(node.IsXmlStyleSelfClose ? " />" : ">");

                        // 開始タグのあとに差し込まれるコンテンツ
                        if (!node.IsXmlStyleSelfClose && !String.IsNullOrEmpty(node.BeforeContent))
                        {
                            sb.Append(node.BeforeContent);
                        }

                        writer.Write(sb.ToString());
                    }
                    else
                    {
                        // そのまま出力する
                        writer.Write(originalContent.Substring(node.Start, node.End - node.Start));
                    }
                }
                start = node.End;

                // 子ノード
                foreach (var child in node.ChildNodes)
                {
                    ToHtmlString(context, child, ref start);
                }

                // 終了タグの前に差し込まれるコンテンツがある
                if (node.IsDirty && !node.IsXmlStyleSelfClose && !String.IsNullOrEmpty(node.AfterContent))
                {
                    // 終了タグ直前まで書く
                    writer.Write(originalContent.Substring(start, node.EndTagStart - start));
                    // 終了タグの前のやつ
                    writer.Write(node.AfterContent);
                    // 残りの部分
                    writer.Write(originalContent.Substring(node.EndTagStart, node.EndOfElement - node.EndTagStart));
                    start = node.EndOfElement;
                }
                else
                {
                    // 残りの部分
                    if (node.EndOfElement != -1)
                    {
                        writer.Write(originalContent.Substring(start, node.EndOfElement - start));
                        start = node.EndOfElement;
                    }
                }
            }

            // 本当に余った部分
            if (node.Parent == null)
            {
                writer.Write(originalContent.Substring(start, originalContent.Length - start));
            }
        }

        /// <summary>
        /// マッチしたハンドラーを実行します。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="matchedHandlers"></param>
        private void ProcessHandlers(CarteletContext context, NodeInfo node, List<CompiledSelectorHandler> matchedHandlers)
        {
#if DEBUG && MEASURE_TIME
            var stopwatchProcessHandlers = Stopwatch.StartNew();
#endif
            if (matchedHandlers.Count > 0)
            {
                foreach (var matchedHandlerGroup in matchedHandlers.GroupBy(x => x.Type))
                {
                    var handler = (matchedHandlerGroup.Key != null && AggregatedHandlers.ContainsKey(matchedHandlerGroup.Key))
                        ? AggregatedHandlers[matchedHandlerGroup.Key]
                        : (ctx, nodeInfo, handlers) =>
                          {
                              foreach (var h in handlers)
                              {
                                  h.Handler(ctx, nodeInfo);
                              }
                              return true;
                          };
                    handler(context, node, matchedHandlerGroup.ToList());
                }
            }
#if DEBUG && MEASURE_TIME
            stopwatchProcessHandlers.Stop();
            context.ElapsedHandlerTicks += stopwatchProcessHandlers.Elapsed.Ticks;
#endif
        }

        /// <summary>
        /// 要素がマッチするかどうかテストしてマッチしたハンドラーを収集します。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<CompiledSelectorHandler> ExecuteMatches(CarteletContext context, NodeInfo node)
        {
#if DEBUG && MEASURE_TIME
            var stopwatchMatch = Stopwatch.StartNew();
#endif
            var nodePath = node.Path;

            // マッチテストするハンドラたちを用意する
            CacheHandlerSet handlerSet;
            if (UseSelectorCache)
            {
                handlerSet = _cacheHandlerSet.GetOrAdd(nodePath,
                    key =>
                    {
                        var cacheHandlerSet = new CacheHandlerSet();
                        // 簡略化されたセレクター(ID, Type, Class, Combinator(' ' or '>'))で構成されるものはここでマッチ確定できる
                        cacheHandlerSet.HandlersForSimplifiedSelectors
                            = HandlersForSimplifiedSelectors.Where(x => x.Match(context, node)).ToList();
                        cacheHandlerSet.HandlersByClassName
                            = node.ClassList.SelectMany(x => HandlersByClassName.ContainsKey(x) ? HandlersByClassName[x] : Enumerable.Empty<CompiledSelectorHandler>())
                                            .Where(x => x.RequiredClassNames.IsSubsetOf(node.CascadeClassNames) && x.RequiredIds.IsSubsetOf(node.CascadeIds)).ToList();
                        cacheHandlerSet.HandlersByTagName
                            = HandlersByTagName.ContainsKey(node.TagNameUpper)
                                ? HandlersByTagName[node.TagNameUpper].Where(x => x.RequiredClassNames.IsSubsetOf(node.CascadeClassNames) && x.RequiredIds.IsSubsetOf(node.CascadeIds)).ToList()
                                : Enumerable.Empty<CompiledSelectorHandler>().ToList();
                        cacheHandlerSet.Handlers
                            = Handlers.Where(x => x.RequiredClassNames.IsSubsetOf(node.CascadeClassNames) && x.RequiredIds.IsSubsetOf(node.CascadeIds)).ToList();
                        return cacheHandlerSet;
                    });
            }
            else
            {
                handlerSet = new CacheHandlerSet();
                handlerSet.HandlersForSimplifiedSelectors = HandlersForSimplifiedSelectors;
                handlerSet.HandlersByTagName = HandlersByTagName.ContainsKey(node.TagNameUpper) ? HandlersByTagName[node.TagNameUpper] : Enumerable.Empty<CompiledSelectorHandler>().ToList();
                handlerSet.HandlersByClassName = node.ClassList.SelectMany(x => HandlersByClassName.ContainsKey(x) ? HandlersByClassName[x] : Enumerable.Empty<CompiledSelectorHandler>()).ToList();
                handlerSet.Handlers = Handlers;
            }

            var matchedHandlers = new List<CompiledSelectorHandler>();
            if (node.Id != null && HandlersById.ContainsKey(node.Id))
            {
                foreach (var handler in HandlersById[node.Id])
                {
                    ExecuteMatch(context, node, handler, matchedHandlers);
                }
            }

            foreach (var handler in handlerSet.HandlersByClassName.Concat(handlerSet.HandlersByTagName).Concat(handlerSet.Handlers))
            {
                ExecuteMatch(context, node, handler, matchedHandlers);
            }

#if DEBUG && MEASURE_TIME
            stopwatchMatch.Stop();
            context.ElapsedSelectorMatchTicks += stopwatchMatch.Elapsed.Ticks;
#endif
            return matchedHandlers.Concat(handlerSet.HandlersForSimplifiedSelectors).ToList();
        }

        /// <summary>
        /// 要素がハンドラーにマッチするかテストします。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="handler"></param>
        /// <param name="matchedHandlers"></param>
        private void ExecuteMatch(CarteletContext context, NodeInfo node, CompiledSelectorHandler handler, List<CompiledSelectorHandler> matchedHandlers)
        {
#if DEBUG && MEASURE_TIME
            var traceResult = context.TraceCountHandlers[handler];
            traceResult.MatcherCallCount++;
            var stopwatch = Stopwatch.StartNew();
#endif
            if (handler.Match(context, node))
            {
#if DEBUG && MEASURE_TIME
                stopwatch.Stop();
                traceResult.MatchedCount++;
                traceResult.MatchTotalElapsedTicks += stopwatch.Elapsed.Ticks;
#endif
                matchedHandlers.Add(handler);
            }
        }

        private static String EscapeHtml(String value)
        {
            //return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
            return value.Replace("\"", "&quot;");
        }
    }
}
