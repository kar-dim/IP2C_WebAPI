using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.Repositories;
using IP2C_WebAPI.Services.Implementations;
using IP2C_WebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using RestSharp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Database context
builder.Services.AddDbContext<Ip2cDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

// Repositories & Services
builder.Services.AddScoped<IIp2cRepository, Ip2cRepository>();
builder.Services.AddScoped<IGeoIpService, GeoIpService>();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Hosted background worker
builder.Services.AddHostedService<GeoIpRenewalService>();

// Configured RestClient with timeout options
builder.Services.AddSingleton(new RestClient(new RestClientOptions("https://ip2c.org")
{
    Timeout = TimeSpan.FromSeconds(10)
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.MapControllers();
app.Run();