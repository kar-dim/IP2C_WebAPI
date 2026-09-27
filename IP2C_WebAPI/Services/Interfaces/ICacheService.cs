using IP2C_WebAPI.DTO;

namespace IP2C_WebAPI.Services.Interfaces;

public interface ICacheService
{
    Task InitializeCacheAsync(CancellationToken cancellationToken = default);

    IpInfoDTO? Get(string ip);

    void Set(string ip, IpInfoDTO infoDTO);
}
