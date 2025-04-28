using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Interfaces;
using System.Collections.Specialized;

namespace IP2C_WebAPI.Services.Implementations
{
    public class CacheService : ICacheService
    {
        private readonly Ip2cRepository repository;
        private readonly OrderedDictionary cache;
        private readonly object cacheLock;
        private readonly int maxCacheSize;

        public CacheService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            repository = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<Ip2cRepository>();
            cacheLock = new object();
            maxCacheSize = configuration["IpCacheMaxSize"] == null ? 50 : int.Parse(configuration["IpCacheMaxSize"]);
            cache = new OrderedDictionary(maxCacheSize);
        }

        //All cache operations must execute under this method, in order to lock the cache, execute, and then release the lock
        private T ExecuteWithCacheLock<T>(Func<T> action)
        {
            lock (cacheLock)
                return action();
        }
        private void ExecuteWithCacheLock(Action action) => ExecuteWithCacheLock(() => { action(); return true; });

        public void InitializeCache() => ExecuteWithCacheLock(() =>
        {
            foreach (var cacheEntry in repository.GetIpsWithCountryAsc(maxCacheSize))
                cache[cacheEntry.Ip] = new IpInfoDTO(cacheEntry.TwoLetterCode, cacheEntry.ThreeLetterCode, cacheEntry.CountryName);
        });

        public IpInfoDTO GetIpInformation(string Ip) => ExecuteWithCacheLock(() =>
            cache.Contains(Ip) ? (IpInfoDTO)cache[Ip] : null
        );

        public void UpdateCacheEntry(string Ip, IpInfoDTO infoDTO) => ExecuteWithCacheLock(() =>
        {
            if (cache.Count >= maxCacheSize)
                cache.Remove(cache[0]);
            cache[Ip] = infoDTO;
        });
    }
}
