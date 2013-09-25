using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class AttributeSelector : Production
    {
        public AttributeSelector(string name, SelectorParser lexier) : base(name, lexier)
        {
        }

        public String AttributeName { get { return Captures[0]; } }
        public String Value { get { return Captures.Count > 1 ? Captures[2].Trim('"', '\'') : null; } }

        public Boolean IsAttributeNameMatch { get { return Captures.Count == 1; } }
        public Boolean IsSubcodeMatch { get { return Captures.Count > 1 ? Captures[1] == "|=" : false; } }
        public Boolean IsExactMatch { get { return Captures.Count > 1 ? Captures[1] == "=" : false; } }
        public Boolean IsContainsMatch { get { return Captures.Count > 1 ? Captures[1] == "~=" : false; } }
        public Boolean IsPrefixMatch { get { return Captures.Count > 1 ? Captures[1] == "^=" : false; } }
        public Boolean IsSuffixMatch { get { return Captures.Count > 1 ? Captures[1] == "$=" : false; } }
        public Boolean IsSubstringMatch { get { return Captures.Count > 1 ? Captures[1] == "*=" : false; } }

        public override int Specificity
        {
            get { return 10; }
        }
    }
}
