using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class FunctionalPseudoSelector : Production
    {
        public FunctionalPseudoSelector(string name, SelectorParser lexier)
            : base(name, lexier)
        {
        }

        public String PseudoName { get { return Captures.First().TrimEnd('('); } }

        public Expression Expression { get { return Children.OfType<Expression>().First(); } }

        public override string ToString()
        {
            return String.Format("PseudoSelector(Functional): :{0}{1}", PseudoName, Expression.Value);
        }
    }
}
