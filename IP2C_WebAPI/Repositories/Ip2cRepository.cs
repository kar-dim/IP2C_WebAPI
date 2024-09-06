using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.Repositories;
public class Ip2cRepository(Ip2cDbContext ip2CDbContext)
{
    public async Task SaveChangesAsync()
    {
        await ip2CDbContext.SaveChangesAsync();
    }

    public async Task AddCountry(Country country)
    {
        ip2CDbContext.Countries.Add(country);
        await ip2CDbContext.SaveChangesAsync();
    }

    public void UpdateIpAddress(IpAddress address)
    {
        ip2CDbContext.Ipaddresses.Update(address);
    }

    public async Task<List<Country>> GetCountriesAsync()
    {
        return await ip2CDbContext.Countries.ToListAsync();
    }

    public async Task<List<IpAddress>> GetIpAddressesRangeAsync(int lastId)
    {
        return await ip2CDbContext.Ipaddresses.OrderBy(x => x.Id).Where(x => x.Id > lastId).Take(100).ToListAsync(); //read 100 per batch.ToListAsync();
    }

    public async Task AddIpAddress(IpAddress address)
    {
        ip2CDbContext.Ipaddresses.Add(address);
        await ip2CDbContext.SaveChangesAsync();
    }

    public async Task<Country> GetCountryFromI2PcInfo(IpInfoDTO i2pcInfo)
    {
        return await ip2CDbContext.Countries
            .Where(country => country.TwoLetterCode.Equals(i2pcInfo.TwoLetterCode)
                && country.ThreeLetterCode.Equals(i2pcInfo.ThreeLetterCode)
                && country.Name.ToLower().Equals(i2pcInfo.CountryName.ToLower())).FirstOrDefaultAsync();
    }

    public async Task<List<IpReportDTO>> GetAllIps()
    {
        return await ip2CDbContext.IpReportDTOs
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
        return await ip2CDbContext.IpReportDTOs.FromSqlRaw(
            $"SELECT Countries.Name, COUNT(Countries.NAME) AS 'AddressesCount', MAX(IPAddresses.UpdatedAt) AS 'LastAddressUpdated' " +
            $"FROM Countries INNER JOIN IPAddresses ON IPAddresses.CountryId = Countries.Id " + countryCodesSqlClause + " " +
            "GROUP BY Countries.Name", parameterArray).AsNoTracking().ToListAsync();
    }

    public async Task<IpCountryRelation> GetIpWithCountry(string Ip)
    {
        return await (from ip in ip2CDbContext.Ipaddresses
                      join country in ip2CDbContext.Countries on ip.CountryId equals country.Id
                      where ip.Ip == Ip
                      select new IpCountryRelation(ip.Ip, country.Name, country.TwoLetterCode, country.ThreeLetterCode))
               .AsNoTracking().FirstOrDefaultAsync();
    }

    public IQueryable<IpCountryRelation> GetIpsWithCountryAsc(int maxSize)
    {
        return (from ip in ip2CDbContext.Ipaddresses
                join country in ip2CDbContext.Countries on ip.CountryId equals country.Id
                orderby ip.UpdatedAt ascending
                select new IpCountryRelation(ip.Ip, country.Name, country.TwoLetterCode, country.ThreeLetterCode))
                            .Take(maxSize);

    }
}
