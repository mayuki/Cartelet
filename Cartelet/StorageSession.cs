using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet
{
    internal class StorageSession : IDisposable
    {
        private CarteletContext _ctx;
        public StorageSession(CarteletContext ctx)
        {
            _ctx = ctx;
            _ctx.Items = new ContextStorage(_ctx.Items);
        }
        public void Dispose()
        {
            _ctx.Items = _ctx.Items.Parent ?? _ctx.Items;
        }
    }
}
