using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class IdSelector : Production
    {
        public IdSelector(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// ID
        /// </summary>
        public String Id { get { return this.Captures.First().TrimStart('#'); } }

        public override int Specificity
        {
            get { return 100; }
        }

        public override string ToString()
        {
            return String.Format("IdSelector: #{0}", Id);
        }
    }
}
