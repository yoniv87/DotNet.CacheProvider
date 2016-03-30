using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;

namespace CacheProvider.Services.Cache
{
    public class MemoryCacheService : ICacheService
    {
        private ObjectCache Cache { get; } = MemoryCache.Default;
        public T Get<T>(string key)
        {
            return (T)Cache[key];
        }

        public void Set<T>(string key, T data, int cacheTime)
        {
            if (data == null)
            {
                return;
            }
            if (IsSet(key))
            {
                Remove(key);
            }
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTime.Now + TimeSpan.FromSeconds(cacheTime);
            Cache.Add(new CacheItem(key, data), policy);
        }

        public bool IsSet(string key)
        {
            return Cache.Contains(key);
        }

        public void Remove(string key)
        {
            Cache.Remove(key);
        }

        public void RemoveByPattern(string pattern)
        {
            Regex regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            List<string> keysToRemove = (from item in Cache where regex.IsMatch(item.Key) select item.Key).ToList();

            foreach (string key in keysToRemove)
            {
                Remove(key);
            }
        }

        public void Clear()
        {
            foreach (KeyValuePair<string, object> item in Cache)
                Remove(item.Key);
        }
    }
}
