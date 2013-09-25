using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    /// <summary>
    /// 6.2. Universal selector
    /// </summary>
    public class UniversalSelector : Production
    {
        public UniversalSelector(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public String Namespace { get { return Children.Where(x => x.Name == "NamespacePrefix").Select(x => x.Captures.FirstOrDefault()).FirstOrDefault(); } }

        public override string ToString()
        {
            return String.Format("UniversalSelector: {0}|{1}", Namespace, "*");
        }
    }
}
