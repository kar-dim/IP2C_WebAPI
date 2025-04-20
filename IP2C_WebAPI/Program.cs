using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services;
using Microsoft.EntityFrameworkCore;
using RestSharp;

namespace IP2C_WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        //add db context
        builder.Services.AddDbContext<Ip2cDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

        //our custom services
        //ip2c service and repository
        builder.Services.AddScoped<Ip2cService>();
        builder.Services.AddScoped<Ip2cRepository>();
        //ip renewal service -> renews the IPs (local db and cache) by calling the IP2C API every 1 hour, also initializes the cache (from db) on startup
        builder.Services.AddSingleton<CacheService>();
        builder.Services.AddSingleton<IpRenewalService>();
        builder.Services.AddHostedService(provider => provider.GetService<IpRenewalService>());
        //Singleton RestClient for IP2C service rest calls
        builder.Services.AddSingleton(provider => new RestClient("https://ip2c.org"));

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}