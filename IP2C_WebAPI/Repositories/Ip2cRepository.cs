using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.Repositories;
public class Ip2cRepository(Ip2cDbContext dbContext)
{
    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task AddCountry(Country country)
    {
        dbContext.Countries.Add(country);
        await dbContext.SaveChangesAsync();
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
        return await dbContext.Ipaddresses.OrderBy(x => x.Id).Where(x => x.Id > lastId).Take(100).ToListAsync(); //read 100 per batch.ToListAsync();
    }

    public async Task AddIpAddress(IpAddress address)
    {
        dbContext.Ipaddresses.Add(address);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Country> GetCountryFromIP2CInfo(IpInfoDTO ip2cInfo)
    {
        return await dbContext.Countries
            .Where(country => country.TwoLetterCode == ip2cInfo.TwoLetterCode
                && country.ThreeLetterCode == ip2cInfo.ThreeLetterCode
                && string.Equals(country.Name, ip2cInfo.CountryName, StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync();
    }

    public async Task<List<IpReportDTO>> GetAllIps()
    {
        return await dbContext.IpReportDTOs
            .FromSqlRaw(
            $"SELECT Countries.Name, COUNT(Countries.NAME) AS 'AddressesCount', MAX(IPAddresses.UpdatedAt) AS 'LastAddressUpdated' " +
            $"FROM Countries " +
            $"INNER JOIN IPAddresses ON IPAddresses.CountryId = Countries.Id " +
            $"GROUP BY Countries.Name").AsNoTracking().ToListAsync();
    }

    public async Task<List<IpReportDTO>> GetAllIpsFromCountryCodes(string[] countryCodes)
    {
        string countryCodesSqlClause = countryCodes.Length == 1 ? "WHERE Countries.TwoLetterCode = @p0" : "WHERE Countries.TwoLetterCode IN (" + string.Join(", ", countryCodes.Select((_, i) => $"@p{i}")) + ")";
        object[] parameterArray = countryCodes.Select((val, i) => new SqlParameter($"@p{i}", val)).ToArray();
        return await dbContext.IpReportDTOs.FromSqlRaw(
            $"SELECT Countries.Name, COUNT(Countries.NAME) AS 'AddressesCount', MAX(IPAddresses.UpdatedAt) AS 'LastAddressUpdated' " +
            $"FROM Countries INNER JOIN IPAddresses ON IPAddresses.CountryId = Countries.Id " + countryCodesSqlClause + " " +
            "GROUP BY Countries.Name", parameterArray).AsNoTracking().ToListAsync();
    }

    public async Task<IpCountryRelation> GetIpWithCountry(string Ip)
    {
        return await (from ip in dbContext.Ipaddresses
                      join country in dbContext.Countries on ip.CountryId equals country.Id
                      where ip.Ip == Ip
                      select new IpCountryRelation(ip.Ip, country.Name, country.TwoLetterCode, country.ThreeLetterCode))
               .AsNoTracking().FirstOrDefaultAsync();
    }

    public IQueryable<IpCountryRelation> GetIpsWithCountryAsc(int maxSize)
    {
        return (from ip in dbContext.Ipaddresses
                join country in dbContext.Countries on ip.CountryId equals country.Id
                orderby ip.UpdatedAt ascending
                select new IpCountryRelation(ip.Ip, country.Name, country.TwoLetterCode, country.ThreeLetterCode))
                            .Take(maxSize);

    }
}
