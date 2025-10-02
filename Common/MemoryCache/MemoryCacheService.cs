using Microsoft.Extensions.Caching.Memory;

namespace Common.MemoryCache
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T? Get<T>(string key)
        {
            return _cache.TryGetValue(key, out T value) ? value : default;
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var options = new MemoryCacheEntryOptions();
            if (absoluteExpiration.HasValue)
                options.SetAbsoluteExpiration(absoluteExpiration.Value);

            _cache.Set(key, value, options);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}