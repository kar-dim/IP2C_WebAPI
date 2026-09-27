using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace IP2C_WebAPI.Tests;

public class CacheServiceTests
{
    private static CacheService CreateCacheService(int maxSize)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IpCacheMaxSize"] = maxSize.ToString()
            })
            .Build();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        return new CacheService(config, scopeFactoryMock.Object);
    }

    [Fact]
    public void Set_WhenKeyAlreadyExists_DoesNotEvictIndexZeroOrShrinkCount()
    {
        // Arrange
        var cache = CreateCacheService(3);
        cache.Set("1.1.1.1", new IpInfoDTO("US", "USA", "United States"));
        cache.Set("2.2.2.2", new IpInfoDTO("GR", "GRC", "Greece"));
        cache.Set("3.3.3.3", new IpInfoDTO("DE", "DEU", "Germany"));

        // Act: Update existing key "2.2.2.2"
        cache.Set("2.2.2.2", new IpInfoDTO("FR", "FRA", "France"));

        // Assert: "1.1.1.1" MUST still be in the cache! It should not have been evicted!
        Assert.NotNull(cache.Get("1.1.1.1"));
        Assert.NotNull(cache.Get("3.3.3.3"));

        var updated = cache.Get("2.2.2.2");
        Assert.NotNull(updated);
        Assert.Equal("FRA", updated.ThreeLetterCode);
    }

    [Fact]
    public void Set_WhenNewKeyAndCacheFull_EvictsLeastRecentlyUsedKey()
    {
        // Arrange
        var cache = CreateCacheService(2);
        cache.Set("1.1.1.1", new IpInfoDTO("US", "USA", "United States"));
        cache.Set("2.2.2.2", new IpInfoDTO("GR", "GRC", "Greece"));

        // Act: Insert new key "3.3.3.3", should evict LRU "1.1.1.1"
        cache.Set("3.3.3.3", new IpInfoDTO("DE", "DEU", "Germany"));

        // Assert
        Assert.Null(cache.Get("1.1.1.1"));
        Assert.NotNull(cache.Get("2.2.2.2"));
        Assert.NotNull(cache.Get("3.3.3.3"));
    }

    [Fact]
    public void Get_WhenAccessed_PromotesItemToMostRecentlyUsed()
    {
        // Arrange
        var cache = CreateCacheService(2);
        cache.Set("1.1.1.1", new IpInfoDTO("US", "USA", "United States"));
        cache.Set("2.2.2.2", new IpInfoDTO("GR", "GRC", "Greece"));

        // Act: Access "1.1.1.1" to refresh its LRU position (making "2.2.2.2" the oldest)
        var accessed = cache.Get("1.1.1.1");
        Assert.NotNull(accessed);

        // Add a new key "3.3.3.3"; should evict "2.2.2.2" instead of "1.1.1.1"
        cache.Set("3.3.3.3", new IpInfoDTO("DE", "DEU", "Germany"));

        // Assert
        Assert.NotNull(cache.Get("1.1.1.1"));
        Assert.Null(cache.Get("2.2.2.2"));
        Assert.NotNull(cache.Get("3.3.3.3"));
    }
}
