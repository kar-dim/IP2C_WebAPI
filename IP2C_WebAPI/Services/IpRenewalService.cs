using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using IP2C_WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Services;
using System.Collections.Specialized;

namespace IP2C_WebAPI.Services
{
    public class IpRenewalService : IHostedService, IDisposable
    {
        private string _className;
        private OrderedDictionary _ipCache;
        private readonly object ipCacheLock;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<IpRenewalService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly int maxCacheSize;
        public IpRenewalService(IServiceScopeFactory scopeFactory, ILogger<IpRenewalService> logger, IConfiguration configuration)
        {
            ipCacheLock = new object();
            _className = nameof(IpRenewalService) + ": ";
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger;
            _scopeFactory = scopeFactory;
            using (var scope = _scopeFactory.CreateScope())
            {
                //get the scoped contextDb
                var mainDbContext = scope.ServiceProvider.GetRequiredService<Ip2cDbContext>();

                _configuration = configuration;
                maxCacheSize = _configuration["IpCacheMaxSize"] == null ? 50 : int.Parse(_configuration["IpCacheMaxSize"]);
                //initialize cache from db, get the TOP N ordered by IP "UpdatedAt" (most recent IP which changed, first the old and then the new)
                _ipCache = new OrderedDictionary(maxCacheSize);
                var cacheEntries = (from ip in mainDbContext.Ipaddresses
                                    join country in mainDbContext.Countries on ip.CountryId equals country.Id
                                    orderby ip.UpdatedAt ascending
                                    select new
                                    {
                                        ip.Ip,
                                        country.Name,
                                        country.TwoLetterCode,
                                        country.ThreeLetterCode
                                    }).Take(maxCacheSize);
                //populate cache
                foreach (var cacheEntry in cacheEntries)
                {
                    _ipCache[cacheEntry.Ip] = new IpInfoDTO { 
                        CountryName = cacheEntry.Name, 
                        TwoLetterCode = cacheEntry.TwoLetterCode, 
                        ThreeLetterCode = cacheEntry.ThreeLetterCode 
                    };
                }
            }
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () => await RenewIpsLoop(_cancellationTokenSource.Token));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource?.Cancel();
            await Task.CompletedTask;
        }


        //main service loop, renews the IPs by using the ip2c service and then sleeps for 1 hour
        private async Task RenewIpsLoop(CancellationToken cancellationToken)
        {
            _logger.LogInformation(_className + "RenewIpsLoop called");
            using (var scope = _scopeFactory.CreateScope())
            {
                var ip2cService = scope.ServiceProvider.GetRequiredService<Ip2cService>();
                var mainDbContext = scope.ServiceProvider.GetRequiredService<Ip2cDbContext>();
                while (!cancellationToken.IsCancellationRequested)
                {
                    //get all the countries from our db first
                    List<Country> countries = await mainDbContext.Countries.ToListAsync();
                    if (countries.Any())
                    {
                        Dictionary<string, int> countryIdCodes = new Dictionary<string, int>();
                        foreach(var country in countries)
                        {
                            countryIdCodes[country.ThreeLetterCode] = country.Id;
                        }
                        //get ips by batches (keyset pagination)
                        int lastId = 6; //in our sample db the minimum id = 6
                        while (true)
                        {
                            var ipPage = await mainDbContext.Ipaddresses
                                .OrderBy(x => x.Id)
                                .Where(x => x.Id > lastId)
                                .Take(100) //read 100 per batch
                                .ToListAsync();
                            if (!ipPage.Any())
                                break;
                            //update information for these 100 Ip addresses
                            foreach(var ipAddress in ipPage)
                            {
                                (IpInfoDTO? ipInfo, int result) = await ip2cService.RetrieveIpInfo(ipAddress.Ip);
                                if (ipInfo != null)
                                {
                                    //we have to check if the country for this IP changed
                                    //which MAY happen to be a new country that does not exist in our db, so we must add it
                                    bool countryExists = countryIdCodes.ContainsKey(ipInfo.ThreeLetterCode);
                                    //if country does not exist, we add it to db
                                    if (!countryExists)
                                    {
                                        Country countryToAdd = new Country
                                        {
                                            CreatedAt = DateTime.Now,
                                            TwoLetterCode = ipInfo.TwoLetterCode,
                                            ThreeLetterCode = ipInfo.ThreeLetterCode,
                                            Name = ipInfo.CountryName
                                        };
                                        mainDbContext.Countries.Add(countryToAdd);
                                        //we can't avoid this, we must insert to db so that we can retrieve the ID from db
                                        await mainDbContext.SaveChangesAsync();
                                        countryIdCodes[ipInfo.ThreeLetterCode] = countryToAdd.Id;
                                    }

                                    //update cache (only if changed)
                                    if (_ipCache[ipAddress.Ip] != null && ipAddress.Country != null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                                    {
                                        _ipCache[ipAddress.Ip] = new IpInfoDTO
                                        {
                                            CountryName = ipInfo.CountryName,
                                            TwoLetterCode = ipInfo.TwoLetterCode,
                                            ThreeLetterCode = ipInfo.ThreeLetterCode
                                        };
                                    }

                                    //update db, replace old values with new values ONLY if changes occured
                                    if (ipAddress.Country!= null && !ipAddress.Country.ThreeLetterCode.Equals(ipInfo.ThreeLetterCode))
                                    {
                                        ipAddress.Country = null;
                                        ipAddress.CountryId = countryIdCodes[ipInfo.ThreeLetterCode];
                                        ipAddress.UpdatedAt = DateTime.Now;
                                        mainDbContext.Update(ipAddress);
                                        //await mainDbContext.SaveChangesAsync(); //don't update here
                                    }
                                }
                            }
                            //update all IPs at once
                            await mainDbContext.SaveChangesAsync();
                            lastId += 100;
                        }
                        
                    }
                    //sleep for 1 hour
                    _logger.LogInformation(_className + "Service will sleep for 1 hour");
                    await Task.Delay(3600000);
                }
            }
        }

        public IpInfoDTO? GetIpInformation(string Ip)
        {
            lock (ipCacheLock) {
                return _ipCache.Contains(Ip) ? (IpInfoDTO?)_ipCache[Ip] : null;
            }
        }

        public void UpdateCacheEntry(string Ip, IpInfoDTO infoDTO)
        {
            lock (ipCacheLock)
            {
                //remove the oldest if we are at the cache limit
                if (_ipCache.Count >= maxCacheSize)
                    _ipCache.Remove(_ipCache[0]);
                if (_ipCache.Contains(Ip))
                    _ipCache.Remove(Ip);
                _ipCache[Ip] = infoDTO;
            }

        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
