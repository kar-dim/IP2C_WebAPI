using IP2C_WebAPI.DTO;
using RestSharp;

namespace IP2C_WebAPI.Services;
public enum IP2C_STATUS
{
    OK,
    CONNECTION_ERROR,
    API_ERROR
}

//Service that calls IP2C Rest endpoint and retrieves IP Info
public class Ip2cService(ILogger<Ip2cService> logger, RestClient client)
{
    public async Task<(IpInfoDTO, IP2C_STATUS)> RetrieveIpInfo(string ip)
    {
        //GET request to -> https://ip2c.org/{ip}
        var response = await client.ExecuteAsync(new RestRequest(ip, Method.Get));
        if (response == null || string.IsNullOrEmpty(response.Content))
        {
            logger.LogError("Ip2c API connection error...");
            return (null, IP2C_STATUS.CONNECTION_ERROR);
        }
        //ip2c returns in format "1;CD;COD;COUNTRY" (string, no JSON)
        string[] parts = response.Content.Split(';');
        if (!parts[0].Equals("1"))
        {
            logger.LogError("Ip2c API ip not found error...");
            return (null, IP2C_STATUS.API_ERROR);
        }
        //truncate to 50 characters for db safety
        return (new IpInfoDTO(parts[1], parts[2], parts[3][..Math.Min(parts[3].Length, 50)]), IP2C_STATUS.OK);
    }
}
