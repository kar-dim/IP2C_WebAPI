namespace IP2C_WebAPI.Models;

public class Country(int id, string name, string twolettercode, string threelettercode, DateTime createdAt)
{
    public Country() : this(default, default, default, default, default) { }
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string TwoLetterCode { get; set; } = twolettercode;
    public string ThreeLetterCode { get; set; } = threelettercode;
    public DateTime CreatedAt { get; set; } = createdAt;
    public ICollection<IpAddress> Ipaddresses { get; set; } = new List<IpAddress>();
}
