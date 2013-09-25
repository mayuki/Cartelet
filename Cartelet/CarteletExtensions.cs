using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cartelet.Html;
using Cartelet.Selector;

namespace Cartelet
{
    /// <summary>
    /// 各種ユーティリティの拡張メソッドを含みます。
    /// </summary>
    public static class CarteletExtensions
    {
        public static void AddWithSelector(this IList<Func<CarteletContext, NodeInfo, Boolean>> handlers, String selector,
            Func<CarteletContext, NodeInfo, Boolean> handler)
        {
            var parsed = new SelectorParser(selector).Parse();
            var compiled = CompiledSelector.Compile(parsed);
            handlers.Add((ctx, nodeInfo) =>
                       {
                           if (compiled(nodeInfo))
                           {
                               return handler(ctx, nodeInfo);
                           }
                           return true;
                       });
        }
    }
}
