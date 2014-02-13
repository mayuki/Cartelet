using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Cartelet.Html;
using Cartelet.Mvc;

namespace Cartelet.Sample.Web
{
    public class CarteletConfig
    {
        public static void Register(ViewEngineCollection viewEngineCollection)
        {
            // CarteletのHTMLフィルターを作成
            var htmlFilter = new HtmlFilter();
            
            // HTMLフィルターにCSS(~/Asset/Css/feature.css)を展開するハンドラなどを登録
            // 更新チェックはMvcApplication.Application_BeginRequestで
            StylesheetExpander.StylesheetExpander.Register(htmlFilter, HttpContext.Current.Server.MapPath("~/Asset/Css/feature.css"));

            // Carteletを通すViewEngineを登録
            var viewEngine = new CarteletViewEngine(new RazorViewEngine());
            viewEngineCollection.Clear();
            viewEngineCollection.Add(viewEngine);

            // Carteletのコンテキスト(変換の単位=リクエストごとのオブジェクト)を生成するメソッドをViewEngineにセット
            viewEngine.CarteletContextFactory = (content, writer) =>
                                                {
                                                    // 例えばガラケーの時だけWriterをかぶせるとか…
                                                    // ここでは小文字化するWriterをセットする
                                                    var ctx = new CarteletContext(content, writer);
                                                    ctx.Writer = new LowerTextWriter(writer, ctx);
                                                    // class属性を保持する
                                                    //ctx.Items.Set(StylesheetExpander.StylesheetExpander.ContextKeyPreserveClassNames, true);
                                                    return ctx;
                                                };

            // HTMLフィルターを取得するためのメソッドをセット(ファクトリだけどスレッドセーフな処理な限り、同じものを返しても問題ない)
            viewEngine.HtmlFilterFactory = () =>
                                           {
                                               return htmlFilter;
                                           };


            // その他フィルター
            htmlFilter.AddHandler("td", (context, node) =>
                                                {
                                                    node.BeforeContent = "<div style=\"font-size:xx-small;\">";
                                                    node.AfterContent = "</div>";
                                                    return true;
                                                });
            htmlFilter.AddHandler(".to-lower", (context, node) =>
                                               {
                                                   // 小文字化Writerが有効になるようにContextのItemsにtrueをセットする
                                                   // このデータはマッチした要素より上の要素には影響を与えない
                                                   context.Items.Set("Cartelet.Sample.Web.UpperTextWriter:Enable", true);
                                                   return true;
                                               });
            htmlFilter.AddHandler("img", (context, node) =>
                                                       {
                                                           // 画像のalt属性がなかったらsrc属性から付けてみる
                                                           if (!node.Attributes.ContainsKey("alt"))
                                                           {
                                                               node.Attributes["alt"] = node.Attributes["title"] = Path.GetFileName(node.Attributes["src"]);
                                                           }
                                                           return true;
                                                       });
        }
    }

    /// <summary>
    /// 文字列をすべて小文字にするWriter
    /// </summary>
    public class LowerTextWriter : TextWriter
    {
        public TextWriter BaseWriter { get; private set; }
        private CarteletContext _ctx;

        public LowerTextWriter(TextWriter writer, CarteletContext ctx)
        {
            BaseWriter = writer;
            _ctx = ctx;
        }

        public override void Write(char value)
        {
            if (_ctx.Items.Get<Boolean>("Cartelet.Sample.Web.UpperTextWriter:Enable"))
            {
                BaseWriter.Write(Char.ToLower(value));
            }
            else
            {
                BaseWriter.Write(value);
            }
        }

        public override void Write(string value)
        {
            if (_ctx.Items.Get<Boolean>("Cartelet.Sample.Web.UpperTextWriter:Enable"))
            {
                BaseWriter.Write(value.ToLower());
            }
            else
            {
                BaseWriter.Write(value);
            }
        }

        public override Encoding Encoding
        {
            get { return BaseWriter.Encoding; }
        }
    }
}