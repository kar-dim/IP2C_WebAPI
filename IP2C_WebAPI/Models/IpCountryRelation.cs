namespace IP2C_WebAPI.Models;

public class IpCountryRelation(string ip, string countryName, string twoLetterCode, string threeLetterCode)
{
    public IpCountryRelation() : this(default, default, default, default) { }
    public string Ip { get; set; } = ip;
    public string CountryName { get; set; } = countryName;
    public string TwoLetterCode { get; set; } = twoLetterCode;
    public string ThreeLetterCode { get; set; } = threeLetterCode;
}
