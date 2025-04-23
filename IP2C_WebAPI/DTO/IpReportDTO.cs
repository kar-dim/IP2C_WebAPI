using Microsoft.EntityFrameworkCore;

namespace IP2C_WebAPI.DTO;

[Keyless]
public record IpReportDTO(string Name, int AddressesCount, DateTime LastAddressUpdated);
