namespace Mezon_sdk.Managers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Collection<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _data = new Dictionary<TKey, TValue>();
        // To maintain insertion order, we keep a separate list of keys
        private readonly List<TKey> _orderedKeys = new List<TKey>();

        public int Size => _data.Count;

        public TValue? Get(TKey key)
        {
            if (_data.TryGetValue(key, out var value))
            {
                return value;
            }
            return default;
        }

        public void Set(TKey key, TValue value)
        {
            if (!_data.ContainsKey(key))
            {
                _orderedKeys.Add(key);
            }
            _data[key] = value;
        }

        public bool Delete(TKey key)
        {
            if (_data.Remove(key))
            {
                _orderedKeys.Remove(key);
                return true;
            }
            return false;
        }

        public TValue? First()
        {
            if (_orderedKeys.Count == 0)
            {
                return default;
            }
            return _data[_orderedKeys[0]];
        }

        public TKey? FirstKey()
        {
            if (_orderedKeys.Count == 0)
            {
                return default;
            }
            return _orderedKeys[0];
        }

        public Collection<TKey, TValue> Filter(Func<TValue, bool> fn)
        {
            var result = new Collection<TKey, TValue>();
            foreach (var key in _orderedKeys)
            {
                var value = _data[key];
                if (fn(value))
                {
                    result.Set(key, value);
                }
            }
            return result;
        }

        public List<T> Map<T>(Func<TValue, T> fn)
        {
            var result = new List<T>(_orderedKeys.Count);
            foreach (var key in _orderedKeys)
            {
                result.Add(fn(_data[key]));
            }
            return result;
        }

        public IEnumerable<TValue> Values()
        {
            foreach (var key in _orderedKeys)
            {
                yield return _data[key];
            }
        }

        public IEnumerable<TKey> Keys()
        {
            return _orderedKeys;
        }

        public void Clear()
        {
            _data.Clear();
            _orderedKeys.Clear();
        }

        public bool Contains(TKey key)
        {
            return _data.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var key in _orderedKeys)
            {
                yield return new KeyValuePair<TKey, TValue>(key, _data[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class CacheManager<TKey, TValue> where TKey : notnull
    {
        private readonly Func<TKey, Task<TValue>> _fetcher;
        private readonly int? _maxSize;
        public Collection<TKey, TValue> Cache { get; }

        public CacheManager(Func<TKey, Task<TValue>> fetcher, int? maxSize = null)
        {
            _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
            _maxSize = maxSize;
            Cache = new Collection<TKey, TValue>();
        }

        public int Size => Cache.Size;

        public TValue? Get(TKey id)
        {
            return Cache.Get(id);
        }

        public void Set(TKey id, TValue value)
        {
            if (_maxSize.HasValue && Cache.Size >= _maxSize.Value)
            {
                var firstKey = Cache.FirstKey();
                if (firstKey != null)
                {
                    Cache.Delete(firstKey);
                }
            }
            Cache.Set(id, value);
        }

        public async Task<TValue> FetchAsync(TKey id)
        {
            var existing = Get(id);
            if (existing != null)
            {
                return existing;
            }

            var fetched = await _fetcher(id);
            Set(id, fetched);
            return fetched;
        }

        public TValue? First()
        {
            return Cache.First();
        }

        public Collection<TKey, TValue> Filter(Func<TValue, bool> fn)
        {
            return Cache.Filter(fn);
        }

        public List<T> Map<T>(Func<TValue, T> fn)
        {
            return Cache.Map(fn);
        }

        public IEnumerable<TValue> Values()
        {
            return Cache.Values();
        }

        public bool Delete(TKey id)
        {
            return Cache.Delete(id);
        }

        public void Clear()
        {
            Cache.Clear();
        }

        public bool Has(TKey id)
        {
            return Cache.Contains(id);
        }
    }
}
