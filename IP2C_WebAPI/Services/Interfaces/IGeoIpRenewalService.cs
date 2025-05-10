namespace IP2C_WebAPI.Services.Interfaces;

//Interface that defines the business logic of IP2C Renewal operations
public interface IGeoIpRenewalService : IHostedService, IDisposable
{
    public Task RenewIpsLoop();
}
