using System;
using System.IO;
using System.Linq;
using System.Text;
using Cartelet.Html;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cartelet.Tests
{
    [TestClass]
    public class HtmlFilterTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<h1>homuhomu</h1>
<p>mamimami</p>
</body>
</html>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            htmlFilter.Execute(context, node);
        }
        [TestMethod]
        public void InvalidHtml_1()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</titdy></><br/></br/>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            htmlFilter.Execute(context, node);
        }
        [TestMethod]
        public void InvalidHtml_2()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</titdy></><br/><""
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            htmlFilter.Execute(context, node);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void InvalidHtml_3()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</ti><""tdy></><br/><""</div></div></div></div>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            htmlFilter.Execute(context, node);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void InvalidHtml_4()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</ti><></><a></a><><><
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            htmlFilter.Execute(context, node);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void InvalidHtml_5()
        {
            var content = @"
(><) (<span class=""a"">b</span>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            htmlFilter.Execute(context, node);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void WithWriter_1()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<h1>homuhomu</h1>
<p>mamimami</p>
</body>
</html>
            ";

            var htmlFilter = new HtmlFilter();
            htmlFilter.AddHandler("html", (ctx, nodeInfo) => { ctx.Items.Set("Cartelet.Test.UpperTextWriter:Enable", false); return true; });
            htmlFilter.AddHandler("p", (ctx, nodeInfo) => { ctx.Items.Set("Cartelet.Test.UpperTextWriter:Enable", true); return true; });
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);
            context.Writer = new UpperTextWriter(context.Writer, context);
            htmlFilter.Execute(context, node);
        }

        [TestMethod]
        public void RewriteAttribute_1()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<h1>homuhomu</h1>
<p>mamimami</p>
</body>
</html>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);

            var bodyNode = node.ChildNodes[1].ChildNodes[1];
            bodyNode.IsDirty.IsFalse();
            bodyNode.ClassList.Add("class-01");
            bodyNode.IsDirty.IsTrue();

            htmlFilter.Execute(context, node);
        }

        [TestMethod]
        public void RewriteAttribute_2()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<div data-hoge=""&amp;&gt;&quot;&lt;"" id=box>
<h1>homuhomu</h1>
<p>mamimami</p>
<p class=""alert"">font color is red</p>
</div>
</body>
</html>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);

            htmlFilter.AddHandler("h1", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "font-size:xx-large;"; return true; });
            htmlFilter.AddHandler("#box", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "border: 1px solid blue;"; return true; });
            htmlFilter.AddHandler("p.alert", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "color:red;"; return true; });

            htmlFilter.Execute(context, node);

            sw.ToString().Is(@"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<div data-hoge=""&amp;&gt;&quot;&lt;"" id=""box"" style=""border: 1px solid blue;"">
<h1 style=""font-size:xx-large;"">homuhomu</h1>
<p>mamimami</p>
<p class=""alert"" style=""color:red;"">font color is red</p>
</div>
</body>
</html>
            ");

        }

        [TestMethod]
        public void RewriteAttribute_3()
        {
            var content = @"
<!DOCTYPE html>
<title>hauhau</title>
<ul>
<li><a href=""#"">1</a></li>
<li><a href=""#"">2</a></li>
<li><a href=""#"">3</a></li>
<li><a href=""#"">4</a></li>
</ul>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);

            htmlFilter.AddHandler("li:nth-child(2n)", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "color:red;"; return true; });

            htmlFilter.Execute(context, node);

            sw.ToString().Is(@"
<!DOCTYPE html>
<title>hauhau</title>
<ul>
<li><a href=""#"">1</a></li>
<li style=""color:red;""><a href=""#"">2</a></li>
<li><a href=""#"">3</a></li>
<li style=""color:red;""><a href=""#"">4</a></li>
</ul>
            ");
        }

        [TestMethod]
        public void RewriteAttribute_4()
        {
            var content = @"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<div data-hoge=""&amp;&gt;&quot;&lt;"" id=box>
<h1>homuhomu</h1>
<p>mamimami</p>
<p class=""alert"">font color is red</p>
</div>
</body>
</html>
            ";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);

            htmlFilter.AddHandler("CSSExpander", "h1", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "font-size:xx-large;"; return true; });
            htmlFilter.AddHandler("CSSExpander", "#box", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "border: 1px solid blue;"; return true; });
            htmlFilter.AddHandler("CSSExpander", "p.alert", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "color:red;"; return true; });

            htmlFilter.AggregatedHandlers.Add("CSSExpander", (ctx, nodeInfo, matchedHandlers) =>
                                              {
                                                  foreach (var handler in matchedHandlers.OrderBy(x => x.Selector.Specificity))
                                                  {
                                                      handler.Handler(ctx, nodeInfo);
                                                  }
                                                  nodeInfo.ClassList.Clear();
                                                  return true;
                                              });

            htmlFilter.Execute(context, node);

            sw.ToString().Is(@"
<!DOCTYPE html>
<html>
<head>
<meta charset=utf-8>
<title>hauhau</title>
</head>
<body>
<div data-hoge=""&amp;&gt;&quot;&lt;"" id=""box"" style=""border: 1px solid blue;"">
<h1 style=""font-size:xx-large;"">homuhomu</h1>
<p>mamimami</p>
<p style=""color:red;"">font color is red</p>
</div>
</body>
</html>
            ");
        }

        [TestMethod]
        public void RewriteAttribute_5()
        {
            var content = @"
<!DOCTYPE html>
<title>hauhau</title>
<img src=""#"" />
";

            var htmlFilter = new HtmlFilter();
            var node = HtmlParser.Parse(content);
            var sw = new StringWriter();
            var context = new CarteletContext(content, sw);

            htmlFilter.AddHandler("img", (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "margin:0;"; return true; });

            htmlFilter.Execute(context, node);

            sw.ToString().Is(@"
<!DOCTYPE html>
<title>hauhau</title>
<img src=""#"" style=""margin:0;"" />
");
        }

        public class UpperTextWriter : TextWriter
        {
            public TextWriter BaseWriter { get; private set; }
            private CarteletContext _ctx;

            public UpperTextWriter(TextWriter writer, CarteletContext ctx)
            {
                BaseWriter = writer;
                _ctx = ctx;
            }

            public override void Write(char value)
            {
                if (_ctx.Items.Get<Boolean>("Cartelet.Test.UpperTextWriter:Enable"))
                {
                    BaseWriter.Write(Char.ToUpper(value));
                }
                else
                {
                    BaseWriter.Write(value);
                }
            }

            public override void Write(string value)
            {
                if (_ctx.Items.Get<Boolean>("Cartelet.Test.UpperTextWriter:Enable"))
                {
                    BaseWriter.Write(value.ToUpper());
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
}
