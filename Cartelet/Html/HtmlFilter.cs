using System;
using System.Collections.Concurrent;
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
        /// <summary>
        /// セレクターのキャッシュを使うかどうかを取得・設定します。
        /// </summary>
        public Boolean UseSelectorCache { get; set; }

        public HtmlFilter()
        {
            UseSelectorCache = true;
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
            _cacheHandlersByClassName.Clear();
            _cacheHandlersByTagName.Clear();
            _cacheHandlers.Clear();
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
#if DEBUG
                                   var stopwatch = Stopwatch.StartNew();
#endif
                                   var result = handler(ctx, nodeInfo);
#if DEBUG
                                   stopwatch.Stop();
                                   ctx.TraceCountHandlers[compiled].HandlerElapsedTotalTicks += stopwatch.Elapsed.Ticks;
#endif
                                   return result;
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

            _cacheHandlersByClassName.Clear();
            _cacheHandlersByTagName.Clear();
            _cacheHandlers.Clear();
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

#if DEBUG
            context.TraceCountHandlers.Clear();
            foreach (var handler in HandlersByClassName.Values
                .Concat(HandlersById.Values)
                .Concat(HandlersByTagName.Values)
                .SelectMany(x => x)
                .Concat(Handlers))
            {
                context.TraceCountHandlers[handler] = new TraceResult();
            }
#endif

            ToHtmlString(context, node, ref start);

#if DEBUG
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

                        writer.Write(node.IsXmlStyleSelfClose ? " />" : ">");
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

        /// <summary>
        /// マッチしたハンドラーを実行します。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="matchedHandlers"></param>
        private void ProcessHandlers(CarteletContext context, NodeInfo node, List<CompiledSelectorHandler> matchedHandlers)
        {
#if DEBUG
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
                    handler(context, node, matchedHandlerGroup.Select(x => x).ToList());
                }
            }
#if DEBUG
            stopwatchProcessHandlers.Stop();
            context.ElapsedHandlerTicks += stopwatchProcessHandlers.Elapsed.Ticks;
#endif
        }

        private ConcurrentDictionary<String, List<CompiledSelectorHandler>> _cacheHandlersByClassName = new ConcurrentDictionary<string, List<CompiledSelectorHandler>>(StringComparer.Ordinal);
        private ConcurrentDictionary<String, List<CompiledSelectorHandler>> _cacheHandlersByTagName = new ConcurrentDictionary<string, List<CompiledSelectorHandler>>(StringComparer.Ordinal);
        private ConcurrentDictionary<String, List<CompiledSelectorHandler>> _cacheHandlers = new ConcurrentDictionary<string, List<CompiledSelectorHandler>>(StringComparer.Ordinal);

        /// <summary>
        /// 要素がマッチするかどうかテストしてマッチしたハンドラーを収集します。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<CompiledSelectorHandler> ExecuteMatches(CarteletContext context, NodeInfo node)
        {
#if DEBUG
            var stopwatchMatch = Stopwatch.StartNew();
#endif
            // マッチテストするハンドラたちを用意する
            IList<CompiledSelectorHandler> handlers, handlersByClassName, handlersByTagName;
            if (UseSelectorCache)
            {
                var cascadeClassNamesString = String.Join(" ", node.CascadeClassNames);
                handlersByClassName =
                    _cacheHandlersByClassName.GetOrAdd(String.Join(" ", node.ClassList) + ":" + cascadeClassNamesString,
                        (key) =>
                                node.ClassList.SelectMany(x => HandlersByClassName.ContainsKey(x) ? HandlersByClassName[x] : Enumerable.Empty<CompiledSelectorHandler>()).Where(
                                    x => x.RequiredClassNames.IsSubsetOf(node.CascadeClassNames)).ToList()
                        );
                handlersByTagName =
                    _cacheHandlersByTagName.GetOrAdd(node.TagNameUpper + ":" + cascadeClassNamesString,
                        (key) =>
                            HandlersByTagName.ContainsKey(node.TagNameUpper)
                                ? HandlersByTagName[node.TagNameUpper].Where(x => x.RequiredClassNames.IsSubsetOf(node.CascadeClassNames)).ToList()
                                : Enumerable.Empty<CompiledSelectorHandler>().ToList()
                        );

                handlers =
                    _cacheHandlers.GetOrAdd(node.TagNameUpper + ":" + cascadeClassNamesString,
                        (key) => Handlers.Where(x => x.RequiredClassNames.IsSubsetOf(node.CascadeClassNames)).ToList());
            }
            else
            {
                handlersByTagName = HandlersByTagName.ContainsKey(node.TagNameUpper) ? HandlersByTagName[node.TagNameUpper] : Enumerable.Empty<CompiledSelectorHandler>().ToList();
                handlersByClassName = node.ClassList.SelectMany(x => HandlersByClassName.ContainsKey(x) ? HandlersByClassName[x] : Enumerable.Empty<CompiledSelectorHandler>()).ToList();
                handlers = Handlers;
            }

            var matchedHandlers = new List<CompiledSelectorHandler>();
            if (node.Id != null && HandlersById.ContainsKey(node.Id))
            {
                foreach (var handler in HandlersById[node.Id])
                {
                    ExecuteMatch(context, node, handler, matchedHandlers);
                }
            }

            foreach (var handler in handlersByTagName)
            {
                ExecuteMatch(context, node, handler, matchedHandlers);
            }

            foreach (var handler in handlersByClassName)
            {
                ExecuteMatch(context, node, handler, matchedHandlers);
            }

            foreach (var handler in handlers)
            {
                ExecuteMatch(context, node, handler, matchedHandlers);
            }

#if DEBUG
            stopwatchMatch.Stop();
            context.ElapsedSelectorMatchTicks += stopwatchMatch.Elapsed.Ticks;
#endif
            return matchedHandlers;
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
#if DEBUG
            var traceResult = context.TraceCountHandlers[handler];
            traceResult.MatcherCallCount++;
            var stopwatch = Stopwatch.StartNew();
#endif
            if (handler.Match(context, node))
            {
#if DEBUG
                stopwatch.Stop();
                traceResult.MatchedCount++;
                traceResult.MatchTotalElapsedTicks += stopwatch.Elapsed.Ticks;
#endif
                matchedHandlers.Add(handler);
            }
        }

        private static String EscapeHtml(String value)
        {
            //return value.Replace("&", "&amp;").Replace("<", "&gt;").Replace(">", "&lt;").Replace("\"", "&quot;");
            return value.Replace("\"", "&quot;");
        }
    }
}
