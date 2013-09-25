using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Cartelet.Html;

namespace Cartelet.Mvc
{
    public class CarteletView : IView
    {
        public IView BaseView { get; private set; }

        private HtmlFilter _htmlFilter;
        private Func<String, TextWriter, CarteletContext> _contextFactory;

        public CarteletView(IView baseView, CarteletViewEngine viewEngine)
        {
            BaseView = baseView;

            _htmlFilter = viewEngine.HtmlFilter;
            _contextFactory = viewEngine.CarteletContextFactory;
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var stopwatch = Stopwatch.StartNew();

            // Render view
            var writerBuffer = new StringWriter();
            BaseView.Render(viewContext, writerBuffer);
            var renderMs = stopwatch.ElapsedMilliseconds;
            var content = writerBuffer.ToString();

            var ctx = _contextFactory(content, writer);
            if (ctx != null)
            {
                // Parse HTML
                var rootNode = HtmlParser.Parse(content);
                var parseMs = stopwatch.ElapsedMilliseconds;

                // Filter/Rewrite HTML
                _htmlFilter.Execute(ctx, rootNode);
                var filterMs = stopwatch.ElapsedMilliseconds;
                Trace.WriteLine(String.Format("CarteletView: Render:{0}ms(+0), Parse:{1}ms(+{4}ms), Filter:{2}ms(+{5}ms)/Match:{6}ms; Length:{3}", renderMs, parseMs, filterMs, content.Length, parseMs - renderMs, filterMs - parseMs, ctx.ElapsedSelectorMatchTime));
            }
            else
            {
                writer.Write(content);
            }
        }
    }
}
