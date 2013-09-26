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
        public Func<IViewProfiler> ViewProfilerFactory { get; set; }

        private HtmlFilter _htmlFilter;
        private Func<String, TextWriter, CarteletContext> _contextFactory;

        public CarteletView(IView baseView, CarteletViewEngine viewEngine) : this(baseView, viewEngine, null)
        {
        }
        public CarteletView(IView baseView, CarteletViewEngine viewEngine, Func<IViewProfiler> viewProfilerFactory)
        {
            BaseView = baseView;
            ViewProfilerFactory = viewProfilerFactory ?? (() => new DefaultViewProfiler());

            _htmlFilter = viewEngine.HtmlFilter;
            _contextFactory = viewEngine.CarteletContextFactory;
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var profiler = ViewProfilerFactory();
            profiler.Start();

            // Render view
            profiler.OnBeforeRender();
            var writerBuffer = new StringWriter();
            BaseView.Render(viewContext, writerBuffer);
            profiler.OnAfterRender();
            var content = writerBuffer.ToString();

            profiler.OnBeforeCreateContext();
            var ctx = _contextFactory(content, writer);
            profiler.OnAfterCreateContext(ctx);

            if (ctx != null)
            {
                // Parse HTML
                profiler.OnBeforeParse(ctx);
                var rootNode = HtmlParser.Parse(content);
                profiler.OnAfterParsed(ctx);

                // Filter/Rewrite HTML
                profiler.OnBeforeFilter(ctx);
                _htmlFilter.Execute(ctx, rootNode);
                profiler.OnAfterFilter(ctx);
            }
            else
            {
                writer.Write(content);
            }
            profiler.End(ctx, content);
        }
    }
}
