using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace IP2C_WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IpController(Ip2cRepository repository, ILogger<IpController> logger, Ip2cService service, IpRenewalService renewalService) : ControllerBase
{
    private static readonly Regex ipPattern = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");

    //Get IP information
    [HttpGet("GetIpInfo/{Ip}")]
    public async Task<ActionResult<IpInfoDTO>> GetIpInfo(string Ip)
    {
        //basic validation of IP (regex works for ipv4 addresses)
        if (!ipPattern.IsMatch(Ip))
        {
            logger.LogError("GetIpInfo: BAD Ip received");
            return BadRequest("BAD IP");
        }
        //first check cache
        IpInfoDTO ipInfo = renewalService.GetIpInformation(Ip);
        if (ipInfo != null)
        {
            logger.LogInformation("GetIpInfo: CACHE HIT for {ip} returned to client", Ip);
            return Ok(ipInfo); //ipInfo in BODY json
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
            renewalService.UpdateCacheEntry(Ip, info);
            return Ok(info);
        }

        //last try from ip2c service
        logger.LogError("GetIpInfo: Could not find Ip Info in db for IP : {ip}, calling IP2C service...", Ip);

        (IpInfoDTO ip2cInfo, IP2C_STATUS statusCode) = await service.RetrieveIpInfo(Ip);
        if (statusCode != IP2C_STATUS.OK)
            return statusCode == IP2C_STATUS.API_ERROR ? NotFound("IP NOT FOUND") : Problem("INTERNAL ERROR");

        //if OK update cache and DB
        renewalService.UpdateCacheEntry(Ip, ip2cInfo);
        Country countryDb = await repository.GetCountryFromIP2CInfo(ip2cInfo);
        if (countryDb == null)
        {
            countryDb = new Country(default, ip2cInfo.CountryName, ip2cInfo.TwoLetterCode, ip2cInfo.ThreeLetterCode, DateTime.Now);
            await repository.AddCountry(countryDb);
        }
        await repository.AddIpAddress(new IpAddress(default, countryDb.Id, Ip, DateTime.Now, DateTime.Now, default));
        logger.LogInformation("GetIpInfo: Found Ip Info from IP2C service, returned to client");

        return Ok(ip2cInfo);
    }

    //Get IPs report
    [HttpGet("GetIpReport")]
    public async Task<ActionResult<List<IpInfoDTO>>> GetIpReport([FromQuery] string[] countryCodes)
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
                return BadRequest("BAD COUNTRY CODE"); //400
            }
            results = await repository.GetAllIpsFromCountryCodes(countryCodes);
        }
        logger.LogInformation("GetIpReport: Report generated successfully. IPs count: {Count}", results.Count);
        return Ok(results);
    }
}
