using IP2C_WebAPI.DTO;
using RestSharp;

namespace IP2C_WebAPI.Services;

public enum IP2C_STATUS
{
    OK,
    CONNECTION_ERROR,
    API_ERROR
}

public class Ip2cService(ILogger<Ip2cService> logger)
{
    public async Task<(IpInfoDTO, IP2C_STATUS)> RetrieveIpInfo(string ip)
    {
        //GET request to -> https://ip2c.org/{ip}
        RestClient client = new RestClient("https://ip2c.org");
        RestRequest request = new RestRequest(ip, Method.Get);
        var response = await client.ExecuteAsync(request);
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
        return (new IpInfoDTO(parts[1], parts[2], parts[3][..Math.Min(parts[3].Length, 50)]), IP2C_STATUS.OK);
    }
}
