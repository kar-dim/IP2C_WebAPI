using IP2C_WebAPI.Contexts;
using IP2C_WebAPI.DTO;
using RestSharp;

namespace ReactMeals_WebApi.Services
{
    public class Ip2cService
    {
        private string _className;
        private readonly Ip2cDbContext _mainDbContext;
        private readonly ILogger<Ip2cService> _logger;
        private readonly IConfiguration _configuration;
        public Ip2cService(Ip2cDbContext mainDbContext, ILogger<Ip2cService> logger, IConfiguration congiguration)
        {
            _className = nameof(Ip2cService) + ": ";
            _mainDbContext = mainDbContext;
            _logger = logger;
            _configuration = congiguration;
        }
        public async Task<(IpInfoDTO?, int)> RetrieveIpInfo(string ip)
        {
            //GET request to -> https://ip2c.org/{ip}
            RestClient client = new RestClient("https://ip2c.org");
            RestRequest request = new RestRequest(ip, Method.Get);
            var response = await client.ExecuteAsync(request);
            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                _logger.LogError(_className + "Ip2c API connection error...");
                return (null, -1); //-1 -> connection error (return 500)
            }
            //ip2c returns in format "1;CD;COD;COUNTRY" (string, no JSON)
            string[] parts = response.Content.Split(';');
            if (!parts[0].Equals("1"))
            {
                _logger.LogError(_className + "Ip2c API ip not found error...");
                return (null, -2); //-2 -> API error (return 404)
            }
            return (new IpInfoDTO
            {
                TwoLetterCode = parts[1],
                ThreeLetterCode = parts[2],
                CountryName = parts[3].Substring(0, Math.Min(parts[3].Length, 50)) //our database scheme allows 50 max name characters
            }, 0);
        }
    }
}
