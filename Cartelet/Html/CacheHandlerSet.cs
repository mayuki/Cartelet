using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Html
{
    internal class CacheHandlerSet
    {
        public IList<CompiledSelectorHandler> Handlers { get; set; }
        public IList<CompiledSelectorHandler> HandlersForSimplifiedSelectors { get; set; }
        public IList<CompiledSelectorHandler> HandlersByClassName { get; set; }
        public IList<CompiledSelectorHandler> HandlersByTagName { get; set; }
    }
}
