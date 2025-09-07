using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Notifico.Models;
using System.Diagnostics;

namespace Notifico.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogError("Unhandled error occurred. RequestId: {RequestId}, IP: {Ip}", requestId, ip);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
