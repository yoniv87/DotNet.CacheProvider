using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using CacheProvider.ConfigSections;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CacheProvider.Services.Cache
{
    public class RedisCacheService : ICacheService, IDisposable
    {
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly IDatabase _redisDb;
        public RedisCacheService()
        {
            CacheSettings cacheSettings = ConfigurationManager.GetSection("CacheSettings") as CacheSettings;
            if (cacheSettings != null)
            {
                _redisConnection = ConnectionMultiplexer.Connect(cacheSettings.RedisConnectionUrl);
                _redisDb = _redisConnection.GetDatabase();
            }
            else
            {
                throw new ConfigurationErrorsException($"Section {nameof(cacheSettings)} is missing");
            }
        }

        public T Get<T>(string key)
        {
            if (!IsSet(key))
            {
                return default(T);
            }
            string value = _redisDb.StringGet(key);
            return JsonConvert.DeserializeObject<T>(value);
        }

        public void Set<T>(string key, T data, int cacheTime)
        {
            if (data == null) return;
            string value = JsonConvert.SerializeObject(data);
            TimeSpan? expiresIn = TimeSpan.FromSeconds(cacheTime);
            if (cacheTime <= 0) expiresIn = null;
            _redisDb.StringSet(key, value, expiresIn);
        }

        public bool IsSet(string key)
        {
            return _redisDb.KeyExists(key);
        }

        public void Remove(string key)
        {
            _redisDb.KeyDelete(key);
        }

        public void RemoveByPattern(string pattern)
        {
            foreach (EndPoint ep in _redisConnection.GetEndPoints())
            {
                IServer server = _redisConnection.GetServer(ep);
                IEnumerable<RedisKey> keys = server.Keys(pattern: "*" + pattern + "*");
                foreach (RedisKey key in keys)
                    _redisDb.KeyDelete(key);
            }
        }

        public void Clear()
        {
            foreach (EndPoint ep in _redisConnection.GetEndPoints())
            {
                IServer server = _redisConnection.GetServer(ep);
                IEnumerable<RedisKey> keys = server.Keys();
                foreach (RedisKey key in keys)
                    _redisDb.KeyDelete(key);
            }
        }

        public void Dispose()
        {
            _redisConnection?.Dispose();
        }
    }
}
