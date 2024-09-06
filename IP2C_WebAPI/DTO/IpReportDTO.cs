using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.DTO;

[Keyless]
public class IpReportDTO(string name, int addressesCount, DateTime lastAddressUpdated)
{
    public string Name { get; set; } = name;
    public int AddressesCount { get; set; } = addressesCount;
    public DateTime LastAddressUpdated { get; set; } = lastAddressUpdated;
}
