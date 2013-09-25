using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class Negation : Production
    {
        public Negation(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        public override int Specificity
        {
            get { return 1; } // TODO: Selectors inside the negation pseudo-class are counted like any other, but the negation itself does not count as a pseudo-class. 
        }
    }
}
