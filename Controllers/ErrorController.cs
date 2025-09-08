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
            _logger.LogWarning("404 Not Found | IP: {Ip} | Path: {Path}",
                HttpContext.Connection.RemoteIpAddress,
                HttpContext.Request.Path);
            return View("404");
        }

        [Route("Error/403")]
        public IActionResult Error403()
        {
            _logger.LogWarning("403 Forbidden | IP: {Ip} | Path: {Path}",
                HttpContext.Connection.RemoteIpAddress,
                HttpContext.Request.Path);
            return View("403");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            _logger.LogError("Genel Hata | IP: {Ip} | Path: {Path}",
                HttpContext.Connection.RemoteIpAddress,
                HttpContext.Request.Path);
            return View("Error");
        }
    }
}
