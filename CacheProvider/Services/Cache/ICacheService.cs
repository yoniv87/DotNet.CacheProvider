using System;
using System.Configuration;
using CacheProvider.ConfigSections;

namespace CacheProvider.Services.Cache
{
    public interface ICacheService
    {
        T Get<T>(string key);
        void Set<T>(string key, T data, int cacheTime);
        bool IsSet(string key);
        void Remove(string key);
        void RemoveByPattern(string pattern);
        void Clear();
    }

    public static class CacheServiceExtension
    {
        public static ICacheService Resolve(this ICacheService cacheService)
        {
            CacheSettings cacheSettings = ConfigurationManager.GetSection("CacheSettings") as CacheSettings;
            if (cacheSettings == null)
            {
                throw new ConfigurationErrorsException($"Missing {nameof(CacheSettings)} configuration");
            }
            if (cacheSettings.UseRedis)
            {
                return CacheServiceManager.RedisCacheInstance;
            }
            return CacheServiceManager.MemCacheInstance;
        }
    }

    internal class CacheServiceManager
    {
        private static readonly Lazy<ICacheService> RedisLazy = new Lazy<ICacheService>(() => new RedisCacheService());
        private static readonly Lazy<ICacheService> MemCacheLazy = new Lazy<ICacheService>(() => new MemoryCacheService());
        private CacheServiceManager()
        {
        }
        public static ICacheService RedisCacheInstance => RedisLazy.Value;
        public static ICacheService MemCacheInstance => MemCacheLazy.Value;
    }
}
