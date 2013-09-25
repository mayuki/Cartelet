using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Cartelet.Html;
using Cartelet.Mvc;
using ExCSS;

namespace Cartelet.StylesheetExpander
{
    /// <summary>
    /// スタイルシートをstyle属性に展開する機能を提供します。
    /// </summary>
    public class StylesheetExpander
    {
        private static IDictionary<String, StylesheetExpander> Expanders { get; set; }

        private CarteletViewEngine _carteletViewEngine;
        private DateTime _cssLastUpdatedAt;
        private String _cssPath;

        static StylesheetExpander()
        {
            Expanders = new ConcurrentDictionary<String, StylesheetExpander>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="carteletViewEngine"></param>
        /// <param name="cssPath"></param>
        public StylesheetExpander(CarteletViewEngine carteletViewEngine, String cssPath)
        {
            _carteletViewEngine = carteletViewEngine;
            _cssPath = cssPath;
        }

        /// <summary>
        /// CarteletViewEngineにハンドラ等を設定します。
        /// </summary>
        /// <param name="carteletViewEngine"></param>
        /// <param name="cssPath"></param>
        public static void RegisterViewEngine(CarteletViewEngine carteletViewEngine, String cssPath)
        {
            if (Expanders.ContainsKey(cssPath))
            {
                throw new ArgumentException("すでに登録されています。", "cssPath");
            }

            var expander = new StylesheetExpander(carteletViewEngine, cssPath);

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
                    expander.UpdateStyleSheetIfChangedInternal();
                }
            }
        }

        private void RemoveHandler()
        {
            _carteletViewEngine.HtmlFilter.RemoveHandlers("Cartelet.StylesheetExpander.ExecuteHandlers");
        }

        /// <summary>
        /// CarteletViewEngineのHtmlFilterにハンドラを設定したりします。
        /// </summary>
        private void SetupExpanderForViewEngine()
        {
            lock (_carteletViewEngine)
            {
                // Setup HTML Filter
                var htmlFilter = _carteletViewEngine.HtmlFilter;
                // Register Expander
                if (!htmlFilter.AggregatedHandlers.ContainsKey("Cartelet.StylesheetExpander.ExecuteHandlers"))
                {
                    htmlFilter.AggregatedHandlers.Add("Cartelet.StylesheetExpander.ExecuteHandlers", ExecuteHandlers);
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
                nodeInfo.Attributes["style"] = String.Join(";", styleDict.Select(x => x.Key + ":" + x.Value));
                styleDict.Clear();
            }

            // TODO:classは消すところは考えないとマッチするのに影響が出る
            //nodeInfo.ClassList.Clear();

            return true;
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
        private void UpdateStyleSheetIfChangedInternal()
        {
            if (File.Exists(_cssPath))
            {
                var updatedAt = File.GetLastWriteTime(_cssPath);
                if (updatedAt != _cssLastUpdatedAt)
                {
                    UpdateStyleSheet();
                }
            }
            else
            {
                _cssLastUpdatedAt = new DateTime();
            }
        }

        /// <summary>
        /// スタイルシートを読み込みます。
        /// </summary>
        private void UpdateStyleSheet()
        {
            lock (_carteletViewEngine)
            {
                if (!File.Exists(_cssPath))
                    return;

                _cssLastUpdatedAt = File.GetLastWriteTime(_cssPath);

                // Parse Stylesheet
                var stylesheet = new StylesheetParser().Parse(File.ReadAllText(_cssPath));

                // Setup HTML Filter
                var htmlFilter = _carteletViewEngine.HtmlFilter;

                // Register Style/Selectors
                foreach (var styleRule in stylesheet.RuleSets)
                {
                    foreach (var selector in styleRule.Selectors)
                    {
                        // マッチした要素に対する処理のハンドラ。
                        htmlFilter.AddHandler("Cartelet.StylesheetExpander.ExecuteHandlers", selector.ToString(), (ctx, nodeInfo) =>
                        {
                            var styleDict = ctx.Items.Get<Dictionary<String, String>>("Cartelet.StylesheetExpander:StyleDictionary");
                            if (styleDict == null)
                            {
                                styleDict = new Dictionary<String, String>(StringComparer.Ordinal);
                                ctx.Items.Set("Cartelet.StylesheetExpander:StyleDictionary", styleDict);
                            }
                            foreach (var declaration in styleRule.Declarations)
                            {
                                styleDict[declaration.Name] = declaration.Expression.ToString();
                            }
                            return true;
                        });
                    }
                }
            }
        }
    }
}
