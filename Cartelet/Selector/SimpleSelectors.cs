using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    /// <summary>
    /// 6. Simple selectors
    /// </summary>
    public class SimpleSelectors : Production
    {
        public SimpleSelectors(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public TypeSelector TypeSelector { get { return Children.OfType<TypeSelector>().First(); } }

        /// <summary>
        /// 
        /// </summary>
        public ClassSelector[] ClassSelectors { get { return Children.OfType<ClassSelector>().ToArray(); } }

        /// <summary>
        /// 
        /// </summary>
        public IdSelector[] IdSelectors { get { return Children.OfType<IdSelector>().ToArray(); } }

        /// <summary>
        /// 
        /// </summary>
        public AttributeSelector[] AttributeSelectors { get { return Children.OfType<AttributeSelector>().ToArray(); } }

        /// <summary>
        /// 
        /// </summary>
        public PseudoSelector[] PseudoSelectors { get { return Children.OfType<PseudoSelector>().ToArray(); } }

        public override int Specificity
        {
            get { return Children.Sum(x => x.Specificity); }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(TypeSelector.Namespace))
            {
                sb.Append(TypeSelector.Namespace).Append("|");
            }
            sb.Append(TypeSelector.ElementName);

            sb.Append(String.Join("", ClassSelectors.Select(x => "." + x.ClassName)));
            sb.Append(String.Join("", IdSelectors.Select(x => "#" + x.Id)));
            sb.Append(String.Join("", PseudoSelectors.Select(x => ":" + x.PseudoName)));

            return String.Format("SimpleSelectors: {0}", sb.ToString());
        }
    }
}
