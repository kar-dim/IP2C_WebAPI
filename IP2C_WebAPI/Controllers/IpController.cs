using IP2C_WebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IP2C_WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IpController(IGeoIpService service) : ControllerBase
{
    //Get IP information
    [HttpGet("GetIpInfo/{Ip}")]
    public async Task<IActionResult> GetIpInfo(string Ip)
    {
        return await service.GetIpInfo(Ip);
    }

    //Get IPs report
    [HttpGet("GetIpReport")]
    public async Task<IActionResult> GetIpReport([FromQuery] string[] countryCodes)
    {
        return await service.GetIpReport(countryCodes);
    }
}
