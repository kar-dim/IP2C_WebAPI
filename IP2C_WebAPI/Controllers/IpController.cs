using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Services;
using System.Net;
using System.Text.RegularExpressions;

namespace IP2C_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IpController : ControllerBase
    {
        private readonly ILogger<IpController> _logger;
        private readonly Ip2cDbContext _mainDbContext;
        private readonly IpRenewalService _ipRenewalService;
        private readonly Ip2cService _ip2cService;

        public IpController(Ip2cDbContext mainDbContext, ILogger<IpController> logger, Ip2cService ip2cService, IpRenewalService ipRenewalService)
        {
            _mainDbContext = mainDbContext;
            _logger = logger;
            _ipRenewalService = ipRenewalService;
            _ip2cService = ip2cService;
        }

        //Get IP information
        [HttpGet("GetIpInfo/{Ip}")]
        public async Task<ActionResult<IpInfoDTO>> GetIpInfo(string Ip)
        {
            //basic validation of IP (regex works for ipv4 addresses)
            Regex pattern = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
            if (!pattern.IsMatch(Ip))
            {
                _logger.LogError("GetIpInfo: BAD Ip received");
                return BadRequest("BAD IP");
            }
            //first check cache
            IpInfoDTO? ipInfo = _ipRenewalService.GetIpInformation(Ip);
            if (ipInfo != null)
            {
                _logger.LogInformation("GetIpInfo: CACHE HIT for " + Ip + ", returned to client");
                return Ok(ipInfo); //ipInfo in BODY json
            }
            _logger.LogInformation("GetIpInfo: CACHE MISS for " + Ip + ", checking in db..");

            //second try from db
            var entry = await (from ip in _mainDbContext.Ipaddresses
                          join country in _mainDbContext.Countries on ip.CountryId equals country.Id
                          where ip.Ip == Ip
                          select new
                          {
                              ip.Ip,
                              country.Name,
                              country.TwoLetterCode,
                              country.ThreeLetterCode
                          }).AsNoTracking().FirstOrDefaultAsync();
            //if entry is not null -> found in db
            if (entry != null)
            {
                _logger.LogInformation("GetIpInfo: Found Ip Info in db, returned to client");
                IpInfoDTO info = new IpInfoDTO
                {
                    CountryName = entry.Name,
                    TwoLetterCode = entry.TwoLetterCode,
                    ThreeLetterCode = entry.ThreeLetterCode
                };
                //update cache
                _ipRenewalService.UpdateCacheEntry(Ip, info);
                //return to client
                return Ok(info);
            }

            //last try from i2pc service
            _logger.LogError("GetIpInfo: Could not find Ip Info in db for IP : " + Ip + ", calling IP2C service...");

            (IpInfoDTO? i2pcInfo, int statusCode) = await _ip2cService.RetrieveIpInfo(Ip);
            //if error
            if (i2pcInfo == null)
                return statusCode == -2 ? NotFound("IP NOT FOUND") : Problem("INTERNAL ERROR");

            //if OK
            //update cache
            _ipRenewalService.UpdateCacheEntry(Ip, i2pcInfo);

            //update DB
            //todo
            Country? countryDb = await _mainDbContext.Countries.Where(x => x.TwoLetterCode.Equals(i2pcInfo.TwoLetterCode) && x.ThreeLetterCode.Equals(i2pcInfo.ThreeLetterCode) && x.Name.ToLower().Equals(i2pcInfo.CountryName.ToLower()) ).FirstOrDefaultAsync();
            if (countryDb == null)
            {
                countryDb = new Country
                {
                    Name = i2pcInfo.CountryName,
                    TwoLetterCode = i2pcInfo.TwoLetterCode,
                    ThreeLetterCode = i2pcInfo.ThreeLetterCode,
                    CreatedAt = DateTime.Now,
                };
                await _mainDbContext.Countries.AddAsync(countryDb);
                await _mainDbContext.SaveChangesAsync();
            }
            Ipaddress toAdd = new Ipaddress
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Ip = Ip,
                CountryId = countryDb.Id
            };
            await _mainDbContext.Ipaddresses.AddAsync(toAdd);
            await _mainDbContext.SaveChangesAsync();
            _logger.LogInformation("GetIpInfo: Found Ip Info from I2PC service, returned to client");

            return Ok(i2pcInfo);
        }

        //Get IPs report
        [HttpGet("GetIpReport")]
        public async Task<ActionResult<List<IpInfoDTO>>> GetIpReport([FromQuery] string[]? countryCodes)
        {
            List<IpReportDTO>? results = null;
            //if no query parameters,  get all countries
            if (countryCodes == null || countryCodes.Length == 0)
                results = await _mainDbContext.IpReportDTOs.FromSqlRaw($"SELECT Countries.Name, COUNT(Countries.NAME) AS 'AddressesCount', MAX(IPAddresses.UpdatedAt) AS 'LastAddressUpdated' FROM Countries INNER JOIN IPAddresses ON IPAddresses.CountryId = Countries.Id GROUP BY Countries.Name").AsNoTracking().ToListAsync(); 
            //else, add to the WHERE clause the countries that the client wants
            else
            {
                //validate countryCodes
                foreach(var country in countryCodes)
                {
                    if (country == null || country.Trim().Length != 2)
                    {
                        _logger.LogError("GetIpReport: At least one wrong country code received");
                        return BadRequest("BAD COUNTRY CODE"); //400
                    }
                }
                string countryCodesSqlClause = countryCodes.Length == 1 ? "WHERE Countries.TwoLetterCode = @p0" : "WHERE Countries.TwoLetterCode IN (" + string.Join(", ", countryCodes.Select((_, i) => $"@p{i}")) + ")";
                object[] parameterArray = countryCodes.Select((val, i) => new SqlParameter($"@p{i}", val)).ToArray();
                results = await _mainDbContext.IpReportDTOs.FromSqlRaw($"SELECT Countries.Name, COUNT(Countries.NAME) AS 'AddressesCount', MAX(IPAddresses.UpdatedAt) AS 'LastAddressUpdated' FROM Countries INNER JOIN IPAddresses ON IPAddresses.CountryId = Countries.Id " + countryCodesSqlClause + " GROUP BY Countries.Name", parameterArray)
                    .AsNoTracking()
                    .ToListAsync();
            }
            if (results == null || results.Count == 0)
            {
                _logger.LogError("GetIpReport: No IPs were found!");
                return NotFound("NO IP DATA FOUND");
            }
            _logger.LogInformation("GetIpReport: Report generated successfully");
            return Ok(results);
        }
    }
    
}
