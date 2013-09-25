using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Cartelet.Html;

namespace Cartelet.Mvc
{
    public class CarteletViewEngine : IViewEngine
    {
        public IViewEngine BaseViewEngine { get; private set; }
        public HtmlFilter HtmlFilter { get; set; }
        public Func<String, TextWriter, CarteletContext> CarteletContextFactory { get; set; }

        public CarteletViewEngine(IViewEngine baseViewEngine)
            : this(baseViewEngine, new HtmlFilter(), (content, writer) => new CarteletContext(content, writer))
        {

        }
        public CarteletViewEngine(IViewEngine baseViewEngine, HtmlFilter htmlFilter, Func<String, TextWriter, CarteletContext> contextFactory)
        {
            if (baseViewEngine == null)
                throw new ArgumentNullException("baseViewEngine");
            if (htmlFilter == null)
                throw new ArgumentNullException("htmlFilter");

            BaseViewEngine = baseViewEngine;
            HtmlFilter = htmlFilter;
            CarteletContextFactory = contextFactory;
        }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            // パーシャルに対しては処理しない
            return BaseViewEngine.FindPartialView(controllerContext, partialViewName, useCache);
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var result = BaseViewEngine.FindView(controllerContext, viewName, masterName, useCache);
            if (result.View == null)
                return result;
            return new ViewEngineResult(new CarteletView(result.View, this), this);
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            if (view is CarteletView)
            {
                BaseViewEngine.ReleaseView(controllerContext, (view as CarteletView).BaseView);
            }
            else
            {
                BaseViewEngine.ReleaseView(controllerContext, view);
            }
        }
    }
}
