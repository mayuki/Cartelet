using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet
{
    public class AttributesDictionary : IDictionary<String, String>
    {
        public Action OnChanged { get; set; }
        private IDictionary<String, String> _dict;

        public AttributesDictionary() : this(null)
        {
        }
        public AttributesDictionary(Action onChanged)
        {
            OnChanged = onChanged;
            _dict = new Dictionary<String, String>(StringComparer.Ordinal);
        }

        public void Add(string key, string value)
        {
            _dict.Add(key, value);
            if (OnChanged != null) OnChanged();
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _dict.Keys; }
        }

        public bool Remove(string key)
        {
            var result = _dict.Remove(key);
            if (OnChanged != null) OnChanged();
            return result;
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get { return _dict.Values; }
        }

        public string this[string key]
        {
            get
            {
                return _dict[key];
            }
            set
            {
                _dict[key] = value;
                if (OnChanged != null) OnChanged();
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _dict.Add(item);
            if (OnChanged != null) OnChanged();
        }

        public void Clear()
        {
            _dict.Clear();
            if (OnChanged != null) OnChanged();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _dict.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return _dict.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            var result = _dict.Remove(item);
            if (OnChanged != null) OnChanged();
            return result;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
