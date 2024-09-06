using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using System.Collections.Specialized;

namespace IP2C_WebAPI.Services;

public class IpRenewalService : IHostedService, IDisposable
{
    private readonly Ip2cRepository _ip2cRepository;
    private readonly Ip2cService _ip2cService;
    private readonly ILogger<IpRenewalService> _logger;
    private readonly OrderedDictionary _ipCache;
    private readonly object _ipCacheLock;
    private readonly int maxCacheSize;
    public IpRenewalService(IServiceScopeFactory serviceScopeFactory, ILogger<IpRenewalService> logger, IConfiguration configuration)
    {
        var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
        _ip2cRepository = serviceProvider.GetRequiredService<Ip2cRepository>();
        _ip2cService = serviceProvider.GetRequiredService<Ip2cService>();
        _ipCacheLock = new object();
        _logger = logger;
        maxCacheSize = configuration["IpCacheMaxSize"] == null ? 50 : int.Parse(configuration["IpCacheMaxSize"]);
        //populate cache from db
        _ipCache = new OrderedDictionary(maxCacheSize);
        foreach (var cacheEntry in _ip2cRepository.GetIpsWithCountryAsc(maxCacheSize))
        {
            _ipCache[cacheEntry.Ip] = new IpInfoDTO(cacheEntry.TwoLetterCode, cacheEntry.ThreeLetterCode, cacheEntry.CountryName);
        }
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () => await RenewIpsLoop(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    //main service loop, renews the IPs by using the ip2c service and then sleeps for 1 hour
    private async Task RenewIpsLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RenewIpsLoop called");
        while (!cancellationToken.IsCancellationRequested)
        {
            //get all the countries from our db first
            List<Country> countries = await _ip2cRepository.GetCountriesAsync();
            if (countries.Count != 0)
            {
                Dictionary<string, int> countryIdCodes = new Dictionary<string, int>();
                foreach (var country in countries)
                {
                    countryIdCodes[country.ThreeLetterCode] = country.Id;
                }
                //get ips by batches (keyset pagination)
                int lastId = 6; //in our sample db the minimum id = 6
                while (true)
                {
                    var ipPage = await _ip2cRepository.GetIpAddressesRangeAsync(lastId);
                    if (ipPage.Count == 0)
                        break;
                    //update information for these 100 Ip addresses
                    foreach (var ipAddress in ipPage)
                    {
                        (IpInfoDTO ipInfo, IP2C_STATUS result) = await _ip2cService.RetrieveIpInfo(ipAddress.Ip);
                        if (ipInfo != null)
                        {
                            //we have to check if the country for this IP changed
                            if (!countryIdCodes.ContainsKey(ipInfo.ThreeLetterCode))
                            {
                                Country countryToAdd = new Country(default, ipInfo.CountryName, ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, DateTime.Now);
                                await _ip2cRepository.AddCountry(countryToAdd);
                                countryIdCodes[ipInfo.ThreeLetterCode] = countryToAdd.Id;
                            }

                            //update cache (only if changed)
                            lock (_ipCacheLock)
                            {
                                if (_ipCache[ipAddress.Ip] != null && ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                                    _ipCache[ipAddress.Ip] = new IpInfoDTO(ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, ipInfo.CountryName);
                            }
                            //update db, replace old values with new values ONLY if changes occured
                            if (ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                            {
                                ipAddress.Country = default;
                                ipAddress.CountryId = countryIdCodes[ipInfo.ThreeLetterCode];
                                ipAddress.UpdatedAt = DateTime.Now;
                                _ip2cRepository.UpdateIpAddress(ipAddress);
                            }
                        }
                    }
                    //update all IPs at once
                    await _ip2cRepository.SaveChangesAsync();
                    lastId += 100;
                }

            }
            //sleep for 1 hour
            _logger.LogInformation("Service will sleep for 1 hour");
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }

    public IpInfoDTO GetIpInformation(string Ip)
    {
        lock (_ipCacheLock)
        {
            return _ipCache.Contains(Ip) ? (IpInfoDTO)_ipCache[Ip] : null;
        }
    }

    public void UpdateCacheEntry(string Ip, IpInfoDTO infoDTO)
    {
        lock (_ipCacheLock)
        {
            //remove the oldest if we are at the cache limit
            if (_ipCache.Count >= maxCacheSize)
                _ipCache.Remove(_ipCache[0]);
            _ipCache[Ip] = infoDTO;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
