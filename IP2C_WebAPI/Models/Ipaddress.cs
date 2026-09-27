namespace IP2C_WebAPI.Models;

public class IpAddress
{
    public IpAddress() { }

    public IpAddress(int id, int countryId, string ip, DateTime createdAt, DateTime updatedAt, Country? country = null)
    {
        Id = id;
        CountryId = countryId;
        Ip = ip;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Country = country;
    }

    public int Id { get; set; }
    public int CountryId { get; set; }
    public string Ip { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Country? Country { get; set; }
}
