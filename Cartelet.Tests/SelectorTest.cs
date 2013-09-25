using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cartelet.Selector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cartelet.Tests
{
    [TestClass]
    public class SelectorTest
    {
        [TestMethod]
        public void LexierTest()
        {
            var parser = new SelectorParser(" ");
            parser.Space().IsTrue();
            parser.Space().IsFalse();
            parser = new SelectorParser(" \t  ");
            parser.Space().IsTrue();

            parser = new SelectorParser("\\xa");
            parser.Escape().IsTrue();
            parser = new SelectorParser("\n");
            parser.Escape().IsFalse();
        }

        [TestMethod]
        public void LexierTest2()
        {
            SelectorParser parser;

            parser = new SelectorParser("#foobar");
            parser.Parse();

            parser = new SelectorParser("html|body.class-01 > section#id + a:hover img:nth-child(2n)");
            var ret = parser.Parse();

            parser = new SelectorParser("html|body   >  a:not(.hoge)");
            ret = parser.Parse();
        }

        [TestMethod]
        public void Attribute_Name_1()
        {
            var selector = new SelectorParser("div[id]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsTrue();
        }

        [TestMethod]
        public void Attribute_Exact_1()
        {
            var selector = new SelectorParser("div[id=hoge]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsExactMatch.IsTrue();
            attrSelector.Value.Is("hoge");
        }

        [TestMethod]
        public void Attribute_Prefix_1()
        {
            var selector = new SelectorParser("div[id^=hoge]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsPrefixMatch.IsTrue();
            attrSelector.Value.Is("hoge");
        }

        [TestMethod]
        public void Attribute_Suffix_1()
        {
            var selector = new SelectorParser("div[id$=hoge]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsSuffixMatch.IsTrue();
            attrSelector.Value.Is("hoge");
        }

        [TestMethod]
        public void Attribute_Contains_1()
        {
            var selector = new SelectorParser("div[id~=hoge]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsContainsMatch.IsTrue();
            attrSelector.Value.Is("hoge");
        }

        [TestMethod]
        public void Attribute_Substring_1()
        {
            var selector = new SelectorParser("div[id*=hoge]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsSubstringMatch.IsTrue();
            attrSelector.Value.Is("hoge");
        }

        [TestMethod]
        public void Attribute_7()
        {
            var selector = new SelectorParser("div[id=\"hauhau homuhomu\"]").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsExactMatch.IsTrue();
            attrSelector.Value.Is("hauhau homuhomu");
        }

        [TestMethod]
        public void Attribute_8()
        {
            var selector = new SelectorParser("div[id='hauhau homuhomu']").Parse();
            var attrSelector = selector.Children[0].Children[1] as AttributeSelector;
            attrSelector.IsNotNull();
            attrSelector.AttributeName.Is("id");
            attrSelector.IsAttributeNameMatch.IsFalse();
            attrSelector.IsExactMatch.IsTrue();
            attrSelector.Value.Is("hauhau homuhomu");
        }

        [TestMethod]
        public void Specificity_1()
        {
            // 9. Calculating a selector's specificity
            // http://www.w3.org/TR/css3-selectors/#specificity

            var selector = new SelectorParser("*").Parse();
            selector.Specificity.Is(0);
            selector = new SelectorParser("LI").Parse();
            selector.Specificity.Is(1);
            selector = new SelectorParser("UL LI").Parse();
            selector.Specificity.Is(2);
            selector = new SelectorParser("UL OL+LI").Parse();
            selector.Specificity.Is(3);
            selector = new SelectorParser("H1 + *[REL=up]").Parse();
            selector.Specificity.Is(11);
            selector = new SelectorParser("UL OL LI.red").Parse();
            selector.Specificity.Is(13);
            selector = new SelectorParser("LI.red.level").Parse();
            selector.Specificity.Is(21);
            selector = new SelectorParser("#x34y").Parse();
            selector.Specificity.Is(100);
            selector = new SelectorParser("#s12:not(FOO)").Parse();
            selector.Specificity.Is(101);
        }
    }
}
