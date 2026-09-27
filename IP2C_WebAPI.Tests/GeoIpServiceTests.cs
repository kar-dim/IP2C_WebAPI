using IP2C_WebAPI.Common;
using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Implementations;
using IP2C_WebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;
using Xunit;

namespace IP2C_WebAPI.Tests;

public class GeoIpServiceTests
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

    [Theory]
    [InlineData("invalid-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetIpInfoAsync_InvalidIp_ReturnsInvalidIpStatus(string ip)
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var repo = new Ip2cRepository(db);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<GeoIpService>>();
        var restClient = new RestClient("https://ip2c.org");

        var service = new GeoIpService(loggerMock.Object, cacheMock.Object, restClient, repo);

        // Act
        var result = await service.GetIpInfoAsync(ip);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Ip2cStatus.InvalidIp, result.Status);
    }

    [Fact]
    public async Task GetIpInfoAsync_CacheHit_ReturnsImmediatelyWithoutHittingDbOrExternal()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var repo = new Ip2cRepository(db);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<GeoIpService>>();
        var restClient = new RestClient("https://ip2c.org");

        var expectedDto = new IpInfoDTO("GR", "GRC", "Greece");
        cacheMock.Setup(c => c.Get("8.8.8.8")).Returns(expectedDto);

        var service = new GeoIpService(loggerMock.Object, cacheMock.Object, restClient, repo);

        // Act
        var result = await service.GetIpInfoAsync("8.8.8.8");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDto, result.IpInfo);
    }

    [Fact]
    public async Task GetReportAsync_CommaSeparatedAndUntrimmedCodes_ParsesAndQueriesSuccessfully()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var country1 = new Country(1, "United States", "US", "USA", DateTime.UtcNow);
        var country2 = new Country(2, "Greece", "GR", "GRC", DateTime.UtcNow);
        var country3 = new Country(3, "Germany", "DE", "DEU", DateTime.UtcNow);
        db.Countries.AddRange(country1, country2, country3);

        db.IpAddresses.AddRange(
            new IpAddress(1, 1, "1.1.1.1", DateTime.UtcNow, DateTime.UtcNow, country1),
            new IpAddress(2, 2, "2.2.2.2", DateTime.UtcNow, DateTime.UtcNow, country2),
            new IpAddress(3, 3, "3.3.3.3", DateTime.UtcNow, DateTime.UtcNow, country3)
        );
        await db.SaveChangesAsync();

        var repo = new Ip2cRepository(db);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<GeoIpService>>();
        var restClient = new RestClient("https://ip2c.org");

        var service = new GeoIpService(loggerMock.Object, cacheMock.Object, restClient, repo);

        // Act: Pass comma-separated with irregular spaces and casing
        var result = await service.GetReportAsync([" us , gr "]);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Reports);
        Assert.Equal(2, result.Reports.Count);
        Assert.Contains(result.Reports, r => r.Name == "United States");
        Assert.Contains(result.Reports, r => r.Name == "Greece");
        Assert.DoesNotContain(result.Reports, r => r.Name == "Germany");
    }

    [Fact]
    public async Task GetReportAsync_InvalidCodeLength_ReturnsInvalidCountryCodeStatus()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var repo = new Ip2cRepository(db);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<GeoIpService>>();
        var restClient = new RestClient("https://ip2c.org");

        var service = new GeoIpService(loggerMock.Object, cacheMock.Object, restClient, repo);

        // Act: Pass invalid length country code
        var result = await service.GetReportAsync(["USA"]);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Ip2cStatus.InvalidCountryCode, result.Status);
    }
}
