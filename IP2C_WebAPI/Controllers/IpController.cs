using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace IP2C_WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IpController : ControllerBase
{
    private readonly Ip2cRepository _ip2cRepository;
    private readonly ILogger<IpController> _logger;
    private readonly IpRenewalService _ipRenewalService;
    private readonly Ip2cService _ip2cService;
    private static readonly Regex ipPattern = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
    public IpController(Ip2cRepository ip2cRepository, ILogger<IpController> logger, Ip2cService ip2cService, IpRenewalService ipRenewalService)
    {
        _ip2cRepository = ip2cRepository;
        _logger = logger;
        _ipRenewalService = ipRenewalService;
        _ip2cService = ip2cService;
    }

    //Get IP information
    [HttpGet("GetIpInfo/{Ip}")]
    public async Task<ActionResult<IpInfoDTO>> GetIpInfo(string Ip)
    {
        //basic validation of IP (regex works for ipv4 addresses)
        if (!ipPattern.IsMatch(Ip))
        {
            _logger.LogError("GetIpInfo: BAD Ip received");
            return BadRequest("BAD IP");
        }
        //first check cache
        IpInfoDTO ipInfo = _ipRenewalService.GetIpInformation(Ip);
        if (ipInfo != null)
        {
            _logger.LogInformation("GetIpInfo: CACHE HIT for {ip} returned to client", Ip);
            return Ok(ipInfo); //ipInfo in BODY json
        }
        _logger.LogInformation("GetIpInfo: CACHE MISS for {ip}, checking in db..", Ip);

        //second try from db
        IpCountryRelation entry = await _ip2cRepository.GetIpWithCountry(Ip);
        //if entry is not null -> found in db
        if (entry != null)
        {
            _logger.LogInformation("GetIpInfo: Found Ip Info in db, returned to client");
            IpInfoDTO info = new IpInfoDTO(entry.TwoLetterCode, entry.ThreeLetterCode, entry.CountryName);
            //update cache
            _ipRenewalService.UpdateCacheEntry(Ip, info);
            return Ok(info);
        }

        //last try from i2pc service
        _logger.LogError("GetIpInfo: Could not find Ip Info in db for IP : {ip}, calling IP2C service...", Ip);

        (IpInfoDTO i2pcInfo, IP2C_STATUS statusCode) = await _ip2cService.RetrieveIpInfo(Ip);
        if (statusCode != IP2C_STATUS.OK)
            return statusCode == IP2C_STATUS.API_ERROR ? NotFound("IP NOT FOUND") : Problem("INTERNAL ERROR");

        //if OK update cache and DB
        _ipRenewalService.UpdateCacheEntry(Ip, i2pcInfo);
        Country countryDb = await _ip2cRepository.GetCountryFromI2PcInfo(i2pcInfo);
        if (countryDb == null)
        {
            countryDb = new Country(default, i2pcInfo.CountryName, i2pcInfo.TwoLetterCode, i2pcInfo.ThreeLetterCode, DateTime.Now);
            await _ip2cRepository.AddCountry(countryDb);
        }
        await _ip2cRepository.AddIpAddress(new IpAddress(default, countryDb.Id, Ip, DateTime.Now, DateTime.Now, default));
        _logger.LogInformation("GetIpInfo: Found Ip Info from I2PC service, returned to client");

        return Ok(i2pcInfo);
    }

    //Get IPs report
    [HttpGet("GetIpReport")]
    public async Task<ActionResult<List<IpInfoDTO>>> GetIpReport([FromQuery] string[] countryCodes)
    {
        List<IpReportDTO> results;
        //if no query parameters, get all countries
        if (countryCodes == null || countryCodes.Length == 0)
            results = await _ip2cRepository.GetAllIps();
        //else get the IPs with the countries specified
        else
        {
            if (countryCodes.Where(countryCode => countryCode == null || countryCode.Trim().Length != 2).Any())
            {
                _logger.LogError("GetIpReport: At least one wrong country code received");
                return BadRequest("BAD COUNTRY CODE"); //400
            }
            results = await _ip2cRepository.GetAllIpsFromCountryCodes(countryCodes);
        }
        _logger.LogInformation("GetIpReport: Report generated successfully. IPs count: {Count}", results.Count);
        return Ok(results);
    }
}
