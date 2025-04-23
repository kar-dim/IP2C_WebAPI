using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Interfaces;

namespace IP2C_WebAPI.Services.Implementations;

public class GeoIpRenewalService : IGeoIpRenewalService
{
    private readonly Ip2cRepository repository;
    private readonly IGeoIpService service;
    private readonly ICacheService cache;
    private readonly ILogger<GeoIpRenewalService> logger;
    
    public GeoIpRenewalService(IServiceScopeFactory serviceScopeFactory, ICacheService cacheService, ILogger<GeoIpRenewalService> ip2cLogger)
    {
        var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
        repository = serviceProvider.GetRequiredService<Ip2cRepository>();
        service = serviceProvider.GetRequiredService<IGeoIpService>();
        logger = ip2cLogger;
        cache = cacheService;
        //populate cache from db
        cache.InitializeCache();
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
    public async Task RenewIpsLoop(CancellationToken cancellationToken)
    {
        logger.LogInformation("RenewIpsLoop called");
        while (!cancellationToken.IsCancellationRequested)
        {
            //get all the countries from our db first
            List<Country> countries = await repository.GetCountriesAsync();
            if (countries.Count != 0)
            {
                Dictionary<string, int> countryIdCodes = [];
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
                        var result = await service.RetrieveIpInfo(ipAddress.Ip);
                        if (!result.IsSuccess)
                            continue;
                        
                        var ipInfo = result.IpInfo;
                        //we have to check if the country for this IP changed
                        if (!countryIdCodes.TryGetValue(ipInfo.ThreeLetterCode, out int countryId))
                        {
                            var countryToAdd = new Country(default, ipInfo.CountryName, ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, DateTime.Now);
                            await repository.AddCountryAsync(countryToAdd);
                            countryId = countryToAdd.Id;
                            countryIdCodes[ipInfo.ThreeLetterCode] = countryId;
                        }

                        //update cache (only if changed)
                        if (cache.GetIpInformation(ipAddress.Ip) != null && ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                            cache.UpdateCacheEntry(ipAddress.Ip, new IpInfoDTO(ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, ipInfo.CountryName));
                            
                        //update db, replace old values with new values ONLY if changes occured
                        if (ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                        {
                            ipAddress.Country = default;
                            ipAddress.CountryId = countryId;
                            ipAddress.UpdatedAt = DateTime.Now;
                            repository.UpdateIpAddress(ipAddress);
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
