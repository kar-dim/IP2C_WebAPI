using IP2C_WebAPI.DTO;

namespace IP2C_WebAPI.Common
{
    public enum IP2C_STATUS
    {
        OK,
        CONNECTION_ERROR,
        API_ERROR
    }
    public record Ip2cResult(IpInfoDTO IpInfo, IP2C_STATUS Status)
    {
        public Ip2cResult(IP2C_STATUS status) : this(null, status) { }
        public bool IsSuccess => Status == IP2C_STATUS.OK;
    }
}
