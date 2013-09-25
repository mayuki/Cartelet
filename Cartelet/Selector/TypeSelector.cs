using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    /// <summary>
    /// 6.1. Type selector
    /// </summary>
    public class TypeSelector : Production
    {
        public TypeSelector(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public String ElementName { get { return Children.First(x => x.Name == "ElementName").Captures.FirstOrDefault(); } }

        /// <summary>
        /// 
        /// </summary>
        public String Namespace { get { return Children.Where(x => x.Name == "NamespacePrefix").Select(x => x.Captures.FirstOrDefault()).FirstOrDefault(); } }

        public override int Specificity
        {
            get { return 1; }
        }
        public override string ToString()
        {
            return String.Format("TypeSelector: {0}|{1}", Namespace, ElementName);
        }
    }
}
