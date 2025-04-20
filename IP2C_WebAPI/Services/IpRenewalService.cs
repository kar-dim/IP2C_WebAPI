using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using System.Collections.Specialized;

namespace IP2C_WebAPI.Services;

public class IpRenewalService : IHostedService, IDisposable
{
    private readonly Ip2cRepository repository;
    private readonly Ip2cService service;
    private readonly ILogger<IpRenewalService> logger;
    private readonly OrderedDictionary cache;
    private readonly object cacheLock;
    private readonly int maxCacheSize;
    public IpRenewalService(IServiceScopeFactory serviceScopeFactory, ILogger<IpRenewalService> ip2cLogger, IConfiguration configuration)
    {
        var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
        repository = serviceProvider.GetRequiredService<Ip2cRepository>();
        service = serviceProvider.GetRequiredService<Ip2cService>();
        cacheLock = new object();
        logger = ip2cLogger;
        maxCacheSize = configuration["IpCacheMaxSize"] == null ? 50 : int.Parse(configuration["IpCacheMaxSize"]);
        //populate cache from db
        cache = new OrderedDictionary(maxCacheSize);
        foreach (var cacheEntry in repository.GetIpsWithCountryAsc(maxCacheSize))
        {
            cache[cacheEntry.Ip] = new IpInfoDTO(cacheEntry.TwoLetterCode, cacheEntry.ThreeLetterCode, cacheEntry.CountryName);
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
        logger.LogInformation("RenewIpsLoop called");
        while (!cancellationToken.IsCancellationRequested)
        {
            //get all the countries from our db first
            List<Country> countries = await repository.GetCountriesAsync();
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
                    var ipPage = await repository.GetIpAddressesRangeAsync(lastId);
                    if (ipPage.Count == 0)
                        break;
                    //update information for these 100 Ip addresses
                    foreach (var ipAddress in ipPage)
                    {
                        (IpInfoDTO ipInfo, IP2C_STATUS result) = await service.RetrieveIpInfo(ipAddress.Ip);
                        if (ipInfo != null)
                        {
                            //we have to check if the country for this IP changed
                            if (!countryIdCodes.ContainsKey(ipInfo.ThreeLetterCode))
                            {
                                Country countryToAdd = new Country(default, ipInfo.CountryName, ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, DateTime.Now);
                                await repository.AddCountry(countryToAdd);
                                countryIdCodes[ipInfo.ThreeLetterCode] = countryToAdd.Id;
                            }

                            //update cache (only if changed)
                            lock (cacheLock)
                            {
                                if (cache[ipAddress.Ip] != null && ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                                    cache[ipAddress.Ip] = new IpInfoDTO(ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, ipInfo.CountryName);
                            }
                            //update db, replace old values with new values ONLY if changes occured
                            if (ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                            {
                                ipAddress.Country = default;
                                ipAddress.CountryId = countryIdCodes[ipInfo.ThreeLetterCode];
                                ipAddress.UpdatedAt = DateTime.Now;
                                repository.UpdateIpAddress(ipAddress);
                            }
                        }
                    }
                    //update all IPs at once
                    await repository.SaveChangesAsync();
                    lastId += 100;
                }

            }
            //sleep for 1 hour
            logger.LogInformation("Service will sleep for 1 hour");
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
