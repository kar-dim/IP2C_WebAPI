using IP2C_WebAPI.DTO;

namespace IP2C_WebAPI.Common
{
    public enum IP2C_STATUS
    {
        OK,
        CONNECTION_ERROR,
        API_ERROR
    }
    public class Ip2cResult(IpInfoDTO ipInfo, IP2C_STATUS status)
    {
        public IpInfoDTO IpInfo { get; } = ipInfo;
        public IP2C_STATUS Status { get; } = status;

        public bool IsSuccess => Status == IP2C_STATUS.OK;
    }
}
