using MarketConnect.Data;
using MarketConnect.Models;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace MarketConnect.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _products;

        public HomeController(IProductService products)
        {
            _products = products;
        }

        public async Task<IActionResult> Index(string q)
        {
            var items = await _products.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                items = items.Where(p => p.Name != null && p.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase));
            }
            return View(items);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
