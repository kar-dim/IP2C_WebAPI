using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Interfaces;

namespace IP2C_WebAPI.Services.Implementations;

public class GeoIpRenewalService : IGeoIpRenewalService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ICacheService cache;
    private readonly ILogger<GeoIpRenewalService> logger;
    private CancellationTokenSource _cts;
    private Task _renewalTask;

    public GeoIpRenewalService(IServiceScopeFactory serviceScopeFactory, ICacheService cacheService, ILogger<GeoIpRenewalService> ip2cLogger)
    {
        scopeFactory = serviceScopeFactory;
        logger = ip2cLogger;
        cache = cacheService;
        //populate cache from db
        cache.InitializeCache();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _renewalTask = Task.Run(() => RenewIpsLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_renewalTask is not null)
            await Task.WhenAny(_renewalTask, Task.Delay(Timeout.Infinite, cancellationToken));
        logger.LogInformation("GeoIpRenewalService stopped");
    }

    public void Dispose()
    {
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }

    //main service loop, renews the IPs by using the ip2c service and then sleeps for 1 hour
    public async Task RenewIpsLoop(CancellationToken cancellationToken)
    {
        logger.LogInformation("RenewIpsLoop started");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RenewIps(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RenewIps failed, will retry after 1 hour");
            }

            logger.LogInformation("Service will sleep for 1 hour");
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        logger.LogInformation("RenewIpsLoop stopped");
    }

    private async Task RenewIps(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<Ip2cRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IGeoIpService>();

        //get all the countries and their ID codes from our db first
        var countryIdCodes = await repository.GetCountriesAsDictAsync();
        if (countryIdCodes.Count == 0)
            return;
        //get ips by batches (keyset pagination)
        int lastId = 0;
        while (!cancellationToken.IsCancellationRequested)
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
            //update all batched IPs at once
            await repository.SaveChangesAsync();
            lastId = ipPage.Last().Id;
        }
    }
}
