using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    /// <summary>
    /// 6.4. Class selectors
    /// </summary>
    public class ClassSelector : Production
    {
        public ClassSelector(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// クラス名
        /// </summary>
        public String ClassName { get { return this.Captures.First(); } }

        public override int Specificity
        {
            get { return 10; }
        }

        public override string ToString()
        {
            return String.Format("ClassSelector: .{0}", ClassName);
        }
    }
}
