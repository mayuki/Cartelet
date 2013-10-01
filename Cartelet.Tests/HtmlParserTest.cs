using System;
using System.IO;
using System.Linq;
using Cartelet.Html;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cartelet.Tests
{
    [TestClass]
    public class HtmlParserTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var rootNode = HtmlParser.Parse(@"<!DOCTYPE html>
<html>
<head>
<meta charset=UTF-8>
<title>Hauhau</title>
<script async src=test.js></script>
</head>
<body>
<section id=""section-01"">
<img src=hoge.png alt=hoge fuga />
<img src='gaogao.jpg' alt='Gaogao'>
<p>fugafuga</p>
</section>
</body>
</html>");
        }

        [TestMethod]
        public void TestMethod2()
        {
            var content = @"<!DOCTYPE html>
<meta charset=UTF-8>
<title>Hauhau</title>
<script async src=test.js></script>
<section id=""section-01"">
<img src=hoge.png alt=hoge fuga />
<img src='gaogao.jpg' alt='Gaogao'>
<p>fugafuga</p>
</section>
";
            var rootNode = HtmlParser.Parse(content);

            rootNode.ChildNodes[0].TagNameUpper.Is("!DOCTYPE");
            rootNode.ChildNodes[1].TagNameUpper.Is("META");
            rootNode.ChildNodes[2].TagNameUpper.Is("TITLE");
            rootNode.ChildNodes[3].TagNameUpper.Is("SCRIPT");
            rootNode.ChildNodes[4].TagNameUpper.Is("SECTION");

            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Attribute_1()
        {
            var content = @"<meta charset=UTF-8>";
            var rootNode = HtmlParser.Parse(content);

            var node = rootNode.ChildNodes[0];
            node.TagNameUpper.Is("META");
            node.Attributes.Count.IsNot(0);
            node.Attributes["charset"].Is("UTF-8");
            node.IsXmlStyleSelfClose.IsFalse();

            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Attribute_2()
        {
            var content = @"<p foo = bar baz= maumau hoge =fuga moge =muga=>a</p>";
            var rootNode = HtmlParser.Parse(content);
            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Attribute_3()
        {
            var content = @"<img src=hoge.png alt />";
            var rootNode = HtmlParser.Parse(content);

            var node = rootNode.ChildNodes[0];
            node.TagNameUpper.Is("IMG");
            node.Attributes.Count.IsNot(0);
            node.Attributes["src"].Is("hoge.png");
            node.Attributes["alt"].Is("");
            node.IsXmlStyleSelfClose.IsTrue();

            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Comment_1()
        {
            var content = @"<!DOCTYPE html>
<meta charset=UTF-8>
<title>Hauhau</title>
<script async src=test.js></script>
<section id=""section-01"">
<!--<img src=hoge.png alt=hoge fuga />
<img src='gaogao.jpg' alt='Gaogao'>-->
<p>fugafuga</p>
</section>
";
            var rootNode = HtmlParser.Parse(content);
            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Comment_2()
        {
            var content = @"<!DOCTYPE html>
<meta charset=UTF-8>
<title>Hauhau</title>
<script async src=test.js></script>
<section id=""section-01"">
<!--
--
<img src=hoge.png alt=hoge fuga />
<img src='gaogao.jpg' alt='Gaogao'>
-->
<p>fugafuga<!--</p> --></p>
</section>
";
            var rootNode = HtmlParser.Parse(content);
            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Element_Script_1()
        {
            var content = @"<script>document.write('</scr' + 'pt>');</script>";
            var rootNode = HtmlParser.Parse(content);
            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }


        [TestMethod]
        public void Element_SelfClose_1()
        {
            var content = @"<br/><span></span>";
            var rootNode = HtmlParser.Parse(content);

            var node = rootNode.ChildNodes[0];
            node.TagNameUpper.Is("BR");
            node.ChildNodes.Any().IsFalse();
            node.IsXmlStyleSelfClose.IsTrue();

            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Element_SelfClose_2()
        {
            var content = @"<br /><span></span>";
            var rootNode = HtmlParser.Parse(content);

            var node = rootNode.ChildNodes[0];
            node.TagNameUpper.Is("BR");
            node.ChildNodes.Any().IsFalse();
            node.IsXmlStyleSelfClose.IsTrue();

            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }

        [TestMethod]
        public void Element_SelfClose_3()
        {
            var content = @"<br><span></span>";
            var rootNode = HtmlParser.Parse(content);

            var node = rootNode.ChildNodes[0];
            node.TagNameUpper.Is("BR");
            node.ChildNodes.Any().IsFalse();
            node.IsXmlStyleSelfClose.IsFalse();

            var sw = new StringWriter();
            HtmlParser.ToHtmlString(rootNode, content, sw);
            sw.ToString().Is(content);
        }
    }
}
