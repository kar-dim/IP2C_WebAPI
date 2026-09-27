using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Interfaces;

namespace IP2C_WebAPI.Services.Implementations;

public class GeoIpRenewalService(
    IServiceScopeFactory serviceScopeFactory,
    ICacheService cacheService,
    ILogger<GeoIpRenewalService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GeoIpRenewalService background worker started");

        // Asynchronously warm the cache on startup without blocking DI resolution
        try
        {
            await cacheService.InitializeCacheAsync(stoppingToken);
            logger.LogInformation("GeoIpRenewalService: Cache warm-up completed successfully");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GeoIpRenewalService: Failed to initialize IP cache on startup");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RenewIpsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GeoIpRenewalService: Renewal cycle encountered an error; will retry in 1 hour");
            }

            logger.LogInformation("GeoIpRenewalService: Sleeping for 1 hour until next renewal cycle");
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("GeoIpRenewalService background worker stopped");
    }

    public async Task RenewIpsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GeoIpRenewalService: Starting IP renewal cycle");

        // Load country cache once at the start of renewal
        Dictionary<string, int> countryIdCodes;
        using (var initialScope = serviceScopeFactory.CreateScope())
        {
            var initialRepo = initialScope.ServiceProvider.GetRequiredService<IIp2cRepository>();
            countryIdCodes = await initialRepo.GetCountriesAsDictAsync(cancellationToken);
        }

        if (countryIdCodes.Count == 0)
        {
            logger.LogWarning("GeoIpRenewalService: No countries found in database. Skipping renewal.");
            return;
        }

        var countryLock = new object();
        int lastId = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Fresh scope per batch prevents DbContext memory bloat
            using var batchScope = serviceScopeFactory.CreateScope();
            var repository = batchScope.ServiceProvider.GetRequiredService<IIp2cRepository>();
            var service = batchScope.ServiceProvider.GetRequiredService<IGeoIpService>();

            var ipPage = await repository.GetIpAddressesRangeAsync(lastId, 100, cancellationToken);
            if (ipPage.Count == 0)
                break;

            // Controlled parallel HTTP calls (5 concurrent) to avoid overloading ip2c.org
            await Parallel.ForEachAsync(ipPage, new ParallelOptions
            {
                MaxDegreeOfParallelism = 5,
                CancellationToken = cancellationToken
            }, async (ipAddress, ct) =>
            {
                var result = await service.RetrieveIpInfoAsync(ipAddress.Ip, ct);
                if (!result.IsSuccess || result.IpInfo is null)
                    return;

                var ipInfo = result.IpInfo;
                var threeLetter = ipInfo.ThreeLetterCode.Trim();

                int countryId;
                lock (countryLock)
                {
                    if (!countryIdCodes.TryGetValue(threeLetter, out countryId))
                    {
                        using var countryScope = serviceScopeFactory.CreateScope();
                        var countryRepo = countryScope.ServiceProvider.GetRequiredService<IIp2cRepository>();

                        var existingCountry = countryRepo.GetCountryFromIP2CInfoAsync(ipInfo, ct).GetAwaiter().GetResult();
                        if (existingCountry is null)
                        {
                            var newCountry = new Country(0, ipInfo.CountryName, ipInfo.TwoLetterCode.Trim(), threeLetter, DateTime.UtcNow);
                            countryRepo.AddCountryAsync(newCountry, ct).GetAwaiter().GetResult();
                            countryId = newCountry.Id;
                        }
                        else
                        {
                            countryId = existingCountry.Id;
                        }

                        countryIdCodes[threeLetter] = countryId;
                    }
                }

                bool countryChanged = ipAddress.Country is null ||
                    !string.Equals(ipAddress.Country.ThreeLetterCode.Trim(), threeLetter, StringComparison.OrdinalIgnoreCase);

                if (countryChanged)
                {
                    ipAddress.Country = null;
                    ipAddress.CountryId = countryId;
                    ipAddress.UpdatedAt = DateTime.UtcNow;
                    repository.UpdateIpAddress(ipAddress);

                    if (cacheService.Get(ipAddress.Ip) is not null)
                    {
                        cacheService.Set(ipAddress.Ip, ipInfo);
                    }
                }
                else
                {
                    // Update verification timestamp even when country remained identical
                    ipAddress.UpdatedAt = DateTime.UtcNow;
                    repository.UpdateIpAddress(ipAddress);
                }
            });

            await repository.SaveChangesAsync(cancellationToken);
            lastId = ipPage.Last().Id;
        }

        logger.LogInformation("GeoIpRenewalService: IP renewal cycle finished successfully");
    }
}
