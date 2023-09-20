using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.DTO
{
    [Keyless]
    public class IpReportDTO
    {
        public string Name { get; set; }
        public int AddressesCount { get; set; }
        public DateTime LastAddressUpdated { get; set; }
    }
}
