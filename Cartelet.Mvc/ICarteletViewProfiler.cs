using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Cartelet.Mvc
{
    public interface ICarteletViewProfiler
    {
        void Start();

        void OnBeforeRender();
        void OnAfterRender();

        void OnBeforeCreateContext();
        void OnAfterCreateContext(CarteletContext ctx);

        void OnBeforeParse(CarteletContext ctx);
        void OnAfterParsed(CarteletContext ctx);

        void OnBeforeFilter(CarteletContext ctx);
        void OnAfterFilter(CarteletContext ctx);

        void End(CarteletContext ctx, String resultContent);
    }

    public class DefaultCarteletViewProfiler : ICarteletViewProfiler
    {
        private Stopwatch _stopwatch;
        private Int64 _renderMs;
        private Int64 _parseMs;
        private Int64 _filterMs;

        public void Start()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        public void OnBeforeRender()
        {
        }

        public void OnAfterRender()
        {
            _renderMs = _stopwatch.ElapsedMilliseconds;
        }

        public void OnBeforeCreateContext()
        {
        }

        public void OnAfterCreateContext(CarteletContext ctx)
        {
        }

        public void OnBeforeParse(CarteletContext ctx)
        {
        }

        public void OnAfterParsed(CarteletContext ctx)
        {
            _parseMs = _stopwatch.ElapsedMilliseconds;
        }

        public void OnBeforeFilter(CarteletContext ctx)
        {
        }

        public void OnAfterFilter(CarteletContext ctx)
        {
            _filterMs = _stopwatch.ElapsedMilliseconds;
        }

        public void End(CarteletContext ctx, String resultContent)
        {
            if (ctx != null)
            {
                Trace.WriteLine(String.Format(
                    "CarteletView: Render:{0}ms(+0), Parse:{1}ms(+{3}ms), Filter:{2}ms(+{4}ms)/Match:{5}ms/Handler:{6}ms, Length:{7}"
                    , _renderMs, _parseMs, _filterMs, _parseMs - _renderMs, _filterMs - _parseMs, ctx.ElapsedSelectorMatchTicks / 10000.0, ctx.ElapsedHandlerTicks / 10000.0, resultContent.Length));
            }
        }
    }
}
