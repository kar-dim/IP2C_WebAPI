using Microsoft.AspNetCore.Mvc;

namespace IP2C_WebAPI.Common
{
    public static class Response
    {
        public static IActionResult IP2C_BAD_COUNTRY_CODE => new BadRequestObjectResult("BAD COUNTRY CODE.");
        public static IActionResult IP2C_BAD_IP => new BadRequestObjectResult("BAD IP.");
        public static IActionResult IP_NOT_FOUND => new NotFoundObjectResult("IP NOT FOUND.");
        public static IActionResult INTERNAL_ERROR => new ObjectResult("INTERNAL ERROR") { StatusCode = 500 };
        public static IActionResult Ok(object value) => new OkObjectResult(value);
    }
}
