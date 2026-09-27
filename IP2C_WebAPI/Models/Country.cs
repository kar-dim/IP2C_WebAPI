namespace IP2C_WebAPI.Models;

public class Country
{
    public Country() { }

    public Country(int id, string name, string twoLetterCode, string threeLetterCode, DateTime createdAt)
    {
        Id = id;
        Name = name;
        TwoLetterCode = twoLetterCode;
        ThreeLetterCode = threeLetterCode;
        CreatedAt = createdAt;
    }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TwoLetterCode { get; set; } = string.Empty;
    public string ThreeLetterCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<IpAddress> IpAddresses { get; set; } = [];
}
