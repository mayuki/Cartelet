using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class Expression : Production
    {
        public Expression(string name, SelectorParser lexier)
            : base(name, lexier)
        {
        }

        /// <summary>
        /// Value
        /// </summary>
        public String Value { get { return String.Join("", Captures); } }

        public override string ToString()
        {
            return String.Format("Expression: {0}", Value);
        }
    }
}
