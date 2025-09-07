using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Notifico.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/404")]
        public IActionResult Error404()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogWarning("404 Not Found | IP: {Ip} | Path: {Path}", ip, HttpContext.Request.Path);
            return View("404");
        }

        [Route("Error/403")]
        public IActionResult Error403()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogWarning("403 Forbidden | IP: {Ip} | Path: {Path}", ip, HttpContext.Request.Path);
            return View("403");
        }
    }
}
