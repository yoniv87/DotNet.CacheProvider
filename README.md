# DotNet.CacheProvider
*DotNet.CacheProvider* is a helper to implement cache with Redis DB / Memory-Cache - configure by web.config<br/>
CacheProvider also provide a ActionFilterAttribute for MVC / WebApi actions, to store the response in the cache, and can be on GET / POST actions<br/>

You can add this attribute to use cache to your action:
```
[CustomCache("CacheClass1","*","*")]
```
The parameters are:<br/>
1. Cache class name<br/>
2. Vary by query params, separated by ```;```, and can be ```*``` for any<br/>
3. Vary by **POST** params<br/>

In *DotNet.CacheProvider* you can also use Cache for your own needs.
To use CacheService you need to implement these rows:
```
ICacheService cacheService = ICacheService.Resolve();
```
Cache Service have these functions:
```
T Get<T>(string key);
void Set<T>(string key, T data, int cacheTime);
bool IsSet(string key);
void Remove(string key);
void RemoveByPattern(string pattern);
void Clear();
```

In your web.config, you need to add this section in configSections creteria:<br/>
```xml
<section name="CacheSettings" type="CacheProvider.ConfigSections.CacheSettings" />
```

Then, add these lines under configuration:<br/>
```xml
<CacheSettings useRedis="true" redisConnectionUrl="localhost:6379">
    <CacheClasses>```
      
      <!---Add your cache classes here. 
      each class must have name as a uniqe value, and duration in seconds.-->
      
      <!--
      <add name="Class1" duration="600" />
      <add name="Class2" duration="300" />
      -->
      
    </CacheClasses>
  </CacheSettings>
  ```
  The sln can be opened with VS15 or higher

