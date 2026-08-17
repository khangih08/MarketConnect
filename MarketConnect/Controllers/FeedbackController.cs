using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class FeedbackController : Controller
    {
        [HttpGet("Feedback")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("Feedback/Submit")]
        public IActionResult Submit([FromForm] string content, [FromForm] string? email)
        {
            TempData["SuccessMessage"] = "Cảm ơn bạn đã đóng góp ý kiến cho MarketConnect!";
            return RedirectToAction("Index");
        }
    }
}
