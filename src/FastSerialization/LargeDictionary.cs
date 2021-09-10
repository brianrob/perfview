using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public class LargeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private List<Dictionary<TKey, TValue>> _dictionarySegments = new List<Dictionary<TKey, TValue>>();
        private int _segmentSize = 100_000_000;
        private int _segments = 1;
        private IEqualityComparer<TKey> _comparer;

        public LargeDictionary(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;

            _segments = (capacity / _segmentSize) + 1;

            for (int i = 0; i < _segments; i++)
            {
                _dictionarySegments.Add(new Dictionary<TKey, TValue>());
            }
        }

        public TValue this[TKey key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ICollection<TValue> Values => throw new NotImplementedException();

        public int Count => _dictionarySegments.Sum(dictionary => dictionary.Count);

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(TKey key, TValue value)
        {
            int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetSegment = hashCode % _segments;

            _dictionarySegments[targetSegment].Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _dictionarySegments.ForEach(dictionary => dictionary.Clear());
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetSegment = hashCode % _segments;

            return _dictionarySegments[targetSegment].ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetSegment = hashCode % _segments;

            return _dictionarySegments[targetSegment].TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
