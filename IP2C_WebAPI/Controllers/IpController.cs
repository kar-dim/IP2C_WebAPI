using IP2C_WebAPI.Common;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IP2C_WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IpController(IGeoIpService service) : ControllerBase
{
    /// <summary>
    /// Look up country information for an IP address.
    /// </summary>
    [HttpGet("{ip}")]
    [ProducesResponseType(typeof(IpInfoDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetByIpAsync(string ip, CancellationToken cancellationToken)
    {
        var result = await service.GetIpInfoAsync(ip, cancellationToken);
        return result.Status switch
        {
            Ip2cStatus.Ok => Ok(result.IpInfo),
            Ip2cStatus.InvalidIp => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid IP Address", detail: result.ErrorMessage),
            Ip2cStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: result.ErrorMessage),
            Ip2cStatus.UpstreamError => Problem(statusCode: StatusCodes.Status502BadGateway, title: "Upstream Error", detail: result.ErrorMessage),
            Ip2cStatus.ConnectionError => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Service Unavailable", detail: result.ErrorMessage),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error", detail: result.ErrorMessage)
        };
    }

    /// <summary>
    /// Get a report of registered IP addresses grouped by country.
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(List<IpReportDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReportAsync([FromQuery] string[]? countryCodes, CancellationToken cancellationToken)
    {
        var result = await service.GetReportAsync(countryCodes, cancellationToken);
        return result.Status switch
        {
            Ip2cStatus.Ok => Ok(result.Reports),
            Ip2cStatus.InvalidCountryCode => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Country Code", detail: result.ErrorMessage),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error", detail: result.ErrorMessage)
        };
    }
}
