using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Html
{
    public class CompiledSelectorHandler
    {
        public Func<NodeInfo, Boolean> Matcher { get; set; }
        public Func<CarteletContext, NodeInfo, Boolean> Handler { get; set; }
        public Selector.Selector Selector { get; set; }
        public String SelectorString { get; set; }
        public String Type { get; set; }

        public Boolean Match(CarteletContext ctx, NodeInfo nodeInfo)
        {
            if (Matcher(nodeInfo))
            {
                if (Handler != null)
                {
                    Handler(ctx, nodeInfo);
                }
                return true;
            }
            return false;
        }
    }
}
