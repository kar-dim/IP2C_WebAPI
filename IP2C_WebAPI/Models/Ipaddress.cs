namespace IP2C_WebAPI.Models;

public class IpAddress(int id, int countryId, string ip, DateTime createdAt, DateTime updatedAt, Country country)
{
    public int Id { get; set; } = id;
    public int CountryId { get; set; } = countryId;
    public string Ip { get; set; } = ip;
    public DateTime CreatedAt { get; set; } = createdAt;
    public DateTime UpdatedAt { get; set; } = updatedAt;
    public Country Country { get; set; } = country;
}
