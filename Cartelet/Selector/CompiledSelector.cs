using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cartelet.Html;

namespace Cartelet.Selector
{
    /// <summary>
    /// そのノードがセレクタにマッチするかを返す関数を生成するクラスです。
    /// </summary>
    public class CompiledSelector
    {
        public static Func<Int32, Boolean> CompileNth(String nth)
        {
            switch (nth)
            {
                case "odd":
                    return (v) => v % 2 == 1;
                case "even":
                    return (v) => v % 2 == 0;
            }

            var m = Regex.Match(nth, @"(([+-]?\d+)?n)?\s*([+-])?\s*(\d+)?");

            var value1 = m.Groups[2].Success ? Int32.Parse(m.Groups[2].Value) : 1;
            var op     = m.Groups[3].Success ? m.Groups[3].Value == "+" ? 1 : -1 : 1;
            var value2 = m.Groups[4].Success ? Int32.Parse(m.Groups[4].Value) : 0;
            if (m.Groups[1].Success)
            {
                return (v) => (v % value1) - (op * value2) == 0;
            }
            else
            {
                return (v) => v == (op * value2);
            }
        }

        /// <summary>
        /// セレクタからマッチテストの関数を生成します。
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Func<NodeInfo, Boolean> Compile(Production selector)
        {
            Func<NodeInfo, Boolean> composedMatcher = (nodeInfo) => true;
            var children = new Queue<Production>(selector.Children);
            while (children.Any())
            {
                var child = children.Dequeue();
                Func<Production, Func<NodeInfo, Boolean>> matcher;
                switch (child.Name)
                {
                    case "Combinator":
                        // コンビネーターは特殊
                        composedMatcher = ComposeCombinator(child, composedMatcher, Compile(children.Dequeue()));
                        continue;
                    case "TypeSelector":
                        matcher = TypeSelectorMatcher;
                        break;
                    case "UniversalSelector":
                        matcher = UniversalSelectorMatcher;
                        break;
                    case "Class":
                        matcher = ClassSelectorMatcher;
                        break;
                    case "Id":
                        matcher = IdSelectorMatcher;
                        break;
                    case "Pseudo":
                        matcher = PseudoSelectorMatcher;
                        break;
                    case "Attrib":
                        matcher = AttributeSelectorMatcher;
                        break;
                    case "SimpleSelectorSequence":
                        matcher = Compile;
                        break;
                    default:
                        throw new NotImplementedException(child.Name);
                }

                composedMatcher = Compose(composedMatcher, matcher(child));
            }

            return composedMatcher;
        }

        private static Func<NodeInfo, Boolean> Compose(Func<NodeInfo, Boolean> left, Func<NodeInfo, Boolean> right)
        {
            return (nodeInfo) => right(nodeInfo) && left(nodeInfo);
        }

        private static Func<NodeInfo, Boolean> ComposeCombinator(Production production, Func<NodeInfo, Boolean> left, Func<NodeInfo, Boolean> right)
        {
            var combinator = production as Combinator;
            return (nodeInfo) =>
                   {
                       if (!right(nodeInfo))
                           return false;

                       if (combinator.IsChild)
                       {
                           // 親 (>)
                           return left(nodeInfo.Parent);
                       }
                       else if (combinator.IsDescendant)
                       {
                           // 祖先 ( )
                           var parent = nodeInfo.Parent;
                           while (parent != null)
                           {
                               if (left(parent))
                               {
                                   return true;
                               }
                               parent = parent.Parent;
                           }
                           return false;
                       }
                       else if (combinator.IsAdjacentSibling)
                       {
                           // 隣接セレクタ (+)
                           // 直後兄弟
                           // 親が同じで left の直後にあるやつ
                           var beforeNode = nodeInfo.Parent.ChildNodes.TakeWhile(x => x != nodeInfo).LastOrDefault(); // 自分の直前
                           return (beforeNode != null) && left(beforeNode);
                       }
                       else if (combinator.IsGeneralSibling)
                       {
                           // 間接セレクタ (~)
                           // 親が共通で自分より前にある
                           // https://developer.mozilla.org/ja/docs/Web/CSS/General_sibling_selectors
                           var beforeNodes = nodeInfo.Parent.ChildNodes.TakeWhile(x => x != nodeInfo).ToList(); // 自分より前
                           return beforeNodes.Any(left);
                       }

                       throw new NotImplementedException("Not Implemented Combinator: " + combinator.CombinatorValue);
                   };
        }

        #region Selectors
        // Universal Selector (*)
        private static Func<NodeInfo, Boolean> UniversalSelectorMatcher(Production production)
        {
            return (nodeInfo) => true;
        }

        // Type Selector
        private static Func<NodeInfo, Boolean> TypeSelectorMatcher(Production production)
        {
            var nameUpper = (production as TypeSelector).ElementName.ToUpper();
            return (nodeInfo) => nodeInfo.TagNameUpper == nameUpper;
        }

        // Class Selector
        private static Func<NodeInfo, Boolean> ClassSelectorMatcher(Production production)
        {
            var className = (production as ClassSelector).ClassName;
            return (nodeInfo) => nodeInfo.ClassList.Contains(className);
        }

        // Id Selector
        private static Func<NodeInfo, Boolean> IdSelectorMatcher(Production production)
        {
            var id = (production as IdSelector).Id;
            return (nodeInfo) => (nodeInfo.Attributes.ContainsKey("id") && nodeInfo.Attributes["id"] == id);
        }

        // Pseudo Selector
        private static Func<NodeInfo, Boolean> PseudoSelectorMatcher(Production production)
        {
            var pseudo = (production as PseudoSelector);
            if (pseudo.IsFunctional)
            {
                // :nth-child(...) みたいなの
                var funcPseudo = pseudo.Children[0] as FunctionalPseudoSelector;
                var nth = CompileNth(funcPseudo.Expression.Value);
                switch (funcPseudo.PseudoName)
                {
                    case "nth-child":
                        return (nodeInfo) => nth(nodeInfo.Index + 1);
                    case "nth-of-type":
                        return (nodeInfo) => nth(nodeInfo.IndexOfType.Value + 1);
                    // TODO: nth-last-childとか
                }
            }
            else
            {
                switch (pseudo.PseudoName)
                {
                    case "first-child":
                        return (nodeInfo) => nodeInfo.IsFirstChild;
                    case "last-child":
                        return (nodeInfo) => nodeInfo.IsLastChild;
                    case "first-of-type":
                        return (nodeInfo) => nodeInfo.IsFirstOfType.Value;
                    case "last-of-type":
                        return (nodeInfo) => nodeInfo.IsLastOfType.Value;
                    case "only-child":
                        return (nodeInfo) => nodeInfo.Parent.ChildNodes.Count == 1 && nodeInfo.Parent.ChildNodes[0] == nodeInfo;
                    case "empty":
                        // TODO: 現状では中身(テキストノード)を見るのが難しい。
                        throw new NotImplementedException(pseudo.ToString());
                }
            }
            throw new NotImplementedException(pseudo.ToString());
        }

        // Attrib Selector
        private static Func<NodeInfo, Boolean> AttributeSelectorMatcher(Production production)
        {
            var attrib = (production as AttributeSelector);
            if (attrib.IsExactMatch)
            {
                return (nodeInfo) => nodeInfo.Attributes.ContainsKey(attrib.AttributeName) && nodeInfo.Attributes[attrib.AttributeName] == attrib.Value;
            }
            else if (attrib.IsPrefixMatch)
            {
                return (nodeInfo) => nodeInfo.Attributes.ContainsKey(attrib.AttributeName) && nodeInfo.Attributes[attrib.AttributeName].StartsWith(attrib.Value);
            }
            else if (attrib.IsSuffixMatch)
            {
                return (nodeInfo) => nodeInfo.Attributes.ContainsKey(attrib.AttributeName) && nodeInfo.Attributes[attrib.AttributeName].EndsWith(attrib.Value);
            }
            else if (attrib.IsSubstringMatch)
            {
                return (nodeInfo) => nodeInfo.Attributes.ContainsKey(attrib.AttributeName) && nodeInfo.Attributes[attrib.AttributeName].Contains(attrib.Value);
            }
            else if (attrib.IsAttributeNameMatch)
            {
                return (nodeInfo) => nodeInfo.Attributes.ContainsKey(attrib.AttributeName);
            }
            else if (attrib.IsContainsMatch)
            {
                return (nodeInfo) => nodeInfo.Attributes.ContainsKey(attrib.AttributeName) && nodeInfo.Attributes[attrib.AttributeName].Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Contains(attrib.Value);
            }

            throw new NotImplementedException(attrib.ToString());
        }

        #endregion

    }
}
