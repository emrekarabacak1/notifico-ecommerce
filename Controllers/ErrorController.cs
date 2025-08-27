using Microsoft.AspNetCore.Mvc;

namespace Notifico.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult Error404()
        {
            return View("404");
        }

        [Route("Error/403")]
        public IActionResult Error403()
        {
            return View("403");
        }

    }
}
