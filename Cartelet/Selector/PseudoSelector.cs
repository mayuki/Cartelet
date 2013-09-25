using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class PseudoSelector : Production
    {
        public PseudoSelector(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// PseudoName
        /// </summary>
        public String PseudoName { get { return IsFunctional ? Children.OfType<FunctionalPseudoSelector>().First().PseudoName : Captures.First(); } }

        /// <summary>
        /// functional-pseudo
        /// </summary>
        public Boolean IsFunctional { get { return Children.OfType<FunctionalPseudoSelector>().Any(); } }

        public override int Specificity
        {
            get { return 10; } // TODO: pseudo-element = 1, pseudo-class = 10
        }

        public override string ToString()
        {
            return String.Format("PseudoSelector: :{0}{1}", PseudoName, (IsFunctional) ? Children.OfType<FunctionalPseudoSelector>().First().Expression.Value + ")" : "");
        }
    }
}
