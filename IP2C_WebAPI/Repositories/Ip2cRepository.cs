using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.Repositories;
public class Ip2cRepository(Ip2cDbContext dbContext)
{
    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task AddCountryAsync(Country country)
    {
        dbContext.Countries.Add(country);
        await SaveChangesAsync();
    }

    public void UpdateIpAddress(IpAddress address)
    {
        dbContext.Ipaddresses.Update(address);
    }

    public async Task<List<Country>> GetCountriesAsync()
    {
        return await dbContext.Countries.ToListAsync();
    }

    public async Task<List<IpAddress>> GetIpAddressesRangeAsync(int lastId)
    {
        return await dbContext.Ipaddresses
            .OrderBy(x => x.Id)
            .Where(x => x.Id > lastId)
            .Take(100).ToListAsync(); //read 100 per batch
    }

    public async Task AddIpAddressAsync(IpAddress address)
    {
        dbContext.Ipaddresses.Add(address);
        await SaveChangesAsync();
    }

    public async Task<Country> GetCountryFromIP2CInfoAsync(IpInfoDTO ip2cInfo)
    {
        return await dbContext.Countries
            .Where(country => country.TwoLetterCode == ip2cInfo.TwoLetterCode
                && country.ThreeLetterCode == ip2cInfo.ThreeLetterCode
                && country.Name.ToLower() == ip2cInfo.CountryName.ToLower()).FirstOrDefaultAsync();
    }

    public async Task<List<IpReportDTO>> GetAllIpsAsync()
    {
        return await dbContext.Ipaddresses
            .Where(ip => ip.Country != null)
            .GroupBy(ip => ip.Country.Name)
            .Select(group => new IpReportDTO(group.Key, group.Count(), group.Max(ip => ip.UpdatedAt)))
            .AsNoTracking().ToListAsync();
    }

    public async Task<List<IpReportDTO>> GetAllIpsFromCountryCodesAsync(string[] countryCodes)
    {
        return await dbContext.Ipaddresses
           .Where(ip => ip.Country != null && countryCodes.Contains(ip.Country.TwoLetterCode))
           .GroupBy(ip => ip.Country.Name)
           .Select(group => new IpReportDTO(group.Key, group.Count(), group.Max(ip => ip.UpdatedAt)))
           .AsNoTracking().ToListAsync();
    }

    public async Task<IpCountryRelation> GetIpWithCountryAsync(string Ip)
    {
        return await dbContext.Ipaddresses
            .Join(dbContext.Countries, ipAddr => ipAddr.CountryId, country => country.Id, (ipAddr, country) => new { ipAddr, country })
            .Where(x => x.ipAddr.Ip == Ip)
            .Select(x => new IpCountryRelation(x.ipAddr.Ip, x.country.Name, x.country.TwoLetterCode, x.country.ThreeLetterCode))
            .AsNoTracking().FirstOrDefaultAsync();
    }

    public IQueryable<IpCountryRelation> GetIpsWithCountryAsc(int maxSize)
    {
        return dbContext.Ipaddresses
            .Join(dbContext.Countries, ip => ip.CountryId, country => country.Id, (ip, country) => new { ip, country })
            .OrderBy(x => x.ip.UpdatedAt)
            .Take(maxSize)
            .Select(x => new IpCountryRelation(x.ip.Ip, x.country.Name, x.country.TwoLetterCode, x.country.ThreeLetterCode));
    }
}
