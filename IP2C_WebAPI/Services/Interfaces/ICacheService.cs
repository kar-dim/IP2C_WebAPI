using IP2C_WebAPI.DTO;

//Interface that defines the business logic of Cache operations
namespace IP2C_WebAPI.Services.Interfaces
{
    public interface ICacheService
    {
        public void InitializeCache();

        public IpInfoDTO GetIpInformation(string Ip);

        public void UpdateCacheEntry(string Ip, IpInfoDTO infoDTO);
    }
}
