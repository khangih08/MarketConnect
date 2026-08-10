using System.Threading.Tasks;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class AdCampaignsController : Controller
    {
        private readonly IAdService _adService;

        public AdCampaignsController(IAdService adService)
        {
            _adService = adService;
        }

        // GET: /AdCampaigns
        public async Task<IActionResult> Index()
        {
            var packages = await _adService.GetActiveAdPackagesAsync();
            var campaigns = await _adService.GetCampaignsByMerchantAsync(merchantUserId: 3);
            ViewBag.Packages = packages;
            return View(campaigns);
        }

        // POST: /AdCampaigns/Create
        [HttpPost]
        public async Task<IActionResult> Create(int storeId, int adPackageId, string? keywords)
        {
            var campaign = await _adService.CreateCampaignAsync(merchantUserId: 3, storeId: storeId, productId: null, adPackageId: adPackageId, targetProvinceId: 1, targetMarketId: null, keywords: keywords);
            TempData["SuccessMessage"] = "Đã khởi tạo chiến dịch quảng cáo thành công! Đang chờ duyệt.";
            return RedirectToAction("Index");
        }
    }
}
