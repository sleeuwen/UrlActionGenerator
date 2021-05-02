using System;
using System.Collections;
using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public class KeyedCollection<TDescriptor> : IList<TDescriptor>
    {
        private readonly List<TDescriptor> _source;
        private readonly HashSet<object> _keys;
        private readonly Func<TDescriptor, object> _keySelector;

        public KeyedCollection(Func<TDescriptor, object> keySelector)
        {
            _source = new List<TDescriptor>();
            _keys = new HashSet<object>();
            _keySelector = keySelector;
        }

        public IEnumerator<TDescriptor> GetEnumerator() => _source.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _source.GetEnumerator();

        public void Add(TDescriptor item)
        {
            if (_keys.Add(_keySelector(item)))
            {
                _source.Add(item);
            }
        }

        public void Clear() => throw new NotImplementedException();

        public bool Contains(TDescriptor item) => throw new NotImplementedException();

        public void CopyTo(TDescriptor[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(TDescriptor item) => throw new NotImplementedException();

        public int Count => _source.Count;
        public bool IsReadOnly => false;

        public int IndexOf(TDescriptor item) => throw new NotImplementedException();

        public void Insert(int index, TDescriptor item) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        public TDescriptor this[int index]
        {
            get => _source[index];
            set => throw new NotImplementedException();
        }
    }
}
