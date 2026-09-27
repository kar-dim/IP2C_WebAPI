using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IP2C_WebAPI.Tests;

public class Ip2cRepositoryTests
{
    private static Ip2cDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<Ip2cDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new Ip2cDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public async Task GetIpReportAsync_WhenCountryCodesNullOrEmpty_ReturnsAllAggregatedIps()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var c1 = new Country(1, "Greece", "GR", "GRC", DateTime.UtcNow);
        var c2 = new Country(2, "Germany", "DE", "DEU", DateTime.UtcNow);
        db.Countries.AddRange(c1, c2);

        db.IpAddresses.AddRange(
            new IpAddress(1, 1, "1.1.1.1", DateTime.UtcNow, DateTime.UtcNow, c1),
            new IpAddress(2, 1, "1.1.1.2", DateTime.UtcNow, DateTime.UtcNow, c1),
            new IpAddress(3, 2, "2.2.2.1", DateTime.UtcNow, DateTime.UtcNow, c2)
        );
        await db.SaveChangesAsync();

        var repo = new Ip2cRepository(db);

        // Act
        var report = await repo.GetIpReportAsync();

        // Assert
        Assert.Equal(2, report.Count);
        var grReport = report.First(r => r.Name == "Greece");
        Assert.Equal(2, grReport.AddressesCount);
        var deReport = report.First(r => r.Name == "Germany");
        Assert.Equal(1, deReport.AddressesCount);
    }

    [Fact]
    public async Task GetIpWithCountryAsync_UsingNavigationProperty_ReturnsCorrectDto()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var country = new Country(1, "Italy", "IT", "ITA", DateTime.UtcNow);
        db.Countries.Add(country);
        db.IpAddresses.Add(new IpAddress(1, 1, "8.8.8.8", DateTime.UtcNow, DateTime.UtcNow, country));
        await db.SaveChangesAsync();

        var repo = new Ip2cRepository(db);

        // Act
        var result = await repo.GetIpWithCountryAsync("8.8.8.8");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IT", result.TwoLetterCode);
        Assert.Equal("ITA", result.ThreeLetterCode);
        Assert.Equal("Italy", result.CountryName);
    }

    [Fact]
    public async Task GetCountriesAsDictAsync_TrimsAndUsesCaseInsensitiveLookup()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        db.Countries.Add(new Country(1, "Italy", "IT", "ITA", DateTime.UtcNow));
        db.Countries.Add(new Country(2, "France", "FR", "FRA", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var repo = new Ip2cRepository(db);

        // Act
        var dict = await repo.GetCountriesAsDictAsync();

        // Assert
        Assert.True(dict.ContainsKey("ita"));
        Assert.True(dict.ContainsKey("ITA"));
        Assert.Equal(1, dict["ita"]);
    }
}
