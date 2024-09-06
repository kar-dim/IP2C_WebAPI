﻿namespace IP2C_WebAPI.DTO;

public class IpInfoDTO(string twoLetterCode, string threeLetterCode, string countryName)
{
    public string CountryName { get; set; } = countryName;
    public string TwoLetterCode { get; set; } = twoLetterCode;
    public string ThreeLetterCode { get; set; } = threeLetterCode;
}
