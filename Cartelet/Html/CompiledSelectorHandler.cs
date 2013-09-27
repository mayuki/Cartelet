using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cartelet.Selector;

namespace Cartelet.Html
{
    public class CompiledSelectorHandler
    {
        private Selector.Selector _selector;
        public Func<NodeInfo, Boolean> Matcher { get; set; }
        public Func<CarteletContext, NodeInfo, Boolean> Handler { get; set; }

        public Selector.Selector Selector
        {
            get { return _selector; }
            set
            {
                _selector = value;
                UpdateRequiredClassNamesAndIds();
            }
        }
        public String SelectorString { get; set; }
        public String Type { get; set; }
        public HashSet<String> RequiredClassNames { get; set; }
        public HashSet<String> RequiredIds { get; set; }

        public Boolean Match(CarteletContext ctx, NodeInfo nodeInfo)
        {
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var isClassNameCascaded = (RequiredClassNames.Count == 0) || RequiredClassNames.IsSubsetOf(nodeInfo.CascadeClassNames);
            var isIdCascaded = (RequiredIds.Count == 0) || RequiredIds.IsSubsetOf(nodeInfo.CascadeIds);
#if DEBUG
            stopwatch.Stop();
            ctx.TraceCountHandlers[this].MatchPreMatcherElapsedTicks += stopwatch.ElapsedTicks;
#endif
            if (isClassNameCascaded && isIdCascaded && Matcher(nodeInfo))
            {
                if (Handler != null)
                {
                    Handler(ctx, nodeInfo);
                }
                return true;
            }
            return false;
        }

        private void UpdateRequiredClassNamesAndIds()
        {
            var ids = new HashSet<String>(StringComparer.Ordinal);
            var classNames = new HashSet<String>(StringComparer.Ordinal);
            for (var i = 0; i < Selector.Children.Count; i++)
            {
                if (Selector.Children.Count > i + 1)
                {
                    var combinator = (Selector.Children[i + 1] as Combinator);
                    if (combinator != null && (combinator.IsChild || combinator.IsDescendant))
                    {
                        foreach (var classSelector in Selector.Children[i].Children.OfType<ClassSelector>())
                        {
                            classNames.Add(classSelector.ClassName);
                        }
                        foreach (var idSelector in Selector.Children[i].Children.OfType<IdSelector>())
                        {
                            ids.Add(idSelector.Id);
                        }
                    }
                }
            }

            RequiredIds = ids;
            RequiredClassNames = classNames;
        }

        public override string ToString()
        {
            return "CompiledSelectorHandler: " + SelectorString;
        }
    }
}
