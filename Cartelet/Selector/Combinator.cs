using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    /// <summary>
    /// 8. Combinators
    /// </summary>
    public class Combinator : Production
    {
        public Combinator(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public String CombinatorValue { get { return Captures.First().Trim(); } }

        /// <summary>
        /// 8.1. Descendant combinator
        /// </summary>
        public Boolean IsDescendant { get { return CombinatorValue == ""; } }

        /// <summary>
        /// 8.2. Child combinators
        /// </summary>
        public Boolean IsChild { get { return CombinatorValue == ">"; } }

        /// <summary>
        /// 8.3.1. Adjacent sibling combinator
        /// </summary>
        public Boolean IsAdjacentSibling { get { return CombinatorValue == "+"; } }

        /// <summary>
        /// 8.3.2. General sibling combinator
        /// </summary>
        public Boolean IsGeneralSibling { get { return CombinatorValue == "~"; } }

        public override string ToString()
        {
            return String.Format("Combinator: {0}", CombinatorValue);
        }
    }
}
