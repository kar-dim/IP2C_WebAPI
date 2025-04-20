using IP2C_WebAPI.Common;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Text.RegularExpressions;

namespace IP2C_WebAPI.Services;
public enum IP2C_STATUS
{
    OK,
    CONNECTION_ERROR,
    API_ERROR
}

//Service that implements the business logic of IP2C operations
public class Ip2cService(ILogger<Ip2cService> logger, CacheService cacheService, RestClient client, Ip2cRepository repository)
{
    private static readonly Regex ipPattern = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");

    //calls IP2C Rest endpoint and retrieves IP Info
    public async Task<(IpInfoDTO, IP2C_STATUS)> RetrieveIpInfo(string ip)
    {
        //GET request to -> https://ip2c.org/{ip}
        var response = await client.ExecuteAsync(new RestRequest(ip, Method.Get));
        if (response == null || string.IsNullOrEmpty(response.Content))
        {
            logger.LogError("Ip2c API connection error...");
            return (null, IP2C_STATUS.CONNECTION_ERROR);
        }
        //ip2c returns in format "1;CD;COD;COUNTRY" (string, no JSON)
        string[] parts = response.Content.Split(';');
        if (!parts[0].Equals("1"))
        {
            logger.LogError("Ip2c API ip not found error...");
            return (null, IP2C_STATUS.API_ERROR);
        }
        //truncate to 50 characters for db safety
        return (new IpInfoDTO(parts[1], parts[2], parts[3][..Math.Min(parts[3].Length, 50)]), IP2C_STATUS.OK);
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
        IpInfoDTO ipInfo = cacheService.GetIpInformation(Ip);
        if (ipInfo != null)
        {
            logger.LogInformation("GetIpInfo: CACHE HIT for {ip} returned to client", Ip);
            return Response.Ok(ipInfo); //ipInfo in BODY json
        }
        logger.LogInformation("GetIpInfo: CACHE MISS for {ip}, checking in db..", Ip);

        //second try from db
        IpCountryRelation entry = await repository.GetIpWithCountry(Ip);
        //if entry is not null -> found in db
        if (entry != null)
        {
            logger.LogInformation("GetIpInfo: Found Ip Info in db, returned to client");
            IpInfoDTO info = new IpInfoDTO(entry.TwoLetterCode, entry.ThreeLetterCode, entry.CountryName);
            //update cache
            cacheService.UpdateCacheEntry(Ip, info);
            return Response.Ok(ipInfo);
        }

        //last try from ip2c service
        logger.LogError("GetIpInfo: Could not find Ip Info in db for IP : {ip}, calling IP2C service...", Ip);

        (IpInfoDTO ip2cInfo, IP2C_STATUS statusCode) = await RetrieveIpInfo(Ip);
        if (statusCode != IP2C_STATUS.OK)
            return statusCode == IP2C_STATUS.API_ERROR ? Response.IP_NOT_FOUND : Response.INTERNAL_ERROR;

        //if OK update cache and DB
        cacheService.UpdateCacheEntry(Ip, ip2cInfo);
        Country countryDb = await repository.GetCountryFromIP2CInfo(ip2cInfo);
        if (countryDb == null)
        {
            countryDb = new Country(default, ip2cInfo.CountryName, ip2cInfo.TwoLetterCode, ip2cInfo.ThreeLetterCode, DateTime.Now);
            await repository.AddCountry(countryDb);
        }
        await repository.AddIpAddress(new IpAddress(default, countryDb.Id, Ip, DateTime.Now, DateTime.Now, default));
        logger.LogInformation("GetIpInfo: Found Ip Info from IP2C service, returned to client");

        return Response.Ok(ip2cInfo);
    }

    public async Task<IActionResult> GetIpReport(string[] countryCodes)
    {
        List<IpReportDTO> results;
        //if no query parameters, get all countries
        if (countryCodes == null || countryCodes.Length == 0)
            results = await repository.GetAllIps();
        //else get the IPs with the countries specified
        else
        {
            if (countryCodes.Where(countryCode => countryCode == null || countryCode.Trim().Length != 2).Any())
            {
                logger.LogError("GetIpReport: At least one wrong country code received");
                return Response.IP2C_BAD_COUNTRY_CODE; //400
            }
            results = await repository.GetAllIpsFromCountryCodes(countryCodes);
        }
        logger.LogInformation("GetIpReport: Report generated successfully. IPs count: {Count}", results.Count);
        return Response.Ok(results);
    }
}
