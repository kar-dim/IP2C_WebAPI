using System.Net;
using System.Net.Sockets;
using IP2C_WebAPI.Common;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using RestSharp;

namespace IP2C_WebAPI.Services.Implementations;

public class GeoIpService(
    ILogger<GeoIpService> logger,
    ICacheService cacheService,
    RestClient client,
    IIp2cRepository repository) : IGeoIpService
{
    public async Task<Ip2cResult> RetrieveIpInfoAsync(string ip, CancellationToken cancellationToken = default)
    {
        RestResponse response;
        try
        {
            var request = new RestRequest(ip, Method.Get);
            response = await client.ExecuteAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to ip2c.org for IP {Ip}", ip);
            return Ip2cResult.Failure(Ip2cStatus.ConnectionError, "Unable to establish connection to the external IP2C service.");
        }

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            logger.LogWarning("Ip2c API returned HTTP error {StatusCode} for IP {Ip}: {ErrorMessage}",
                response.StatusCode, ip, response.ErrorMessage);

            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return Ip2cResult.Failure(Ip2cStatus.UpstreamError, "The upstream IP2C service is currently unavailable or rate-limited.");
            }

            return Ip2cResult.Failure(Ip2cStatus.ConnectionError, "Unable to retrieve response from the upstream IP2C service.");
        }

        // ip2c format: "1;TwoLetter;ThreeLetter;CountryName"
        var trimmed = response.Content.Trim();
        var parts = trimmed.Split(';');
        if (parts.Length < 4 || !parts[0].Equals("1", StringComparison.Ordinal))
        {
            logger.LogInformation("Ip2c API returned no country match for IP {Ip}: {Content}", ip, trimmed);
            return Ip2cResult.Failure(Ip2cStatus.NotFound, $"No country information was found for IP address '{ip}'.");
        }

        var countryName = parts[3].Trim();
        if (countryName.Length > 50)
        {
            countryName = countryName[..50];
        }

        var info = new IpInfoDTO(parts[1].Trim(), parts[2].Trim(), countryName);
        return Ip2cResult.Success(info);
    }

    public async Task<Ip2cResult> GetIpInfoAsync(string ip, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ip) ||
            !IPAddress.TryParse(ip, out var parsedAddress) ||
            (parsedAddress.AddressFamily != AddressFamily.InterNetwork && parsedAddress.AddressFamily != AddressFamily.InterNetworkV6))
        {
            logger.LogWarning("GetIpInfoAsync: Invalid IP address: '{Ip}'", ip);
            return Ip2cResult.Failure(Ip2cStatus.InvalidIp, $"The provided IP address '{ip}' is invalid.");
        }

        var normalizedIp = parsedAddress.ToString();

        // 1. Check in-memory cache
        var cachedInfo = cacheService.Get(normalizedIp);
        if (cachedInfo != null)
        {
            logger.LogInformation("GetIpInfoAsync: CACHE HIT for {Ip}", normalizedIp);
            return Ip2cResult.Success(cachedInfo);
        }

        logger.LogInformation("GetIpInfoAsync: CACHE MISS for {Ip}, querying database...", normalizedIp);

        // 2. Check local database
        var dbInfo = await repository.GetIpWithCountryAsync(normalizedIp, cancellationToken);
        if (dbInfo != null)
        {
            logger.LogInformation("GetIpInfoAsync: Found IP {Ip} in database", normalizedIp);
            cacheService.Set(normalizedIp, dbInfo);
            return Ip2cResult.Success(dbInfo);
        }

        // 3. Fallback to external IP2C service
        logger.LogInformation("GetIpInfoAsync: IP {Ip} not in database, querying ip2c.org...", normalizedIp);
        var result = await RetrieveIpInfoAsync(normalizedIp, cancellationToken);
        if (!result.IsSuccess || result.IpInfo is null)
        {
            return result;
        }

        var ipInfo = result.IpInfo;
        cacheService.Set(normalizedIp, ipInfo);

        // 4. Save to database with concurrency race condition handling
        try
        {
            var countryRecord = await repository.GetCountryFromIP2CInfoAsync(ipInfo, cancellationToken);
            if (countryRecord == null)
            {
                countryRecord = new Country(0, ipInfo.CountryName, ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, DateTime.UtcNow);
                await repository.AddCountryAsync(countryRecord, cancellationToken);
            }

            var ipAddress = new IpAddress(0, countryRecord.Id, normalizedIp, DateTime.UtcNow, DateTime.UtcNow);
            await repository.AddIpAddressAsync(ipAddress, cancellationToken);
            logger.LogInformation("GetIpInfoAsync: Successfully saved IP {Ip} to database", normalizedIp);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict saving IP {Ip} to database; another request inserted it concurrently.", normalizedIp);
            var existing = await repository.GetIpWithCountryAsync(normalizedIp, cancellationToken);
            if (existing != null)
            {
                return Ip2cResult.Success(existing);
            }
        }

        return Ip2cResult.Success(ipInfo);
    }

    public async Task<IpReportResult> GetReportAsync(string[]? countryCodes, CancellationToken cancellationToken = default)
    {
        string[] normalizedCodes = [];

        if (countryCodes is { Length: > 0 })
        {
            var list = new List<string>();
            foreach (var raw in countryCodes)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var segments = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var code in segments)
                {
                    if (code.Length != 2)
                    {
                        logger.LogWarning("GetReportAsync: Invalid country code received: '{Code}'", code);
                        return IpReportResult.Failure(Ip2cStatus.InvalidCountryCode, $"Country code '{code}' is invalid. ISO-2 country codes must be exactly 2 letters (e.g. 'US', 'GR').");
                    }

                    list.Add(code.ToUpperInvariant());
                }
            }

            normalizedCodes = list.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var results = await repository.GetIpReportAsync(normalizedCodes, cancellationToken);
        logger.LogInformation("GetReportAsync: Generated report successfully with {Count} country groups", results.Count);
        return IpReportResult.Success(results);
    }
}
