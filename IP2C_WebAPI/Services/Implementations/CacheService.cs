using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Interfaces;

namespace IP2C_WebAPI.Services.Implementations;

public class CacheService : ICacheService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly System.Collections.Generic.OrderedDictionary<string, IpInfoDTO> _cache;
    private readonly object _cacheLock = new();
    private readonly int _maxCacheSize;

    public CacheService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
    {
        _scopeFactory = serviceScopeFactory;
        _maxCacheSize = configuration.GetValue("IpCacheMaxSize", 50);
        _cache = new System.Collections.Generic.OrderedDictionary<string, IpInfoDTO>(_maxCacheSize > 0 ? _maxCacheSize : 50);
    }

    public async Task InitializeCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_maxCacheSize <= 0)
            return;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIp2cRepository>();
        var entries = await repository.GetLatestIpsWithCountryAsync(_maxCacheSize, cancellationToken);

        lock (_cacheLock)
        {
            _cache.Clear();
            foreach (var (ip, info) in entries)
            {
                _cache[ip] = info;
            }
        }
    }

    public IpInfoDTO? Get(string ip)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(ip, out var info))
            {
                // Refresh LRU position by moving to the end
                _cache.Remove(ip);
                _cache[ip] = info;
                return info;
            }

            return null;
        }
    }

    public void Set(string ip, IpInfoDTO infoDTO)
    {
        if (_maxCacheSize <= 0)
            return;

        lock (_cacheLock)
        {
            if (_cache.ContainsKey(ip))
            {
                _cache.Remove(ip);
            }
            else if (_cache.Count >= _maxCacheSize)
            {
                _cache.RemoveAt(0);
            }

            _cache[ip] = infoDTO;
        }
    }
}
