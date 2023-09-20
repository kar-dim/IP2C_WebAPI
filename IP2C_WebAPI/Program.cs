using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.Services;
using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Services;

namespace IP2C_WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //add db context
            builder.Services.AddDbContext<Ip2cDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));
            //our custom services
            //ip2c service, is used for calling the IP2C API (from controller and from the IP Renewal service)
            builder.Services.AddScoped<Ip2cService>();
            //ip renewal service -> renews the IPs (local db and cache) by calling the IP2C API every 1 hour, also initializes the cache (from db) on startup
            builder.Services.AddSingleton<IpRenewalService>();
            builder.Services.AddHostedService<IpRenewalService>(provider => provider.GetService<IpRenewalService>());

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
}