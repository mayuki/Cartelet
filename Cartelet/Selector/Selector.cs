using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class Selector : Production
    {
        public Selector(string name, SelectorParser lexier) : base(name, lexier)
        {
            _specificity = new Lazy<int>(() => Children.Sum(x => x.Specificity), LazyThreadSafetyMode.PublicationOnly);
        }

        private Lazy<Int32> _specificity;
        public override Int32 Specificity
        {
            get { return _specificity.Value; }
        }
    }
}
