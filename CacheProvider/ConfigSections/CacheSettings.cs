using System.Configuration;

namespace CacheProvider.ConfigSections
{
    public class CacheSettings : ConfigurationSection
    {
        public static CacheSettings Settings { get; } = ConfigurationManager.GetSection("CacheSettings") as CacheSettings;

        [ConfigurationProperty("useRedis", DefaultValue = false, IsRequired = false)]
        public bool UseRedis
        {
            get { return (bool)this["useRedis"]; }
            set { this["useRedis"] = value; }
        }
        [ConfigurationProperty("redisConnectionUrl", DefaultValue = "localhost:6379", IsRequired = true)]
        public string RedisConnectionUrl
        {
            get { return (string)this["redisConnectionUrl"]; }
            set { this["redisConnectionUrl"] = value; }
        }

        [ConfigurationProperty("CacheClasses", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(CacheClass),
            AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
        public CacheClassCollection CacheClasses => (CacheClassCollection)base["CacheClasses"];
    }

    public class CacheClass : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("duration", IsRequired = true, IsKey = false)]
        public int Duration
        {
            get { return (int)this["duration"]; }
            set { this["duration"] = value; }
        }
    }

    public class CacheClassCollection : ConfigurationElementCollection
    {
        public CacheClass this[int index]
        {
            get { return (CacheClass)BaseGet(index); }
            set
            {
                if ((CacheClass)BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheClass();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheClass)element).Name;
        }
    }
}
