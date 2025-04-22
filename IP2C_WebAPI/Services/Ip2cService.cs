using IP2C_WebAPI.Common;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Runtime;
using System.Text.RegularExpressions;

namespace IP2C_WebAPI.Services;


//Service that implements the business logic of IP2C operations
public class Ip2cService(ILogger<Ip2cService> logger, CacheService cacheService, RestClient client, Ip2cRepository repository)
{
    private static readonly Regex ipPattern = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");

    //calls IP2C Rest endpoint and retrieves IP Info
    public async Task<Ip2cResult> RetrieveIpInfo(string ip)
    {
        //GET request to -> https://ip2c.org/{ip}
        var response = await client.ExecuteAsync(new RestRequest(ip, Method.Get));
        if (string.IsNullOrWhiteSpace(response?.Content))
        {
            logger.LogError("Ip2c API connection error...");
            return new Ip2cResult(null, IP2C_STATUS.CONNECTION_ERROR);
        }
        //ip2c returns in format "1;CD;COD;COUNTRY" (string, no JSON)
        var parts = response.Content.Split(';');
        if (!parts[0].Equals("1") || parts.Length < 4)
        {
            logger.LogError("Ip2c API IP not found error...");
            return new Ip2cResult(null, IP2C_STATUS.API_ERROR);
        }
        //truncate to 50 characters for db safety
        return new Ip2cResult(new IpInfoDTO(parts[1], parts[2], parts[3][..Math.Min(parts[3].Length, 50)]), IP2C_STATUS.OK);
    }

    public async Task<IActionResult> GetIpInfo(string Ip)
    {
        //basic validation of IP (regex works for ipv4 addresses)
        if (!ipPattern.IsMatch(Ip))
        {
            logger.LogError("GetIpInfo: BAD Ip received");
            return Response.IP2C_BAD_IP;
        }
        //first check cache
        var ipInfo = cacheService.GetIpInformation(Ip);
        if (ipInfo != null)
        {
            logger.LogInformation("GetIpInfo: CACHE HIT for {ip} returned to client", Ip);
            return Response.Ok(ipInfo); //ipInfo in BODY json
        }
        logger.LogInformation("GetIpInfo: CACHE MISS for {ip}, checking in db..", Ip);

        //second try from db
        IpCountryRelation entry = await repository.GetIpWithCountryAsync(Ip);
        //if entry is not null -> found in db
        if (entry != null)
        {
            logger.LogInformation("GetIpInfo: Found Ip Info in db, returned to client");
            ipInfo = new IpInfoDTO(entry.TwoLetterCode, entry.ThreeLetterCode, entry.CountryName);
            //update cache
            cacheService.UpdateCacheEntry(Ip, ipInfo);
            return Response.Ok(ipInfo);
        }

        //last try from ip2c service
        logger.LogError("GetIpInfo: Could not find Ip Info in db for IP : {ip}, calling IP2C service...", Ip);

        var result = await RetrieveIpInfo(Ip);
        if (!result.IsSuccess)
            return result.Status == IP2C_STATUS.API_ERROR ? Response.IP_NOT_FOUND : Response.INTERNAL_ERROR;

        //if OK update cache and DB
        ipInfo = result.IpInfo;
        cacheService.UpdateCacheEntry(Ip, ipInfo);
        var countryRecord = await repository.GetCountryFromIP2CInfoAsync(ipInfo);
        if (countryRecord == null)
        {
            countryRecord = new Country(default, ipInfo.CountryName, ipInfo.TwoLetterCode, ipInfo.ThreeLetterCode, DateTime.Now);
            await repository.AddCountryAsync(countryRecord);
        }
        await repository.AddIpAddressAsync(new IpAddress(default, countryRecord.Id, Ip, DateTime.Now, DateTime.Now, default));
        logger.LogInformation("GetIpInfo: Found Ip Info from IP2C service, returned to client");

        return Response.Ok(ipInfo);
    }

    public async Task<IActionResult> GetIpReport(string[] countryCodes)
    {
        if (countryCodes?.Any(code => string.IsNullOrWhiteSpace(code) || code.Trim().Length != 2) == true)
        {
            logger.LogError("GetIpReport: At least one invalid country code received");
            return Response.IP2C_BAD_COUNTRY_CODE;
        }

        bool showAllIps = countryCodes == null || countryCodes.Length == 0;
        var results = showAllIps ? await repository.GetAllIpsAsync() : await repository.GetAllIpsFromCountryCodesAsync(countryCodes);

        logger.LogInformation("GetIpReport: Report generated successfully. IPs count: {Count}", results.Count);
        return Response.Ok(results);
    }
}
