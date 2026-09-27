using IP2C_WebAPI.Common;

namespace IP2C_WebAPI.Services.Interfaces;

public interface IGeoIpService
{
    Task<Ip2cResult> RetrieveIpInfoAsync(string ip, CancellationToken cancellationToken = default);

    Task<Ip2cResult> GetIpInfoAsync(string ip, CancellationToken cancellationToken = default);

    Task<IpReportResult> GetReportAsync(string[]? countryCodes, CancellationToken cancellationToken = default);
}
