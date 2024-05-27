using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LiteDB.Utils
{
    public class SlidingCache<TKey, TValue> : IDisposable
    {
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();
        private readonly Timer _timer;

        public SlidingCache(TimeSpan expirationScanFrequency)
        {
            _timer = new Timer(OnTimerCallback, null, expirationScanFrequency, expirationScanFrequency);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan? slidingExpiration = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));
            
            var cacheItem = _cache.GetOrAdd(key, k => new CacheItem<TValue>(valueFactory(k), DateTime.UtcNow, slidingExpiration));
            cacheItem.LastAccessed = DateTime.UtcNow;

            return cacheItem.Value;
        }

        private void OnTimerCallback(object state)
        {
            var now = DateTime.UtcNow;
            foreach (var pair in _cache)
            {
                var key = pair.Key;
                var item = pair.Value;
                if (item.Expired(now))
                    _cache.TryRemove(key, out _);
            }
        }

        private class CacheItem<T>
        {
            private readonly TimeSpan? _slidingExpiration;

            public CacheItem(T value, DateTime lastAccessed, TimeSpan? slidingExpiration)
            {
                _slidingExpiration = slidingExpiration;
                Value = value;
                LastAccessed = lastAccessed;
            }

            public T Value { get; }
            public DateTime LastAccessed { get; set; }

            public bool Expired(DateTime now)
            {
                if (_slidingExpiration == null)
                    return false;

                return now - LastAccessed > _slidingExpiration;
            }
        }
    }
}