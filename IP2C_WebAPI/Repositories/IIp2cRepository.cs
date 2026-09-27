using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;

namespace IP2C_WebAPI.Repositories;

public interface IIp2cRepository
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task AddCountryAsync(Country country, CancellationToken cancellationToken = default);

    void UpdateIpAddress(IpAddress address);

    Task AddIpAddressAsync(IpAddress address, CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetCountriesAsDictAsync(CancellationToken cancellationToken = default);

    Task<List<IpAddress>> GetIpAddressesRangeAsync(int lastId, int pageSize = 100, CancellationToken cancellationToken = default);

    Task<Country?> GetCountryFromIP2CInfoAsync(IpInfoDTO ip2cInfo, CancellationToken cancellationToken = default);

    Task<List<IpReportDTO>> GetIpReportAsync(string[]? countryCodes = null, CancellationToken cancellationToken = default);

    Task<IpInfoDTO?> GetIpWithCountryAsync(string ip, CancellationToken cancellationToken = default);

    Task<List<(string Ip, IpInfoDTO Info)>> GetLatestIpsWithCountryAsync(int maxSize, CancellationToken cancellationToken = default);
}
