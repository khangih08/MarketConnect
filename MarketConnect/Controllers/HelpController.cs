using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class HelpController : Controller
    {
        [HttpGet("Help")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
