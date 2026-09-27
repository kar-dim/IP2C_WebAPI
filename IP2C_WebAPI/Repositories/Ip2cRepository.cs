using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.Repositories;

public class Ip2cRepository(Ip2cDbContext dbContext) : IIp2cRepository
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCountryAsync(Country country, CancellationToken cancellationToken = default)
    {
        dbContext.Countries.Add(country);
        await SaveChangesAsync(cancellationToken);
    }

    public void UpdateIpAddress(IpAddress address)
    {
        dbContext.IpAddresses.Update(address);
    }

    public async Task AddIpAddressAsync(IpAddress address, CancellationToken cancellationToken = default)
    {
        dbContext.IpAddresses.Add(address);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetCountriesAsDictAsync(CancellationToken cancellationToken = default)
    {
        var countries = await dbContext.Countries
            .AsNoTracking()
            .Select(c => new { Code = c.ThreeLetterCode.Trim(), c.Id })
            .ToListAsync(cancellationToken);

        return countries
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<IpAddress>> GetIpAddressesRangeAsync(int lastId, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        return await dbContext.IpAddresses
            .Include(x => x.Country)
            .Where(x => x.Id > lastId)
            .OrderBy(x => x.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Country?> GetCountryFromIP2CInfoAsync(IpInfoDTO ip2cInfo, CancellationToken cancellationToken = default)
    {
        var twoLetter = ip2cInfo.TwoLetterCode.Trim();
        var threeLetter = ip2cInfo.ThreeLetterCode.Trim();

        return await dbContext.Countries
            .FirstOrDefaultAsync(c =>
                c.TwoLetterCode == twoLetter &&
                c.ThreeLetterCode == threeLetter,
                cancellationToken);
    }

    public async Task<List<IpReportDTO>> GetIpReportAsync(string[]? countryCodes = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.IpAddresses
            .AsNoTracking()
            .Where(ip => ip.Country != null);

        if (countryCodes is { Length: > 0 })
        {
            query = query.Where(ip => countryCodes.Contains(ip.Country!.TwoLetterCode));
        }

        return await query
            .GroupBy(ip => ip.Country!.Name)
            .Select(group => new IpReportDTO(group.Key, group.Count(), group.Max(ip => ip.UpdatedAt)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IpInfoDTO?> GetIpWithCountryAsync(string ip, CancellationToken cancellationToken = default)
    {
        return await dbContext.IpAddresses
            .AsNoTracking()
            .Where(x => x.Ip == ip && x.Country != null)
            .Select(x => new IpInfoDTO(x.Country!.TwoLetterCode, x.Country.ThreeLetterCode, x.Country.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<(string Ip, IpInfoDTO Info)>> GetLatestIpsWithCountryAsync(int maxSize, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.IpAddresses
            .AsNoTracking()
            .Where(x => x.Country != null)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(maxSize)
            .Select(x => new { x.Ip, x.Country!.TwoLetterCode, x.Country.ThreeLetterCode, x.Country.Name })
            .ToListAsync(cancellationToken);

        return list.Select(x => (x.Ip, new IpInfoDTO(x.TwoLetterCode, x.ThreeLetterCode, x.Name))).ToList();
    }
}
