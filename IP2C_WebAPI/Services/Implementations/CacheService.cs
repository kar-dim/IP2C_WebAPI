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

        public void InitializeCache()
        {
            lock (cacheLock)
            {
                foreach (var cacheEntry in repository.GetIpsWithCountryAsc(maxCacheSize))
                {
                    cache[cacheEntry.Ip] = new IpInfoDTO(cacheEntry.TwoLetterCode, cacheEntry.ThreeLetterCode, cacheEntry.CountryName);
                }
            }
        }
        public IpInfoDTO GetIpInformation(string Ip)
        {
            lock (cacheLock)
            {
                return cache.Contains(Ip) ? (IpInfoDTO)cache[Ip] : null;
            }
        }

        public void UpdateCacheEntry(string Ip, IpInfoDTO infoDTO)
        {
            lock (cacheLock)
            {
                //remove the oldest if we are at the cache limit
                if (cache.Count >= maxCacheSize)
                    cache.Remove(cache[0]);
                cache[Ip] = infoDTO;
            }
        }
    }
}
