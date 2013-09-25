using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet
{
    /// <summary>
    /// 値を保持するクラスです。
    /// </summary>
    public class ContextStorage
    {
        internal ContextStorage Parent { get; set; }
        private Lazy<Dictionary<String, Object>> _items = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        public ContextStorage(ContextStorage parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// 値を取得します。
        /// 現在のContextStorageに値が設定されていない場合で、親となるContextStorageがある場合そのContextStorageを呼び出します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(String key)
        {
            if (_items.IsValueCreated && _items.Value.ContainsKey(key))
                return (T)_items.Value[key];

            return (Parent == null) ? default(T) : Parent.Get<T>(key);
        }

        /// <summary>
        /// 値をセットします。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set<T>(String key, T value)
        {
            _items.Value[key] = value;
        }
    }
}
