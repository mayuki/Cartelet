using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Mvc
{
    public class CarteletViewException : Exception
    {
        public CarteletViewException(string message, Exception innerException) : base(message, innerException)
        {}
    }
}
