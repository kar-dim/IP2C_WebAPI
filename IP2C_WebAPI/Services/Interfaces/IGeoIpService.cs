using IP2C_WebAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace IP2C_WebAPI.Services.Interfaces;

//Interface that defines the business logic of IP2C operations
public interface IGeoIpService
{
    public Task<Ip2cResult> RetrieveIpInfo(string ip);

    public Task<IActionResult> GetIpInfo(string Ip);

    public Task<IActionResult> GetIpReport(string[] countryCodes);
}
