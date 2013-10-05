using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cartelet.Html;
using Cartelet.Selector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cartelet.Tests
{
    [TestClass]
    public class CompiledSelectorTest
    {
        private NodeInfo CreateNode(String name, Dictionary<String, String> attributes)
        {
            var nodeInfo = CreateNode(name, new NodeInfo[0]);
            foreach (var attr in attributes)
            {
                nodeInfo.Attributes[attr.Key] = attr.Value;
            }
            nodeInfo.IsDirty = false;
            return nodeInfo;
        }
        private NodeInfo CreateNode(String name, params NodeInfo[] childNodes)
        {
            var matches = Regex.Match(name, "^([^.#]+)(?:\\.([a-zA-Z0-9_-]+))?(?:#([a-zA-Z0-9_-]+))?");
            var elementName = matches.Groups[1].Value;

            var nodeInfo = new NodeInfo(null, new ClassList(), new AttributesDictionary()) { TagName = elementName, TagNameUpper = elementName.ToUpper() };
            foreach (var child in childNodes)
                nodeInfo.AppendChild(child);

            if (matches.Groups[3].Success)
            {
                nodeInfo.Id = matches.Groups[3].Value;
            }
            if (matches.Groups[2].Success)
            {
                //node.Attributes["class"] = matches.Groups[2].Value;
                nodeInfo.ClassList.Add(matches.Groups[2].Value);
            }

            nodeInfo.IsDirty = false;

            return nodeInfo;
        }

        [TestMethod]
        public void Nth_1()
        {
            CompiledSelector.CompileNth("1")(1).IsTrue();
            CompiledSelector.CompileNth("1")(2).IsFalse();
            CompiledSelector.CompileNth("1")(10).IsFalse();
            CompiledSelector.CompileNth("n")(1).IsTrue();
            CompiledSelector.CompileNth("n")(2).IsTrue();
            CompiledSelector.CompileNth("n")(10).IsTrue();
        }
        [TestMethod]
        public void Nth_2()
        {
            CompiledSelector.CompileNth("1n")(1).IsTrue();
            CompiledSelector.CompileNth("1n")(2).IsTrue();
            CompiledSelector.CompileNth("1n")(10).IsTrue();

            CompiledSelector.CompileNth("2n")(1).IsFalse();
            CompiledSelector.CompileNth("2n")(2).IsTrue();
            CompiledSelector.CompileNth("2n")(3).IsFalse();
            CompiledSelector.CompileNth("2n")(4).IsTrue();
            CompiledSelector.CompileNth("2n")(11).IsFalse();
            CompiledSelector.CompileNth("2n")(12).IsTrue();
            CompiledSelector.CompileNth("2n")(13).IsFalse();
            CompiledSelector.CompileNth("2n")(14).IsTrue();
        }
        [TestMethod]
        public void Nth_3()
        {
            CompiledSelector.CompileNth("2n+1")(1).IsTrue();
            CompiledSelector.CompileNth("2n+1")(2).IsFalse();
            CompiledSelector.CompileNth("2n+1")(3).IsTrue();
            CompiledSelector.CompileNth("2n+1")(4).IsFalse();
            CompiledSelector.CompileNth("2n+1")(11).IsTrue();
            CompiledSelector.CompileNth("2n+1")(12).IsFalse();
            CompiledSelector.CompileNth("2n+1")(13).IsTrue();
            CompiledSelector.CompileNth("2n+1")(14).IsFalse();

            CompiledSelector.CompileNth("2n+0")(1).IsFalse();
            CompiledSelector.CompileNth("2n+0")(2).IsTrue();
            CompiledSelector.CompileNth("2n+0")(3).IsFalse();
            CompiledSelector.CompileNth("2n+0")(4).IsTrue();
            CompiledSelector.CompileNth("2n+0")(11).IsFalse();
            CompiledSelector.CompileNth("2n+0")(12).IsTrue();
            CompiledSelector.CompileNth("2n+0")(13).IsFalse();
            CompiledSelector.CompileNth("2n+0")(14).IsTrue();
        }
        [TestMethod]
        public void Nth_4()
        {
            CompiledSelector.CompileNth("2n +1")(1).IsTrue();
            CompiledSelector.CompileNth("2n+ 1")(2).IsFalse();
            CompiledSelector.CompileNth("2n + 1")(3).IsTrue();
        }
        [TestMethod]
        public void Nth_Odd_Even_1()
        {
            CompiledSelector.CompileNth("odd")(1).IsTrue();
            CompiledSelector.CompileNth("odd")(2).IsFalse();
            CompiledSelector.CompileNth("odd")(3).IsTrue();
            CompiledSelector.CompileNth("odd")(4).IsFalse();
            CompiledSelector.CompileNth("odd")(11).IsTrue();
            CompiledSelector.CompileNth("odd")(12).IsFalse();
            CompiledSelector.CompileNth("odd")(13).IsTrue();
            CompiledSelector.CompileNth("odd")(14).IsFalse();

            CompiledSelector.CompileNth("even")(1).IsFalse();
            CompiledSelector.CompileNth("even")(2).IsTrue();
            CompiledSelector.CompileNth("even")(3).IsFalse();
            CompiledSelector.CompileNth("even")(4).IsTrue();
            CompiledSelector.CompileNth("even")(11).IsFalse();
            CompiledSelector.CompileNth("even")(12).IsTrue();
            CompiledSelector.CompileNth("even")(13).IsFalse();
            CompiledSelector.CompileNth("even")(14).IsTrue();
        }

        [TestMethod]
        public void ComplexTest_1()
        {
            var selector = new SelectorParser("body > section:nth-of-type(2n) #id1 p.class-01 img:first-of-type[src$=\".jpg\"]").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("html",
                        CreateNode("head"),
                        CreateNode("body",
                            CreateNode("header"),
                            CreateNode("section"),
                            CreateNode("section",
                                CreateNode("div#id1",
                                    CreateNode("p.class-01",
                                        CreateNode("object"),
                                        target = CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.jpg" } }),
                                        target2 = CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.jpg" } })
                                    )
                                )
                            ),
                            CreateNode("section"),
                            CreateNode("div")
                        )
                );

            compiled(target).IsTrue();
            compiled(target2).IsFalse();
        }

        [TestMethod]
        public void TypeSelector_1()
        {
            var selector = new SelectorParser("body").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("body")).IsTrue();
            compiled(CreateNode("html")).IsFalse();
        }

        [TestMethod]
        public void UniversalSelector_1()
        {
            var selector = new SelectorParser("*").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("body")).IsTrue();
            compiled(CreateNode("html")).IsTrue();
            compiled(CreateNode("img")).IsTrue();
            compiled(CreateNode("p")).IsTrue();
        }

        [TestMethod]
        public void ClassSelector_1()
        {
            var selector = new SelectorParser(".class-01").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("body.class-01")).IsTrue();
            compiled(CreateNode("body.class-01#id1")).IsTrue();
            compiled(CreateNode("body.class-02")).IsFalse();
            compiled(CreateNode("body")).IsFalse();
        }

        [TestMethod]
        public void IdSelector_1()
        {
            var selector = new SelectorParser("#id1").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("body#id1")).IsTrue();
            compiled(CreateNode("body.class-01#id1")).IsTrue();
            compiled(CreateNode("body#id2")).IsFalse();
            compiled(CreateNode("body")).IsFalse();
        }

        [TestMethod]
        public void AttributeSelector_AttributeName_1()
        {
            var selector = new SelectorParser("img[src]").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.pdf" } })).IsTrue();
            compiled(CreateNode("img", new Dictionary<string, string>() { })).IsFalse();
        }

        [TestMethod]
        public void AttributeSelector_ExactMatch_1()
        {
            var selector = new SelectorParser("img[src='http://www.example.com/test.pdf']").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.pdf" } })).IsTrue();
            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test2.pdf" } })).IsFalse();
            compiled(CreateNode("img", new Dictionary<string, string>() { })).IsFalse();
        }

        [TestMethod]
        public void AttributeSelector_PrefixMatch_1()
        {
            var selector = new SelectorParser("img[src^='http://']").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.pdf" } })).IsTrue();
            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "https://www.example.com/test.pdf" } })).IsFalse();
            compiled(CreateNode("img", new Dictionary<string, string>() { })).IsFalse();
        }

        [TestMethod]
        public void AttributeSelector_SuffixMatch_1()
        {
            var selector = new SelectorParser("img[src$='.pdf']").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.pdf" } })).IsTrue();
            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.xls" } })).IsFalse();
            compiled(CreateNode("img", new Dictionary<string, string>() { })).IsFalse();
        }

        [TestMethod]
        public void AttributeSelector_SubstringMatch_1()
        {
            var selector = new SelectorParser("img[src*='example.com']").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.com/test.pdf" } })).IsTrue();
            compiled(CreateNode("img", new Dictionary<string, string>() { { "src", "http://www.example.org/test.pdf" } })).IsFalse();
            compiled(CreateNode("img", new Dictionary<string, string>() { })).IsFalse();
        }

        [TestMethod]
        public void AttributeSelector_ContainsMatch_1()
        {
            var selector = new SelectorParser("img[class~='class-01']").Parse();
            var compiled = CompiledSelector.Compile(selector);

            compiled(CreateNode("img", new Dictionary<string, string>() { { "class", "class-02 class-01" } })).IsTrue();
            compiled(CreateNode("img", new Dictionary<string, string>() { { "class", "class-02" } })).IsFalse();
            compiled(CreateNode("img", new Dictionary<string, string>() { })).IsFalse();
        }

        [TestMethod]
        public void Combinator_Child_1()
        {
            var selector = new SelectorParser("body > *").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("html",
                        CreateNode("head"),
                        CreateNode("body",
                            target = CreateNode("section",
                                target3 = CreateNode("div",
                                    CreateNode("p",
                                        CreateNode("img")
                                    )
                                )
                            ),
                            target2 = CreateNode("section"),
                            CreateNode("div")
                        )
                );

            compiled(target).IsTrue(); // section[0]
            compiled(target2).IsTrue(); // section[1]
            compiled(target3).IsFalse(); // section[0] > div
        }

        [TestMethod]
        public void Combinator_Descendant_1()
        {
            var selector = new SelectorParser("body > section div img").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("html",
                        CreateNode("head"),
                        CreateNode("body",
                            CreateNode("section",
                                target3 = CreateNode("img"),
                                CreateNode("div",
                                    target2 = CreateNode("img"),
                                    CreateNode("p",
                                        target = CreateNode("img")
                                    )
                                )
                            ),
                            CreateNode("section"),
                            CreateNode("div")
                        )
                );
            compiled(target).IsTrue(); // OK: section[0] > div > p > img
            compiled(target2).IsTrue(); // OK: section[0] > div > img
            compiled(target3).IsFalse(); // NG: section[0] > img
        }

        [TestMethod]
        public void Combinator_GeneralSibling_1()
        {
            var selector = new SelectorParser("p ~ span").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target2 = CreateNode("span"),
                        CreateNode("p"),
                        CreateNode("code"),
                        target = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG (pより前)
        }

        [TestMethod]
        public void Combinator_AdjacentSibling_1()
        {
            var selector = new SelectorParser("p + span").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target2 = CreateNode("span"),
                        CreateNode("p"),
                        target = CreateNode("span"),
                        CreateNode("code"),
                        target3 = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG (pより前)
            compiled(target3).IsFalse(); // NG (直後じゃない)
        }


        [TestMethod]
        public void PseudoSelector_FirstChild_1()
        {
            var selector = new SelectorParser("span:first-child").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target = CreateNode("span"),
                        CreateNode("p"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target3 = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsFalse(); // NG
        }


        [TestMethod]
        public void PseudoSelector_FirstChild_2()
        {
            var selector = new SelectorParser("span:first-child").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target = CreateNode("span")
                );

            compiled(target).IsFalse(); // NG
        }

        [TestMethod]
        public void PseudoSelector_FirstOfType_1()
        {
            var selector = new SelectorParser("span:first-of-type").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target = CreateNode("span"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target3 = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsFalse(); // NG
        }

        [TestMethod]
        public void PseudoSelector_FirstOfType_2()
        {
            var selector = new SelectorParser("span:first-of-type").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
        }

        [TestMethod]
        public void PseudoSelector_LastChild_1()
        {
            var selector = new SelectorParser("span:last-child").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target3 = CreateNode("span"),
                        CreateNode("p"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsFalse(); // NG
        }


        [TestMethod]
        public void PseudoSelector_LastChild_2()
        {
            var selector = new SelectorParser("span:last-child").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target = CreateNode("span"),
                        CreateNode("p")
                );

            compiled(target).IsFalse(); // NG
        }

        [TestMethod]
        public void PseudoSelector_LastOfType_1()
        {
            var selector = new SelectorParser("span:last-of-type").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target3 = CreateNode("span"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsFalse(); // NG
        }

        [TestMethod]
        public void PseudoSelector_LastOfType_2()
        {
            var selector = new SelectorParser("span:last-of-type").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target = CreateNode("span"),
                        CreateNode("p")
                );

            compiled(target).IsTrue(); // OK
        }

        [TestMethod]
        public void PseudoSelector_NthChild_1()
        {
            var selector = new SelectorParser("span:nth-child(1)").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target = CreateNode("span"),
                        CreateNode("p"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target3 = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsFalse(); // NG
        }

        [TestMethod]
        public void PseudoSelector_NthChild_2()
        {
            var selector = new SelectorParser("span:nth-of-type(odd)").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        target = CreateNode("span"),
                        CreateNode("code"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target3 = CreateNode("span"),
                        CreateNode("p")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsTrue(); // OK
        }

        [TestMethod]
        public void PseudoSelector_NthOfType_1()
        {
            var selector = new SelectorParser("span:nth-of-type(1)").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target = CreateNode("span"),
                        CreateNode("code"),
                        target2 = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
        }

        [TestMethod]
        public void PseudoSelector_NthOfType_2()
        {
            var selector = new SelectorParser("span:nth-of-type(odd)").Parse();
            var compiled = CompiledSelector.Compile(selector);

            NodeInfo target, target2, target3;
            var n = CreateNode("div",
                        CreateNode("p"),
                        target = CreateNode("span"),
                        CreateNode("code"),
                        target2 = CreateNode("span"),
                        CreateNode("code"),
                        target3 = CreateNode("span")
                );

            compiled(target).IsTrue(); // OK
            compiled(target2).IsFalse(); // NG
            compiled(target3).IsTrue(); // OK
        }
    }
}
