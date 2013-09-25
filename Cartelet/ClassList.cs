using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet
{
    public class ClassList : IEnumerable<String>
    {
        private HashSet<String> _classList;
        public Action OnChanged { get; set; }

        public Int32 Count { get { return _classList.Count; } }

        public ClassList() : this(null)
        {}
        public ClassList(Action onChanged)
        {
            OnChanged = onChanged;
            _classList = new HashSet<string>(StringComparer.Ordinal);
        }

        public void Add(String className)
        {
            _classList.Add(className);
            if (OnChanged != null) OnChanged();
        }

        public void Remove(String className)
        {
            _classList.Remove(className);
            if (OnChanged != null) OnChanged();
        }

        public Boolean Contains(String className)
        {
            return _classList.Contains(className);
        }

        public Boolean Toggle(String className)
        {
            var contains = Contains(className);

            if (contains)
            {
                Remove(className);
            }
            else
            {
                Add(className);
            }

            return !contains;
        }

        public void Clear()
        {
            _classList.Clear();
            if (OnChanged != null) OnChanged();
        }

        public IEnumerator<String> GetEnumerator()
        {
            return _classList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
